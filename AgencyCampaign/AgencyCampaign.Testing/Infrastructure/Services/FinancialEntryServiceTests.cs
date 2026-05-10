using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.FinancialEntries;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.TestSupport;
using Archon.Application.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

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
            service = new FinancialEntryService(db, LocalizerMock.Create<AgencyCampaignResource>(), automation.Object, notifications.Object);
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
    }
}
