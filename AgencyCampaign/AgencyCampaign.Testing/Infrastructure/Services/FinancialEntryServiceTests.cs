using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Models.Financial;
using AgencyCampaign.Application.Requests.FinancialEntries;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.TestSupport;
using Archon.Application.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class FinancialEntryServiceTests
    {
        private TestDbContext db = null!;
        private Mock<IAutomationDispatcher> automation = null!;
        private Mock<INotificationService> notifications = null!;
        private FinancialEntryService service = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            automation = new Mock<IAutomationDispatcher>();
            notifications = new Mock<INotificationService>();
            service = new FinancialEntryService(db, automation.Object, notifications.Object, NullLogger<FinancialEntryService>.Instance);
        }

        [TearDown]
        public void TearDown() => db.Dispose();

        private async Task<FinancialAccount> SeedAccountAsync()
        {
            FinancialAccount account = new("Conta", FinancialAccountType.Bank, 0m, "#fff");
            db.Add(account);
            await db.SaveChangesAsync();
            return account;
        }

        private static CreateFinancialEntryRequest BuildCreateRequest(long accountId, FinancialEntryStatus status = FinancialEntryStatus.Pending, decimal amount = 1000m)
        {
            return new CreateFinancialEntryRequest
            {
                AccountId = accountId,
                Type = FinancialEntryType.Receivable,
                Category = FinancialEntryCategory.BrandReceivable,
                Description = "Recebível",
                Amount = amount,
                DueAt = DateTimeOffset.UtcNow.AddDays(15),
                OccurredAt = DateTimeOffset.UtcNow,
                Status = status
            };
        }

        private FinancialEntryService BuildServiceWithIntegration(out Mock<IIntegrationCapabilityService> capability)
        {
            capability = new Mock<IIntegrationCapabilityService>();
            capability
                .Setup(item => item.ResolveForExecution(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ResolvedCapability("receivable.issue-invoice", "payment.charge.create", 5));
            return new FinancialEntryService(db, automation.Object, notifications.Object, NullLogger<FinancialEntryService>.Instance,
                IntegrationPlatformClientFactory.CreateInert(), capability.Object, TenantContextMock.Create());
        }

        private async Task<FinancialEntry> SeedReceivableAsync(FinancialEntryType type = FinancialEntryType.Receivable)
        {
            FinancialAccount account = await SeedAccountAsync();
            FinancialEntry entry = new(account.Id, type, type == FinancialEntryType.Receivable ? FinancialEntryCategory.BrandReceivable : FinancialEntryCategory.OperationalCost, "Recebível", 500m, DateTimeOffset.UtcNow.AddDays(10), DateTimeOffset.UtcNow);
            db.Add(entry);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            return entry;
        }

        [Test]
        public async Task IssueCharge_should_throw_for_a_payable()
        {
            FinancialEntry payable = await SeedReceivableAsync(FinancialEntryType.Payable);
            FinancialEntryService service = BuildServiceWithIntegration(out _);

            Func<Task> act = () => service.IssueCharge(payable.Id, new IssueChargeRequest());

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("financialEntry.charge.onlyReceivable");
        }

        [Test]
        public async Task IssueCharge_should_mark_charge_failed_when_enqueue_fails()
        {
            FinancialEntry receivable = await SeedReceivableAsync();
            FinancialEntryService service = BuildServiceWithIntegration(out _);

            Func<Task> act = () => service.IssueCharge(receivable.Id, new IssueChargeRequest());
            await act.Should().ThrowAsync<Exception>();

            FinancialEntry refreshed = await db.Set<FinancialEntry>().AsNoTracking().FirstAsync(item => item.Id == receivable.Id);
            refreshed.ChargeStatus.Should().Be(ChargeStatus.Failed);
        }

        [Test]
        public async Task HandleChargeCallback_created_should_store_charge_and_mark_issued()
        {
            FinancialEntry receivable = await SeedReceivableAsync();

            await service.HandleChargeCallback(new FinancialEntryChargeCallbackRequest
            {
                Provider = "asaas",
                ChargeId = "chg_1",
                EventType = "created",
                FinancialEntryId = receivable.Id,
                ChargeUrl = "https://asaas/boleto/1"
            });

            FinancialEntry refreshed = await db.Set<FinancialEntry>().AsNoTracking().FirstAsync(item => item.Id == receivable.Id);
            refreshed.ChargeId.Should().Be("chg_1");
            refreshed.ChargeUrl.Should().Be("https://asaas/boleto/1");
            refreshed.ChargeStatus.Should().Be(ChargeStatus.Issued);
            refreshed.Status.Should().Be(FinancialEntryStatus.Pending);
        }

        [Test]
        public async Task HandleChargeCallback_should_store_banking_artifacts()
        {
            FinancialEntry receivable = await SeedReceivableAsync();

            await service.HandleChargeCallback(new FinancialEntryChargeCallbackRequest
            {
                Provider = "asaas",
                ChargeId = "chg_art",
                EventType = "issued",
                FinancialEntryId = receivable.Id,
                DigitableLine = "34191.79001 01043.510047 91020.150008 8 99999999999999",
                BarCode = "34198999900000000000000000000000000000000000",
                NossoNumero = "000123456-7",
                PixCopyPaste = "00020126580014br.gov.bcb.pix...",
                PixQrCodeUrl = "https://prov/qr/chg_art.png",
                TxId = "tx-art-001",
                BankSlipUrl = "https://prov/boleto/chg_art.pdf"
            });

            FinancialEntry refreshed = await db.Set<FinancialEntry>().AsNoTracking().FirstAsync(item => item.Id == receivable.Id);
            refreshed.ChargeDigitableLine.Should().StartWith("34191.79001");
            refreshed.ChargeBarCode.Should().HaveLength(44);
            refreshed.ChargeNossoNumero.Should().Be("000123456-7");
            refreshed.ChargePixCopyPaste.Should().StartWith("00020126");
            refreshed.ChargePixQrCodeUrl.Should().Be("https://prov/qr/chg_art.png");
            refreshed.ChargeTxId.Should().Be("tx-art-001");
            refreshed.ChargeBankSlipUrl.Should().Be("https://prov/boleto/chg_art.pdf");
        }

        [Test]
        public async Task HandleChargeCallback_paid_should_settle_and_be_idempotent()
        {
            FinancialEntry receivable = await SeedReceivableAsync();

            await service.HandleChargeCallback(new FinancialEntryChargeCallbackRequest { Provider = "asaas", ChargeId = "chg_2", EventType = "created", FinancialEntryId = receivable.Id });
            await service.HandleChargeCallback(new FinancialEntryChargeCallbackRequest { Provider = "asaas", ChargeId = "chg_2", EventType = "paid", PaidAt = DateTimeOffset.UtcNow, EndToEndId = "E2E-9" });
            await service.HandleChargeCallback(new FinancialEntryChargeCallbackRequest { Provider = "asaas", ChargeId = "chg_2", EventType = "paid", PaidAt = DateTimeOffset.UtcNow, EndToEndId = "E2E-9" });

            FinancialEntry refreshed = await db.Set<FinancialEntry>().AsNoTracking().FirstAsync(item => item.Id == receivable.Id);
            refreshed.Status.Should().Be(FinancialEntryStatus.Paid);
            refreshed.ChargeStatus.Should().Be(ChargeStatus.Paid);
            refreshed.ReferenceCode.Should().Be("E2E-9");
        }

        [Test]
        public async Task HandleChargeCallback_should_throw_when_entry_not_found()
        {
            Func<Task> act = () => service.HandleChargeCallback(new FinancialEntryChargeCallbackRequest { Provider = "x", ChargeId = "y", EventType = "paid", FinancialEntryId = 999 });
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task CreateEntry_should_throw_when_account_not_found()
        {
            CreateFinancialEntryRequest request = BuildCreateRequest(99);
            Func<Task> act = () => service.CreateEntry(request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task CreateEntry_should_throw_when_subcategory_not_found()
        {
            FinancialAccount account = await SeedAccountAsync();
            CreateFinancialEntryRequest request = BuildCreateRequest(account.Id);
            request.SubcategoryId = 99;

            Func<Task> act = () => service.CreateEntry(request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task CreateEntry_should_dispatch_created_trigger()
        {
            FinancialAccount account = await SeedAccountAsync();

            await service.CreateEntry(BuildCreateRequest(account.Id));

            automation.Verify(item => item.DispatchAsync(AutomationTriggers.FinancialReceivableCreated, It.IsAny<IDictionary<string, object?>>(), It.IsAny<CancellationToken>()), Times.Once);
            automation.Verify(item => item.DispatchAsync(AutomationTriggers.FinancialReceivableSettled, It.IsAny<IDictionary<string, object?>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task CreateEntry_when_paid_should_also_dispatch_settled_trigger()
        {
            FinancialAccount account = await SeedAccountAsync();

            CreateFinancialEntryRequest request = BuildCreateRequest(account.Id, FinancialEntryStatus.Paid);
            request.PaidAt = DateTimeOffset.UtcNow;

            await service.CreateEntry(request);

            automation.Verify(item => item.DispatchAsync(AutomationTriggers.FinancialReceivableCreated, It.IsAny<IDictionary<string, object?>>(), It.IsAny<CancellationToken>()), Times.Once);
            automation.Verify(item => item.DispatchAsync(AutomationTriggers.FinancialReceivableSettled, It.IsAny<IDictionary<string, object?>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task CreateInstallmentSeries_should_reject_total_below_two()
        {
            FinancialAccount account = await SeedAccountAsync();
            CreateInstallmentSeriesRequest request = new()
            {
                AccountId = account.Id,
                Type = FinancialEntryType.Payable,
                Category = FinancialEntryCategory.CreatorPayout,
                Description = "Parcelado",
                Amount = 1000m,
                DueAt = DateTimeOffset.UtcNow,
                OccurredAt = DateTimeOffset.UtcNow,
                InstallmentTotal = 1
            };

            Func<Task> act = () => service.CreateInstallmentSeries(request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task CreateInstallmentSeries_should_split_amount_with_remainder_on_last_installment()
        {
            FinancialAccount account = await SeedAccountAsync();
            CreateInstallmentSeriesRequest request = new()
            {
                AccountId = account.Id,
                Type = FinancialEntryType.Payable,
                Category = FinancialEntryCategory.CreatorPayout,
                Description = "Parcelado",
                Amount = 100m,
                DueAt = DateTimeOffset.UtcNow,
                OccurredAt = DateTimeOffset.UtcNow,
                InstallmentTotal = 3
            };

            IReadOnlyCollection<FinancialEntry> entries = await service.CreateInstallmentSeries(request);

            entries.Should().HaveCount(3);
            // 100/3 = 33.33; 33.33*2 = 66.66; remainder = 33.34
            entries.Take(2).Sum(item => item.Amount).Should().Be(66.66m);
            entries.Last().Amount.Should().Be(33.34m);
            entries.Last().InstallmentNumber.Should().Be(3);
            entries.Last().InstallmentTotal.Should().Be(3);
        }

        [Test]
        public async Task CreateInstallmentSeries_should_link_children_to_first_entry_as_parent()
        {
            FinancialAccount account = await SeedAccountAsync();
            CreateInstallmentSeriesRequest request = new()
            {
                AccountId = account.Id,
                Type = FinancialEntryType.Payable,
                Category = FinancialEntryCategory.CreatorPayout,
                Description = "Parcelado",
                Amount = 300m,
                DueAt = DateTimeOffset.UtcNow,
                OccurredAt = DateTimeOffset.UtcNow,
                InstallmentTotal = 3
            };

            IReadOnlyCollection<FinancialEntry> entries = await service.CreateInstallmentSeries(request);

            FinancialEntry first = entries.OrderBy(item => item.InstallmentNumber).First();
            entries.Skip(1).Should().OnlyContain(item => item.ParentEntryId == first.Id);
        }

        [Test]
        public async Task UpdateEntry_should_throw_when_id_mismatch()
        {
            UpdateFinancialEntryRequest request = new()
            {
                Id = 5, AccountId = 1, Description = "x", Amount = 0,
                DueAt = DateTimeOffset.UtcNow, OccurredAt = DateTimeOffset.UtcNow
            };
            Func<Task> act = () => service.UpdateEntry(99, request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task MarkAsPaid_should_set_status_paid_and_dispatch_settled_trigger()
        {
            FinancialAccount account = await SeedAccountAsync();
            FinancialEntry entry = new(
                account.Id, FinancialEntryType.Receivable, FinancialEntryCategory.BrandReceivable,
                "x", 100m, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            db.Add(entry);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            FinancialEntry result = await service.MarkAsPaid(entry.Id, new MarkAsPaidRequest
            {
                AccountId = account.Id,
                PaidAt = DateTimeOffset.UtcNow
            });

            result.Status.Should().Be(FinancialEntryStatus.Paid);
            result.PaidAt.Should().NotBeNull();
            automation.Verify(item => item.DispatchAsync(AutomationTriggers.FinancialReceivableSettled, It.IsAny<IDictionary<string, object?>>(), It.IsAny<CancellationToken>()), Times.Once);
            notifications.Verify(item => item.Create(It.IsAny<Archon.Core.Notifications.CreateNotificationRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task MarkAsPaid_should_throw_when_account_not_found()
        {
            FinancialAccount account = await SeedAccountAsync();
            FinancialEntry entry = new(
                account.Id, FinancialEntryType.Receivable, FinancialEntryCategory.BrandReceivable,
                "x", 100m, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            db.Add(entry);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            Func<Task> act = () => service.MarkAsPaid(entry.Id, new MarkAsPaidRequest { AccountId = 99 });
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task MarkAsPaid_should_throw_when_entry_not_found()
        {
            FinancialAccount account = await SeedAccountAsync();
            Func<Task> act = () => service.MarkAsPaid(99, new MarkAsPaidRequest { AccountId = account.Id });
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task GetEntryById_should_return_null_when_not_found()
        {
            FinancialEntry? result = await service.GetEntryById(99);

            result.Should().BeNull();
        }

        [Test]
        public async Task GetEntryById_should_return_entry_when_found()
        {
            FinancialAccount account = await SeedAccountAsync();
            FinancialEntry entry = new(account.Id, FinancialEntryType.Receivable, FinancialEntryCategory.BrandReceivable, "x", 100m, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            db.Add(entry);
            await db.SaveChangesAsync();

            FinancialEntry? result = await service.GetEntryById(entry.Id);

            result.Should().NotBeNull();
        }

        [Test]
        public async Task GetByCampaign_should_filter_by_campaign_id()
        {
            FinancialAccount account = await SeedAccountAsync();
            db.Add(new FinancialEntry(account.Id, FinancialEntryType.Receivable, FinancialEntryCategory.BrandReceivable, "a", 100m, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, campaignId: 10));
            db.Add(new FinancialEntry(account.Id, FinancialEntryType.Receivable, FinancialEntryCategory.BrandReceivable, "b", 100m, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, campaignId: 20));
            await db.SaveChangesAsync();

            List<FinancialEntry> result = await service.GetByCampaign(10);

            result.Should().ContainSingle(item => item.Description == "a");
        }

        [Test]
        public async Task UpdateEntry_should_throw_when_not_found()
        {
            FinancialAccount account = await SeedAccountAsync();
            UpdateFinancialEntryRequest request = new()
            {
                Id = 99,
                AccountId = account.Id,
                Type = FinancialEntryType.Receivable,
                Category = FinancialEntryCategory.BrandReceivable,
                Description = "x",
                Amount = 100m,
                DueAt = DateTimeOffset.UtcNow,
                OccurredAt = DateTimeOffset.UtcNow,
                Status = FinancialEntryStatus.Pending
            };

            Func<Task> act = () => service.UpdateEntry(99, request);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("record.notFound");
        }

        [Test]
        public async Task UpdateEntry_should_persist_changes()
        {
            FinancialAccount account = await SeedAccountAsync();
            FinancialEntry entry = new(account.Id, FinancialEntryType.Receivable, FinancialEntryCategory.BrandReceivable, "old", 100m, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            db.Add(entry);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            UpdateFinancialEntryRequest request = new()
            {
                Id = entry.Id,
                AccountId = account.Id,
                Type = FinancialEntryType.Receivable,
                Category = FinancialEntryCategory.BrandReceivable,
                Description = "atualizado",
                Amount = 250m,
                DueAt = DateTimeOffset.UtcNow.AddDays(10),
                OccurredAt = DateTimeOffset.UtcNow,
                Status = FinancialEntryStatus.Pending
            };

            FinancialEntry result = await service.UpdateEntry(entry.Id, request);

            result.Description.Should().Be("atualizado");
            result.Amount.Should().Be(250m);
        }

        private async Task<FinancialEntry> SeedPaidEntryAsync(FinancialAccount account, FinancialEntryType type = FinancialEntryType.Receivable, FinancialEntryCategory category = FinancialEntryCategory.BrandReceivable, decimal amount = 500m, long? campaignId = null, long? creatorId = null)
        {
            FinancialEntry entry = new(account.Id, type, category, "lancamento", amount, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, campaignId: campaignId);
            if (creatorId.HasValue)
            {
                entry.LinkToCreator(creatorId.Value);
            }
            entry.ChangeStatus(FinancialEntryStatus.Paid, DateTimeOffset.UtcNow);
            db.Add(entry);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            return entry;
        }

        [Test]
        public async Task GetSummary_should_compute_overdue_by_due_date_without_writing()
        {
            FinancialAccount account = await SeedAccountAsync();
            FinancialEntry overdue = new(account.Id, FinancialEntryType.Receivable, FinancialEntryCategory.BrandReceivable, "vencido", 100m, DateTimeOffset.UtcNow.AddDays(-5), DateTimeOffset.UtcNow);
            FinancialEntry upcoming = new(account.Id, FinancialEntryType.Receivable, FinancialEntryCategory.BrandReceivable, "a vencer", 50m, DateTimeOffset.UtcNow.AddDays(5), DateTimeOffset.UtcNow);
            FinancialEntry paid = new(account.Id, FinancialEntryType.Receivable, FinancialEntryCategory.BrandReceivable, "pago", 200m, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            paid.ChangeStatus(FinancialEntryStatus.Paid, DateTimeOffset.UtcNow);
            db.AddRange(overdue, upcoming, paid);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            FinancialSummaryModel summary = await service.GetSummary(FinancialEntryType.Receivable);

            summary.TotalOverdue.Should().Be(100m);
            summary.OverdueCount.Should().Be(1);
            summary.TotalPending.Should().Be(50m);
            summary.PendingCount.Should().Be(1);
            summary.TotalSettledThisMonth.Should().Be(200m);

            FinancialEntry reloaded = await db.Set<FinancialEntry>().AsNoTracking().FirstAsync(item => item.Id == overdue.Id);
            reloaded.Status.Should().Be(FinancialEntryStatus.Pending);
        }

        [Test]
        public async Task GetSummary_should_exclude_reversed_pair_from_settled_total()
        {
            FinancialAccount account = await SeedAccountAsync();
            FinancialEntry original = await SeedPaidEntryAsync(account, FinancialEntryType.Receivable, FinancialEntryCategory.BrandReceivable, 700m);
            await service.ReverseEntry(original.Id, new ReverseFinancialEntryRequest { Reason = "estorno" });

            FinancialSummaryModel receivable = await service.GetSummary(FinancialEntryType.Receivable);
            FinancialSummaryModel payable = await service.GetSummary(FinancialEntryType.Payable);

            receivable.TotalSettledThisMonth.Should().Be(0m);
            payable.TotalSettledThisMonth.Should().Be(0m);
        }

        [Test]
        public async Task ReverseEntry_should_create_contra_entry_and_mark_original_reversed()
        {
            FinancialAccount account = await SeedAccountAsync();
            FinancialEntry original = await SeedPaidEntryAsync(account);

            ReverseEntryResult result = await service.ReverseEntry(original.Id, new ReverseFinancialEntryRequest { Reason = "erro de digitacao" });

            result.Reversal.Type.Should().Be(FinancialEntryType.Payable);
            result.Reversal.Amount.Should().Be(500m);
            result.Reversal.ReversalOfEntryId.Should().Be(original.Id);
            result.CreatorPaymentAlreadyPaid.Should().BeFalse();

            FinancialEntry reloaded = await db.Set<FinancialEntry>().AsNoTracking().FirstAsync(item => item.Id == original.Id);
            reloaded.IsReversed.Should().BeTrue();
        }

        [Test]
        public async Task ReverseEntry_should_throw_when_entry_not_paid()
        {
            FinancialAccount account = await SeedAccountAsync();
            FinancialEntry pending = new(account.Id, FinancialEntryType.Receivable, FinancialEntryCategory.BrandReceivable, "x", 100m, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            db.Add(pending);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            Func<Task> act = () => service.ReverseEntry(pending.Id, new ReverseFinancialEntryRequest());

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("financialEntry.onlyPaidCanBeReversed");
        }

        [Test]
        public async Task ReverseEntry_should_flag_when_creator_payment_already_paid()
        {
            Brand brand = new("Acme");
            db.Add(brand);
            Creator creator = new("Foo", pixKey: "foo@x", pixKeyType: PixKeyType.Email);
            db.Add(creator);
            await db.SaveChangesAsync();
            Campaign campaign = new(brand.Id, "C", 0m, DateTimeOffset.UtcNow);
            db.Add(campaign);
            await db.SaveChangesAsync();
            CampaignCreator cc = new(campaign.Id, creator.Id, 1, 100m, 10m);
            db.Add(cc);
            await db.SaveChangesAsync();
            CreatorPayment payment = new(cc.Id, creator.Id, 100m, 0m, PaymentMethod.Pix);
            payment.MarkPaid(DateTimeOffset.UtcNow);
            db.Add(payment);
            FinancialAccount account = await SeedAccountAsync();
            FinancialEntry payout = await SeedPaidEntryAsync(account, FinancialEntryType.Payable, FinancialEntryCategory.CreatorPayout, 100m, campaign.Id, creator.Id);
            await db.SaveChangesAsync();

            ReverseEntryResult result = await service.ReverseEntry(payout.Id, new ReverseFinancialEntryRequest());

            result.CreatorPaymentAlreadyPaid.Should().BeTrue();
        }

        [Test]
        public async Task CreateEntry_should_block_duplicate_manual_creator_payout()
        {
            Brand brand = new("Acme");
            db.Add(brand);
            Creator creator = new("Foo");
            db.Add(creator);
            await db.SaveChangesAsync();
            Campaign campaign = new(brand.Id, "C", 0m, DateTimeOffset.UtcNow);
            db.Add(campaign);
            await db.SaveChangesAsync();
            FinancialAccount account = await SeedAccountAsync();
            await SeedPaidEntryAsync(account, FinancialEntryType.Payable, FinancialEntryCategory.CreatorPayout, 100m, campaign.Id, creator.Id);

            CreateFinancialEntryRequest request = new()
            {
                AccountId = account.Id,
                CampaignId = campaign.Id,
                CreatorId = creator.Id,
                Type = FinancialEntryType.Payable,
                Category = FinancialEntryCategory.CreatorPayout,
                Description = "repasse manual duplicado",
                Amount = 100m,
                DueAt = DateTimeOffset.UtcNow,
                OccurredAt = DateTimeOffset.UtcNow,
                Status = FinancialEntryStatus.Pending,
            };

            Func<Task> act = () => service.CreateEntry(request);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("financialEntry.duplicateCreatorPayout");
        }

        [Test]
        public async Task CreateEntry_should_link_creator_and_allow_first_creator_payout()
        {
            Brand brand = new("Acme");
            db.Add(brand);
            Creator creator = new("Foo");
            db.Add(creator);
            await db.SaveChangesAsync();
            Campaign campaign = new(brand.Id, "C", 0m, DateTimeOffset.UtcNow);
            db.Add(campaign);
            await db.SaveChangesAsync();
            FinancialAccount account = await SeedAccountAsync();

            CreateFinancialEntryRequest request = new()
            {
                AccountId = account.Id,
                CampaignId = campaign.Id,
                CreatorId = creator.Id,
                Type = FinancialEntryType.Payable,
                Category = FinancialEntryCategory.CreatorPayout,
                Description = "primeiro repasse manual",
                Amount = 100m,
                DueAt = DateTimeOffset.UtcNow,
                OccurredAt = DateTimeOffset.UtcNow,
                Status = FinancialEntryStatus.Pending,
            };

            FinancialEntry created = await service.CreateEntry(request);

            created.CreatorId.Should().Be(creator.Id);
        }

        private async Task SeedClosedPeriodAsync(int year, int month)
        {
            FinancialPeriod period = new(year, month);
            period.Close(1);
            db.Add(period);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
        }

        [Test]
        public async Task CreateEntry_should_throw_when_period_is_closed()
        {
            FinancialAccount account = await SeedAccountAsync();
            DateTimeOffset now = DateTimeOffset.UtcNow;
            await SeedClosedPeriodAsync(now.Year, now.Month);

            Func<Task> act = () => service.CreateEntry(BuildCreateRequest(account.Id));

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("financialPeriod.closed");
        }

        [Test]
        public async Task MarkAsPaid_should_throw_when_paid_period_is_closed()
        {
            FinancialAccount account = await SeedAccountAsync();
            FinancialEntry entry = new(account.Id, FinancialEntryType.Receivable, FinancialEntryCategory.BrandReceivable, "x", 100m, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            db.Add(entry);
            await db.SaveChangesAsync();
            DateTimeOffset now = DateTimeOffset.UtcNow;
            await SeedClosedPeriodAsync(now.Year, now.Month);

            Func<Task> act = () => service.MarkAsPaid(entry.Id, new MarkAsPaidRequest { AccountId = account.Id, PaidAt = DateTimeOffset.UtcNow });

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("financialPeriod.closed");
        }

        [Test]
        public async Task ReverseEntry_should_be_allowed_even_with_a_closed_period()
        {
            FinancialAccount account = await SeedAccountAsync();
            FinancialEntry original = await SeedPaidEntryAsync(account);
            DateTimeOffset now = DateTimeOffset.UtcNow;
            await SeedClosedPeriodAsync(now.Year, now.Month);

            ReverseEntryResult result = await service.ReverseEntry(original.Id, new ReverseFinancialEntryRequest());

            result.Reversal.Should().NotBeNull();
        }
    }
}
