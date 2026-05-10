using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Models.Financial;
using AgencyCampaign.Application.Requests.FinancialAccounts;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class FinancialAccountServiceTests
    {
        private TestDbContext db = null!;
        private FinancialAccountService service = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            service = new FinancialAccountService(db, LocalizerMock.Create<AgencyCampaignResource>());
        }

        [TearDown]
        public void TearDown() => db.Dispose();

        [Test]
        public async Task Create_should_persist_account()
        {
            CreateFinancialAccountRequest request = new()
            {
                Name = "Conta principal",
                Type = FinancialAccountType.Bank,
                InitialBalance = 100m,
                Color = "#fff"
            };

            FinancialAccountModel result = await service.Create(request);

            result.Id.Should().BeGreaterThan(0);
            result.InitialBalance.Should().Be(100m);
        }

        [Test]
        public async Task GetAll_should_compute_current_balance_from_paid_entries()
        {
            FinancialAccount account = new("Conta", FinancialAccountType.Bank, 1000m, "#fff");
            db.Add(account);
            await db.SaveChangesAsync();

            FinancialEntry receivablePaid = new(account.Id, FinancialEntryType.Receivable, FinancialEntryCategory.BrandReceivable, "in", 500m, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            receivablePaid.ChangeStatus(FinancialEntryStatus.Paid, DateTimeOffset.UtcNow);

            FinancialEntry payablePaid = new(account.Id, FinancialEntryType.Payable, FinancialEntryCategory.CreatorPayout, "out", 200m, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            payablePaid.ChangeStatus(FinancialEntryStatus.Paid, DateTimeOffset.UtcNow);

            FinancialEntry receivablePending = new(account.Id, FinancialEntryType.Receivable, FinancialEntryCategory.BrandReceivable, "in pending", 999m, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

            db.Add(receivablePaid);
            db.Add(payablePaid);
            db.Add(receivablePending);
            await db.SaveChangesAsync();

            IReadOnlyCollection<FinancialAccountModel> result = await service.GetAll(includeInactive: true);

            FinancialAccountModel persisted = result.Single();
            persisted.CurrentBalance.Should().Be(1000m + 500m - 200m);
        }

        [Test]
        public async Task GetAll_should_filter_inactive_when_requested()
        {
            FinancialAccount inactive = new("Inactive", FinancialAccountType.Bank, 0m, "#fff");
            inactive.Update("Inactive", FinancialAccountType.Bank, 0m, "#fff", null, null, null, isActive: false);
            db.Add(new FinancialAccount("Active", FinancialAccountType.Bank, 0m, "#fff"));
            db.Add(inactive);
            await db.SaveChangesAsync();

            IReadOnlyCollection<FinancialAccountModel> activeOnly = await service.GetAll(includeInactive: false);
            IReadOnlyCollection<FinancialAccountModel> all = await service.GetAll(includeInactive: true);

            activeOnly.Should().ContainSingle();
            all.Should().HaveCount(2);
        }

        [Test]
        public async Task Update_should_throw_when_id_mismatch()
        {
            UpdateFinancialAccountRequest request = new() { Id = 5, Name = "x", Color = "#fff" };
            Func<Task> act = () => service.Update(99, request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task Update_should_throw_when_not_found()
        {
            UpdateFinancialAccountRequest request = new() { Id = 99, Name = "x", Color = "#fff" };
            Func<Task> act = () => service.Update(99, request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task Delete_should_throw_when_account_has_entries()
        {
            FinancialAccount account = new("Conta", FinancialAccountType.Bank, 0m, "#fff");
            db.Add(account);
            await db.SaveChangesAsync();

            FinancialEntry entry = new(account.Id, FinancialEntryType.Receivable, FinancialEntryCategory.BrandReceivable, "x", 1m, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            db.Add(entry);
            await db.SaveChangesAsync();

            Func<Task> act = () => service.Delete(account.Id);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task Delete_should_remove_account_when_no_entries_attached()
        {
            FinancialAccount account = new("Conta", FinancialAccountType.Bank, 0m, "#fff");
            db.Add(account);
            await db.SaveChangesAsync();

            await service.Delete(account.Id);

            (await db.Set<FinancialAccount>().CountAsync()).Should().Be(0);
        }
    }
}
