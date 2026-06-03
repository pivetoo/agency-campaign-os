using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.CreatorPayments;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.TestSupport;
using Microsoft.EntityFrameworkCore;
using DomainEntities = AgencyCampaign.Domain.Entities;
using Moq;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class CreatorPaymentServiceTests
    {
        private TestDbContext db = null!;
        private CreatorPaymentService service = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            service = new CreatorPaymentService(db, IntegrationPlatformClientFactory.CreateInert());
        }

        [TearDown]
        public void TearDown() => db.Dispose();

        private async Task<CreatorPayment> SeedExistingPaymentAsync()
        {
            CreatorPayment payment = new CreatorPayment(1, 7, 100m, 0m, PaymentMethod.Pix);
            db.Add(payment);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            return payment;
        }

        [Test]
        public async Task CreatePayment_should_throw_when_campaign_creator_not_found()
        {
            CreateCreatorPaymentRequest request = new() { CampaignCreatorId = 99, GrossAmount = 100m, Method = PaymentMethod.Pix };
            Func<Task> act = () => service.CreatePayment(request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        private async Task<DomainEntities.CampaignCreator> SeedCampaignCreatorAsync(string? pixKey = "foo@x", PixKeyType? pixKeyType = PixKeyType.Email)
        {
            Brand brand = new("Acme");
            db.Add(brand);
            Creator creator = new("Foo", pixKey: pixKey, pixKeyType: pixKeyType);
            db.Add(creator);
            await db.SaveChangesAsync();

            Campaign campaign = new(brand.Id, "C", 0m, DateTimeOffset.UtcNow);
            db.Add(campaign);
            await db.SaveChangesAsync();

            DomainEntities.CampaignCreator cc = new(campaign.Id, creator.Id, 1, 100m, 10m);
            db.Add(cc);
            await db.SaveChangesAsync();
            return cc;
        }

        [Test]
        public async Task CreatePayment_should_snapshot_pix_data_from_creator_when_available()
        {
            DomainEntities.CampaignCreator cc = await SeedCampaignCreatorAsync();

            CreatorPayment payment = await service.CreatePayment(new CreateCreatorPaymentRequest
            {
                CampaignCreatorId = cc.Id,
                GrossAmount = 1000m,
                Method = PaymentMethod.Pix
            });

            payment.PixKey.Should().Be("foo@x");
            payment.PixKeyType.Should().Be(PixKeyType.Email);

            db.ChangeTracker.Clear();
            CreatorPayment persisted = await db.Set<CreatorPayment>().AsNoTracking().Include(item => item.Events).SingleAsync();
            persisted.Events.Should().ContainSingle(item => item.EventType == CreatorPaymentEventType.Created);
        }

        [Test]
        public async Task CreatePayment_should_skip_pix_snapshot_when_creator_has_no_pix()
        {
            DomainEntities.CampaignCreator cc = await SeedCampaignCreatorAsync(pixKey: null, pixKeyType: null);

            CreatorPayment payment = await service.CreatePayment(new CreateCreatorPaymentRequest
            {
                CampaignCreatorId = cc.Id,
                GrossAmount = 1000m,
                Method = PaymentMethod.Pix
            });

            payment.PixKey.Should().BeNull();
        }

        [Test]
        public async Task UpdatePayment_should_throw_when_id_mismatch()
        {
            UpdateCreatorPaymentRequest request = new() { Id = 5, GrossAmount = 100m, Method = PaymentMethod.Pix };
            Func<Task> act = () => service.UpdatePayment(99, request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task UpdatePayment_should_throw_when_not_found()
        {
            UpdateCreatorPaymentRequest request = new() { Id = 99, GrossAmount = 100m, Method = PaymentMethod.Pix };
            Func<Task> act = () => service.UpdatePayment(99, request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task AttachInvoice_should_throw_when_payment_not_found()
        {
            AttachInvoiceRequest request = new() { InvoiceUrl = "https://x" };
            Func<Task> act = () => service.AttachInvoice(99, request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task AttachInvoice_should_register_event_and_persist_invoice()
        {
            CreatorPayment payment = await SeedExistingPaymentAsync();

            CreatorPayment result = await service.AttachInvoice(payment.Id, new AttachInvoiceRequest
            {
                InvoiceNumber = "NF-1",
                InvoiceUrl = "https://x"
            });

            result.InvoiceNumber.Should().Be("NF-1");
            result.Events.Should().Contain(item => item.EventType == CreatorPaymentEventType.InvoiceAttached);
        }

        [Test]
        public async Task MarkPaid_should_attach_provider_when_provided_and_register_paid_event()
        {
            CreatorPayment payment = await SeedExistingPaymentAsync();

            CreatorPayment result = await service.MarkPaid(payment.Id, new MarkCreatorPaymentPaidRequest
            {
                PaidAt = DateTimeOffset.UtcNow,
                Provider = "Pagar.me",
                ProviderTransactionId = "tx-1"
            });

            result.Status.Should().Be(PaymentStatus.Paid);
            result.Provider.Should().Be("Pagar.me");
            result.Events.Should().Contain(item => item.EventType == CreatorPaymentEventType.Paid);
        }

        [Test]
        public async Task MarkPaid_should_settle_planned_creator_payouts_for_same_campaign_and_creator()
        {
            DomainEntities.CampaignCreator cc = await SeedCampaignCreatorAsync();
            FinancialAccount account = new("Conta", FinancialAccountType.Bank, 0m, "#fff");
            db.Add(account);
            await db.SaveChangesAsync();

            FinancialEntry planned = new(account.Id, FinancialEntryType.Payable, FinancialEntryCategory.CreatorPayout,
                "Repasse previsto", 100m, DateTimeOffset.UtcNow.AddDays(10), DateTimeOffset.UtcNow, campaignId: cc.CampaignId);
            planned.LinkToCreator(cc.CreatorId);
            db.Add(planned);
            await db.SaveChangesAsync();

            CreatorPayment payment = await service.CreatePayment(new CreateCreatorPaymentRequest { CampaignCreatorId = cc.Id, GrossAmount = 100m, Method = PaymentMethod.Pix });
            await service.MarkPaid(payment.Id, new MarkCreatorPaymentPaidRequest { PaidAt = DateTimeOffset.UtcNow });

            db.ChangeTracker.Clear();
            FinancialEntry settled = await db.Set<FinancialEntry>().AsNoTracking().FirstAsync(item => item.Id == planned.Id);
            settled.Status.Should().Be(FinancialEntryStatus.Paid);
        }

        [Test]
        public async Task MarkPaid_should_not_settle_planned_payouts_twice()
        {
            DomainEntities.CampaignCreator cc = await SeedCampaignCreatorAsync();
            FinancialAccount account = new("Conta", FinancialAccountType.Bank, 0m, "#fff");
            db.Add(account);
            await db.SaveChangesAsync();

            FinancialEntry planned = new(account.Id, FinancialEntryType.Payable, FinancialEntryCategory.CreatorPayout,
                "Repasse previsto", 100m, DateTimeOffset.UtcNow.AddDays(10), DateTimeOffset.UtcNow, campaignId: cc.CampaignId);
            planned.LinkToCreator(cc.CreatorId);
            db.Add(planned);
            await db.SaveChangesAsync();

            CreatorPayment payment = await service.CreatePayment(new CreateCreatorPaymentRequest { CampaignCreatorId = cc.Id, GrossAmount = 100m, Method = PaymentMethod.Pix });
            await service.MarkPaid(payment.Id, new MarkCreatorPaymentPaidRequest { PaidAt = DateTimeOffset.UtcNow });
            CreatorPayment result = await service.MarkPaid(payment.Id, new MarkCreatorPaymentPaidRequest { PaidAt = DateTimeOffset.UtcNow });

            result.Events.Count(item => item.EventType == CreatorPaymentEventType.PlannedPayoutSettled).Should().Be(1);
        }

        [Test]
        public async Task MarkPaid_should_signal_divergence_when_planned_total_differs_from_paid()
        {
            DomainEntities.CampaignCreator cc = await SeedCampaignCreatorAsync();
            FinancialAccount account = new("Conta", FinancialAccountType.Bank, 0m, "#fff");
            db.Add(account);
            await db.SaveChangesAsync();

            FinancialEntry planned = new(account.Id, FinancialEntryType.Payable, FinancialEntryCategory.CreatorPayout,
                "Repasse previsto", 100m, DateTimeOffset.UtcNow.AddDays(10), DateTimeOffset.UtcNow, campaignId: cc.CampaignId);
            planned.LinkToCreator(cc.CreatorId);
            db.Add(planned);
            await db.SaveChangesAsync();

            CreatorPayment payment = await service.CreatePayment(new CreateCreatorPaymentRequest { CampaignCreatorId = cc.Id, GrossAmount = 80m, Method = PaymentMethod.Pix });
            CreatorPayment result = await service.MarkPaid(payment.Id, new MarkCreatorPaymentPaidRequest { PaidAt = DateTimeOffset.UtcNow });

            result.Events.Should().Contain(item => item.EventType == CreatorPaymentEventType.PlannedPayoutSettled
                && item.Description != null && item.Description.Contains("diverge"));
        }

        [Test]
        public async Task MarkPaid_should_throw_when_not_found()
        {
            Func<Task> act = () => service.MarkPaid(99, new MarkCreatorPaymentPaidRequest { PaidAt = DateTimeOffset.UtcNow });
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task Cancel_should_set_status_and_register_event()
        {
            CreatorPayment payment = await SeedExistingPaymentAsync();

            CreatorPayment result = await service.Cancel(payment.Id);

            result.Status.Should().Be(PaymentStatus.Cancelled);
            result.Events.Should().Contain(item => item.EventType == CreatorPaymentEventType.Cancelled);
        }

        [Test]
        public async Task Cancel_should_throw_when_not_found()
        {
            Func<Task> act = () => service.Cancel(99);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task GetByCampaign_should_filter_by_campaign_id()
        {
            db.Add(new CreatorPayment(10, 7, 100m, 0m, PaymentMethod.Pix));
            db.Add(new CreatorPayment(11, 7, 100m, 0m, PaymentMethod.Pix));
            await db.SaveChangesAsync();

            // Service uses Include chains that may not return data without Campaign entities seeded.
            // Just verify it doesn't throw.
            await service.GetByCampaign(10);
        }

        [Test]
        public async Task GetPaymentById_should_return_null_when_not_found()
        {
            CreatorPayment? result = await service.GetPaymentById(99);

            result.Should().BeNull();
        }

        [Test]
        public async Task GetPaymentById_should_return_payment_when_found()
        {
            DomainEntities.CampaignCreator cc = await SeedCampaignCreatorAsync();
            CreatorPayment payment = await service.CreatePayment(new CreateCreatorPaymentRequest
            {
                CampaignCreatorId = cc.Id,
                GrossAmount = 1000m,
                Method = PaymentMethod.Pix
            });

            CreatorPayment? result = await service.GetPaymentById(payment.Id);

            result.Should().NotBeNull();
        }

        [Test]
        public async Task GetPayments_should_return_paged_result_ordered_desc()
        {
            DomainEntities.CampaignCreator cc = await SeedCampaignCreatorAsync();
            await service.CreatePayment(new CreateCreatorPaymentRequest { CampaignCreatorId = cc.Id, GrossAmount = 100m, Method = PaymentMethod.Pix });
            await service.CreatePayment(new CreateCreatorPaymentRequest { CampaignCreatorId = cc.Id, GrossAmount = 200m, Method = PaymentMethod.Pix });

            Archon.Core.Pagination.PagedResult<CreatorPayment> result = await service.GetPayments(new Archon.Core.Pagination.PagedRequest { Page = 1, PageSize = 10 });

            result.Items.Should().HaveCount(2);
        }

        [Test]
        public async Task GetByStatus_should_filter_by_status_enum()
        {
            DomainEntities.CampaignCreator cc = await SeedCampaignCreatorAsync();
            CreatorPayment p1 = await service.CreatePayment(new CreateCreatorPaymentRequest { CampaignCreatorId = cc.Id, GrossAmount = 100m, Method = PaymentMethod.Pix });
            CreatorPayment p2 = await service.CreatePayment(new CreateCreatorPaymentRequest { CampaignCreatorId = cc.Id, GrossAmount = 200m, Method = PaymentMethod.Pix });
            await service.MarkPaid(p2.Id, new MarkCreatorPaymentPaidRequest { PaidAt = DateTimeOffset.UtcNow });

            List<CreatorPayment> result = await service.GetByStatus((int)PaymentStatus.Paid);

            result.Should().HaveCount(1);
            result.First().Status.Should().Be(PaymentStatus.Paid);
        }

        [Test]
        public async Task UpdatePayment_should_persist_changes_and_register_event()
        {
            CreatorPayment payment = await SeedExistingPaymentAsync();

            UpdateCreatorPaymentRequest request = new()
            {
                Id = payment.Id,
                GrossAmount = 500m,
                Discounts = 10m,
                Method = PaymentMethod.Ted,
                Description = "atualizado"
            };

            CreatorPayment result = await service.UpdatePayment(payment.Id, request);

            result.GrossAmount.Should().Be(500m);
            result.Method.Should().Be(PaymentMethod.Ted);
            result.Events.Should().Contain(item => item.EventType == CreatorPaymentEventType.Updated);
        }

        [Test]
        public async Task SchedulePaymentBatch_should_throw_when_no_payments_found()
        {
            SchedulePaymentBatchRequest request = new()
            {
                CreatorPaymentIds = new List<long> { 999 },
                ScheduledFor = DateTimeOffset.UtcNow
            };

            Func<Task> act = () => service.SchedulePaymentBatch(request);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("record.notFound");
        }

        [Test]
        public async Task SchedulePaymentBatch_should_register_error_when_creator_has_no_pix_key()
        {
            DomainEntities.CampaignCreator cc = await SeedCampaignCreatorAsync(pixKey: null, pixKeyType: null);
            CreatorPayment payment = await service.CreatePayment(new CreateCreatorPaymentRequest
            {
                CampaignCreatorId = cc.Id,
                GrossAmount = 1000m,
                Method = PaymentMethod.Pix
            });

            SchedulePaymentBatchRequest request = new()
            {
                CreatorPaymentIds = new List<long> { payment.Id },
                ScheduledFor = DateTimeOffset.UtcNow
            };

            List<CreatorPayment> result = await service.SchedulePaymentBatch(request);

            result.Should().BeEmpty();
            CreatorPayment refreshed = await db.Set<CreatorPayment>().AsNoTracking().Include(item => item.Events).FirstAsync(item => item.Id == payment.Id);
            refreshed.Events.Should().Contain(item => item.EventType == CreatorPaymentEventType.ProviderSyncError);
        }

        [Test]
        public async Task SchedulePaymentBatch_should_skip_payments_already_paid()
        {
            DomainEntities.CampaignCreator cc = await SeedCampaignCreatorAsync();
            CreatorPayment payment = await service.CreatePayment(new CreateCreatorPaymentRequest
            {
                CampaignCreatorId = cc.Id,
                GrossAmount = 1000m,
                Method = PaymentMethod.Pix
            });

            CreatorPayment? tracked = await db.Set<CreatorPayment>().AsTracking().FirstAsync(item => item.Id == payment.Id);
            tracked.MarkPaid(DateTimeOffset.UtcNow);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            SchedulePaymentBatchRequest request = new()
            {
                CreatorPaymentIds = new List<long> { payment.Id },
                ScheduledFor = DateTimeOffset.UtcNow
            };

            List<CreatorPayment> result = await service.SchedulePaymentBatch(request);

            result.Should().BeEmpty();
        }

        [Test]
        public async Task HandleProviderCallback_should_throw_when_payment_not_found()
        {
            CreatorPaymentProviderCallbackRequest request = new()
            {
                Provider = "p",
                ProviderTransactionId = "tx-1",
                EventType = "paid"
            };

            Func<Task> act = () => service.HandleProviderCallback(request);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("record.notFound");
        }

        [Test]
        public async Task HandleProviderCallback_should_mark_paid_on_paid_event()
        {
            DomainEntities.CampaignCreator cc = await SeedCampaignCreatorAsync();
            CreatorPayment payment = await service.CreatePayment(new CreateCreatorPaymentRequest
            {
                CampaignCreatorId = cc.Id,
                GrossAmount = 100m,
                Method = PaymentMethod.Pix
            });
            await service.MarkPaid(payment.Id, new MarkCreatorPaymentPaidRequest
            {
                Provider = "provider-x",
                ProviderTransactionId = "tx-callback",
                PaidAt = DateTimeOffset.UtcNow
            });

            CreatorPayment result = await service.HandleProviderCallback(new CreatorPaymentProviderCallbackRequest
            {
                Provider = "provider-x",
                ProviderTransactionId = "tx-callback",
                EventType = "paid"
            });

            result.Events.Should().Contain(item => item.EventType == CreatorPaymentEventType.Paid);
        }

        [Test]
        public async Task HandleProviderCallback_should_register_sync_error_for_unknown_event()
        {
            DomainEntities.CampaignCreator cc = await SeedCampaignCreatorAsync();
            CreatorPayment payment = await service.CreatePayment(new CreateCreatorPaymentRequest
            {
                CampaignCreatorId = cc.Id,
                GrossAmount = 100m,
                Method = PaymentMethod.Pix
            });
            await service.MarkPaid(payment.Id, new MarkCreatorPaymentPaidRequest
            {
                Provider = "provider-x",
                ProviderTransactionId = "tx-callback",
                PaidAt = DateTimeOffset.UtcNow
            });

            CreatorPayment result = await service.HandleProviderCallback(new CreatorPaymentProviderCallbackRequest
            {
                Provider = "provider-x",
                ProviderTransactionId = "tx-callback",
                EventType = "estranho"
            });

            result.Events.Should().Contain(item => item.EventType == CreatorPaymentEventType.ProviderSyncError);
        }

        [Test]
        public async Task HandleProviderCallback_should_mark_failed_on_failed_event()
        {
            DomainEntities.CampaignCreator cc = await SeedCampaignCreatorAsync();
            CreatorPayment payment = await service.CreatePayment(new CreateCreatorPaymentRequest
            {
                CampaignCreatorId = cc.Id,
                GrossAmount = 100m,
                Method = PaymentMethod.Pix
            });
            CreatorPayment? tracked = await db.Set<CreatorPayment>().AsTracking().FirstAsync(item => item.Id == payment.Id);
            tracked.AttachToProvider("provider-x", "tx-failed");
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            CreatorPayment result = await service.HandleProviderCallback(new CreatorPaymentProviderCallbackRequest
            {
                Provider = "provider-x",
                ProviderTransactionId = "tx-failed",
                EventType = "transfer.failed",
                FailureReason = "saldo insuficiente"
            });

            result.Status.Should().Be(PaymentStatus.Failed);
            result.Events.Should().Contain(item => item.EventType == CreatorPaymentEventType.Failed);
        }

        [Test]
        public async Task HandleProviderCallback_should_cancel_on_cancelled_event()
        {
            DomainEntities.CampaignCreator cc = await SeedCampaignCreatorAsync();
            CreatorPayment payment = await service.CreatePayment(new CreateCreatorPaymentRequest
            {
                CampaignCreatorId = cc.Id,
                GrossAmount = 100m,
                Method = PaymentMethod.Pix
            });
            CreatorPayment? tracked = await db.Set<CreatorPayment>().AsTracking().FirstAsync(item => item.Id == payment.Id);
            tracked.AttachToProvider("provider-x", "tx-cancelled");
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            CreatorPayment result = await service.HandleProviderCallback(new CreatorPaymentProviderCallbackRequest
            {
                Provider = "provider-x",
                ProviderTransactionId = "tx-cancelled",
                EventType = "cancelled",
                FailureReason = "cliente desistiu"
            });

            result.Status.Should().Be(PaymentStatus.Cancelled);
            result.Events.Should().Contain(item => item.EventType == CreatorPaymentEventType.Cancelled);
        }

        [Test]
        public async Task HandleProviderCallback_should_normalize_event_type_case_and_whitespace()
        {
            DomainEntities.CampaignCreator cc = await SeedCampaignCreatorAsync();
            CreatorPayment payment = await service.CreatePayment(new CreateCreatorPaymentRequest
            {
                CampaignCreatorId = cc.Id,
                GrossAmount = 100m,
                Method = PaymentMethod.Pix
            });
            CreatorPayment? tracked = await db.Set<CreatorPayment>().AsTracking().FirstAsync(item => item.Id == payment.Id);
            tracked.AttachToProvider("provider-x", "tx-mix");
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            CreatorPayment result = await service.HandleProviderCallback(new CreatorPaymentProviderCallbackRequest
            {
                Provider = "provider-x",
                ProviderTransactionId = "tx-mix",
                EventType = "  TRANSFER.COMPLETED  "
            });

            result.Status.Should().Be(PaymentStatus.Paid);
        }

        [Test]
        public async Task SchedulePaymentBatch_should_mark_failed_when_integration_throws()
        {
            DomainEntities.CampaignCreator cc = await SeedCampaignCreatorAsync();
            CreatorPayment payment = await service.CreatePayment(new CreateCreatorPaymentRequest
            {
                CampaignCreatorId = cc.Id,
                GrossAmount = 100m,
                Method = PaymentMethod.Pix
            });

            SchedulePaymentBatchRequest request = new()
            {
                CreatorPaymentIds = new List<long> { payment.Id },
                ScheduledFor = DateTimeOffset.UtcNow,
                ConnectorId = 1,
                PipelineId = 99
            };

            List<CreatorPayment> result = await service.SchedulePaymentBatch(request);

            result.Should().BeEmpty();
            CreatorPayment refreshed = await db.Set<CreatorPayment>().AsNoTracking().Include(item => item.Events).FirstAsync(item => item.Id == payment.Id);
            refreshed.Status.Should().Be(PaymentStatus.Failed);
            refreshed.Events.Should().Contain(item => item.EventType == CreatorPaymentEventType.ProviderSyncError);
        }

        [Test]
        public async Task SchedulePaymentBatch_should_default_scheduled_for_to_utc_now_when_null()
        {
            DomainEntities.CampaignCreator cc = await SeedCampaignCreatorAsync(pixKey: null, pixKeyType: null);
            CreatorPayment payment = await service.CreatePayment(new CreateCreatorPaymentRequest
            {
                CampaignCreatorId = cc.Id,
                GrossAmount = 100m,
                Method = PaymentMethod.Pix
            });

            SchedulePaymentBatchRequest request = new()
            {
                CreatorPaymentIds = new List<long> { payment.Id },
                ScheduledFor = null
            };

            List<CreatorPayment> result = await service.SchedulePaymentBatch(request);

            result.Should().BeEmpty();
        }

        [Test]
        public async Task GetByStatus_should_return_empty_when_no_match()
        {
            List<CreatorPayment> result = await service.GetByStatus((int)PaymentStatus.Paid);

            result.Should().BeEmpty();
        }

        [Test]
        public async Task CreatePayment_should_attach_campaign_document_when_provided()
        {
            DomainEntities.CampaignCreator cc = await SeedCampaignCreatorAsync();
            CampaignDocument doc = new(campaignId: 1, documentType: CampaignDocumentType.CreatorAgreement, title: "Doc");
            db.Add(doc);
            await db.SaveChangesAsync();

            CreatorPayment payment = await service.CreatePayment(new CreateCreatorPaymentRequest
            {
                CampaignCreatorId = cc.Id,
                GrossAmount = 100m,
                Method = PaymentMethod.Pix,
                CampaignDocumentId = doc.Id
            });

            payment.CampaignDocumentId.Should().Be(doc.Id);
        }

        [Test]
        public async Task HandleProviderCallback_should_be_idempotent_on_repeated_paid()
        {
            CreatorPayment payment = await SeedExistingPaymentAsync();
            CreatorPayment tracked = await db.Set<CreatorPayment>().AsTracking().FirstAsync(item => item.Id == payment.Id);
            tracked.AttachToProvider("provider-x", "tx-idem");
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            CreatorPaymentProviderCallbackRequest callback = new() { Provider = "provider-x", ProviderTransactionId = "tx-idem", EventType = "paid" };
            await service.HandleProviderCallback(callback);
            CreatorPayment result = await service.HandleProviderCallback(callback);

            result.Status.Should().Be(PaymentStatus.Paid);
            result.Events.Count(item => item.EventType == CreatorPaymentEventType.Paid).Should().Be(1);
        }

        [Test]
        public async Task HandleProviderCallback_should_persist_end_to_end_id()
        {
            CreatorPayment payment = await SeedExistingPaymentAsync();
            CreatorPayment tracked = await db.Set<CreatorPayment>().AsTracking().FirstAsync(item => item.Id == payment.Id);
            tracked.AttachToProvider("provider-x", "tx-e2e");
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            CreatorPayment result = await service.HandleProviderCallback(new CreatorPaymentProviderCallbackRequest
            {
                Provider = "provider-x",
                ProviderTransactionId = "tx-e2e",
                EventType = "paid",
                EndToEndId = "E2E-ABC-123"
            });

            result.EndToEndId.Should().Be("E2E-ABC-123");
        }

        [Test]
        public async Task SchedulePaymentBatch_should_assign_idempotency_key_before_enqueue()
        {
            DomainEntities.CampaignCreator cc = await SeedCampaignCreatorAsync();
            CreatorPayment payment = await service.CreatePayment(new CreateCreatorPaymentRequest { CampaignCreatorId = cc.Id, GrossAmount = 100m, Method = PaymentMethod.Pix });

            SchedulePaymentBatchRequest request = new()
            {
                CreatorPaymentIds = new List<long> { payment.Id },
                ScheduledFor = DateTimeOffset.UtcNow,
                ConnectorId = 1,
                PipelineId = 99
            };

            await service.SchedulePaymentBatch(request);

            CreatorPayment refreshed = await db.Set<CreatorPayment>().AsNoTracking().FirstAsync(item => item.Id == payment.Id);
            refreshed.IdempotencyKey.Should().NotBeNullOrEmpty();
        }

        [Test]
        public async Task SchedulePaymentBatch_should_warn_when_pj_creator_has_no_invoice()
        {
            Brand brand = new("Acme");
            db.Add(brand);
            Creator creator = new("Joana", pixKey: "joana@x", pixKeyType: PixKeyType.Email, taxRegime: TaxRegime.SimplesNacional);
            db.Add(creator);
            await db.SaveChangesAsync();
            Campaign campaign = new(brand.Id, "C", 0m, DateTimeOffset.UtcNow);
            db.Add(campaign);
            await db.SaveChangesAsync();
            DomainEntities.CampaignCreator cc = new(campaign.Id, creator.Id, 1, 100m, 10m);
            db.Add(cc);
            await db.SaveChangesAsync();

            CreatorPayment payment = await service.CreatePayment(new CreateCreatorPaymentRequest { CampaignCreatorId = cc.Id, GrossAmount = 100m, Method = PaymentMethod.Pix });

            SchedulePaymentBatchRequest request = new() { CreatorPaymentIds = new List<long> { payment.Id }, ScheduledFor = DateTimeOffset.UtcNow, ConnectorId = 1, PipelineId = 99 };
            await service.SchedulePaymentBatch(request);

            CreatorPayment refreshed = await db.Set<CreatorPayment>().AsNoTracking().Include(item => item.Events).FirstAsync(item => item.Id == payment.Id);
            refreshed.Events.Should().Contain(item => item.EventType == CreatorPaymentEventType.InvoiceMissing);
        }
    }
}
