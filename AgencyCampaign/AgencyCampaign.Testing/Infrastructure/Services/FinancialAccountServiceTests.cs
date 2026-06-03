using AgencyCampaign.Application.Models.Financial;
using AgencyCampaign.Application.Requests.FinancialAccounts;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.TestSupport;
using Archon.Core.Pagination;
using Microsoft.EntityFrameworkCore;
using Moq;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

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
            service = new FinancialAccountService(db, IntegrationPlatformClientFactory.CreateInert());
        }

        [TearDown]
        public void TearDown() => db.Dispose();

        [Test]
        public async Task SetAsDefault_should_clear_other_defaults()
        {
            FinancialAccountModel a = await service.Create(new CreateFinancialAccountRequest { Name = "A", Type = FinancialAccountType.Bank, InitialBalance = 0m, Color = "#fff" });
            FinancialAccountModel b = await service.Create(new CreateFinancialAccountRequest { Name = "B", Type = FinancialAccountType.Bank, InitialBalance = 0m, Color = "#fff" });

            await service.SetAsDefault(a.Id);
            await service.SetAsDefault(b.Id);

            FinancialAccount refreshedA = await db.Set<FinancialAccount>().AsNoTracking().FirstAsync(item => item.Id == a.Id);
            FinancialAccount refreshedB = await db.Set<FinancialAccount>().AsNoTracking().FirstAsync(item => item.Id == b.Id);
            refreshedA.IsDefault.Should().BeFalse();
            refreshedB.IsDefault.Should().BeTrue();
        }

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

            PagedResult<FinancialAccountModel> result = await service.GetAll(new PagedRequest(), search: null, includeInactive: true);

            FinancialAccountModel persisted = result.Items.Single();
            persisted.CurrentBalance.Should().Be(1000m + 500m - 200m);
        }

        [Test]
        public async Task GetAll_should_filter_inactive_when_requested()
        {
            FinancialAccount inactive = new("Inactive", FinancialAccountType.Bank, 0m, "#fff");
            inactive.Update("Inactive", FinancialAccountType.Bank, 0m, "#fff", bankId: null, bank: null, agency: null, number: null, isActive: false);
            db.Add(new FinancialAccount("Active", FinancialAccountType.Bank, 0m, "#fff"));
            db.Add(inactive);
            await db.SaveChangesAsync();

            PagedResult<FinancialAccountModel> activeOnly = await service.GetAll(new PagedRequest(), search: null, includeInactive: false);
            PagedResult<FinancialAccountModel> all = await service.GetAll(new PagedRequest(), search: null, includeInactive: true);

            activeOnly.Items.Should().ContainSingle();
            all.Items.Should().HaveCount(2);
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

        [Test]
        public async Task Create_should_throw_when_name_already_exists_case_insensitive()
        {
            db.Add(new FinancialAccount("Conta Principal", FinancialAccountType.Bank, 0m, "#ffffff"));
            await db.SaveChangesAsync();

            CreateFinancialAccountRequest request = new()
            {
                Name = "conta PRINCIPAL",
                Type = FinancialAccountType.Cash,
                Color = "#000000"
            };

            Func<Task> act = () => service.Create(request);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("financialAccount.name.duplicated");
        }

        [Test]
        public async Task Update_should_throw_when_renaming_to_existing_account_name()
        {
            FinancialAccount first = new("Conta A", FinancialAccountType.Bank, 0m, "#ffffff");
            FinancialAccount second = new("Conta B", FinancialAccountType.Cash, 0m, "#000000");
            db.Add(first);
            db.Add(second);
            await db.SaveChangesAsync();

            UpdateFinancialAccountRequest request = new()
            {
                Id = second.Id,
                Name = "conta a",
                Type = FinancialAccountType.Cash,
                Color = "#000000",
                IsActive = true
            };

            Func<Task> act = () => service.Update(second.Id, request);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("financialAccount.name.duplicated");
        }

        [Test]
        public async Task Update_should_allow_keeping_same_name_for_same_account()
        {
            FinancialAccount account = new("Conta", FinancialAccountType.Bank, 0m, "#ffffff");
            db.Add(account);
            await db.SaveChangesAsync();

            UpdateFinancialAccountRequest request = new()
            {
                Id = account.Id,
                Name = "Conta",
                Type = FinancialAccountType.Bank,
                Color = "#ffffff",
                InitialBalance = 250m,
                IsActive = true
            };

            FinancialAccountModel result = await service.Update(account.Id, request);

            result.InitialBalance.Should().Be(250m);
        }

        [Test]
        public async Task GetById_should_return_null_when_not_found()
        {
            (await service.GetById(99)).Should().BeNull();
        }

        [Test]
        public async Task GetById_should_return_account_when_found()
        {
            FinancialAccount account = new("Conta", FinancialAccountType.Bank, 0m, "#fff");
            db.Add(account);
            await db.SaveChangesAsync();

            FinancialAccountModel? result = await service.GetById(account.Id);

            result.Should().NotBeNull();
            result!.Name.Should().Be("Conta");
        }

        [Test]
        public async Task GetAll_should_apply_search_filter()
        {
            db.Add(new FinancialAccount("Conta corrente", FinancialAccountType.Bank, 0m, "#fff"));
            db.Add(new FinancialAccount("Caixinha", FinancialAccountType.Cash, 0m, "#000"));
            await db.SaveChangesAsync();

            PagedResult<FinancialAccountModel> result = await service.GetAll(new PagedRequest(), search: "conta", includeInactive: true);

            result.Items.Should().ContainSingle(item => item.Name == "Conta corrente");
        }

        [Test]
        public async Task GetSummary_should_aggregate_account_balances()
        {
            FinancialAccount a = new("A", FinancialAccountType.Bank, 1000m, "#fff");
            FinancialAccount b = new("B", FinancialAccountType.Cash, 500m, "#000");
            db.Add(a);
            db.Add(b);
            await db.SaveChangesAsync();

            FinancialAccountSummaryModel summary = await service.GetSummary();

            summary.ActiveCount.Should().Be(2);
            summary.TotalKanvasBalance.Should().Be(1500m);
        }

        [Test]
        public async Task Delete_should_throw_when_account_not_found()
        {
            Func<Task> act = () => service.Delete(99);

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task AttachConnector_should_throw_when_account_not_found()
        {
            Func<Task> act = () => service.AttachConnector(99, 1);

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task AttachConnector_should_set_connector_id()
        {
            FinancialAccount account = new("Conta", FinancialAccountType.Bank, 0m, "#fff");
            db.Add(account);
            await db.SaveChangesAsync();

            FinancialAccountModel result = await service.AttachConnector(account.Id, 42);

            result.IntegrationConnectorId.Should().Be(42);
        }

        [Test]
        public async Task DetachConnector_should_throw_when_account_not_found()
        {
            Func<Task> act = () => service.DetachConnector(99);

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task DetachConnector_should_clear_connector_id()
        {
            FinancialAccount account = new("Conta", FinancialAccountType.Bank, 0m, "#fff");
            db.Add(account);
            await db.SaveChangesAsync();
            await service.AttachConnector(account.Id, 42);

            FinancialAccountModel result = await service.DetachConnector(account.Id);

            result.IntegrationConnectorId.Should().BeNull();
        }
    }
}
