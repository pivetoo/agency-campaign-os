using AgencyCampaign.Application.Models.Financial;
using AgencyCampaign.Application.Requests.Banks;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.TestSupport;
using Archon.Core.Pagination;
using Microsoft.EntityFrameworkCore;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class BankServiceTests
    {
        private TestDbContext db = null!;
        private BankService service = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            service = new BankService(db, CurrentUserMock.Create(userName: "Tester"));
        }

        [TearDown]
        public void TearDown() => db.Dispose();

        private async Task<Bank> SeedBankAsync(string compe = "001", string name = "Banco do Brasil", string shortName = "BB", bool isSystem = false, bool isActive = true)
        {
            Bank bank = new(compe, name, shortName, ispb: "00000000", isSystem: isSystem);
            if (!isActive)
            {
                bank.Update(bank.Compe, bank.Name, bank.ShortName, bank.Ispb, bank.LogoUrl, isActive: false);
            }
            db.Add(bank);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            return bank;
        }

        [Test]
        public async Task GetAll_should_filter_inactive_by_default()
        {
            await SeedBankAsync("001", "Banco do Brasil", "BB");
            await SeedBankAsync("002", "Itaú", "Itau", isActive: false);

            PagedResult<BankModel> result = await service.GetAll(new PagedRequest { Page = 1, PageSize = 10 }, search: null, includeInactive: false);

            result.Items.Should().HaveCount(1);
            result.Items.First().Compe.Should().Be("001");
        }

        [Test]
        public async Task GetAll_should_include_inactive_when_requested()
        {
            await SeedBankAsync("001", "Banco do Brasil", "BB");
            await SeedBankAsync("002", "Itaú", "Itau", isActive: false);

            PagedResult<BankModel> result = await service.GetAll(new PagedRequest { Page = 1, PageSize = 10 }, search: null, includeInactive: true);

            result.Items.Should().HaveCount(2);
        }

        [Test]
        public async Task GetAll_should_filter_by_search_term()
        {
            await SeedBankAsync("001", "Banco do Brasil", "BB");
            await SeedBankAsync("237", "Bradesco", "Brad");
            await SeedBankAsync("341", "Itaú", "Itau");

            PagedResult<BankModel> result = await service.GetAll(new PagedRequest { Page = 1, PageSize = 10 }, search: "brad", includeInactive: true);

            result.Items.Should().HaveCount(1);
            result.Items.First().ShortName.Should().Be("Brad");
        }

        [Test]
        public async Task GetActive_should_return_only_active_ordered_by_short_name()
        {
            await SeedBankAsync("237", "Bradesco", "Bradesco");
            await SeedBankAsync("001", "Banco do Brasil", "BB");
            await SeedBankAsync("002", "Itaú", "Itau", isActive: false);

            List<BankModel> result = await service.GetActive();

            result.Should().HaveCount(2);
            result.Select(item => item.ShortName).Should().ContainInOrder("BB", "Bradesco");
        }

        [Test]
        public async Task GetById_should_return_null_when_not_found()
        {
            BankModel? result = await service.GetById(99);

            result.Should().BeNull();
        }

        [Test]
        public async Task GetById_should_return_bank_when_found()
        {
            Bank seed = await SeedBankAsync();

            BankModel? result = await service.GetById(seed.Id);

            result.Should().NotBeNull();
            result!.Compe.Should().Be("001");
        }

        [Test]
        public async Task Create_should_persist_bank_with_created_by_user_name()
        {
            CreateBankRequest request = new() { Compe = "237", Name = "Bradesco", ShortName = "Brad", Ispb = "60746948" };

            BankModel result = await service.Create(request);

            result.Compe.Should().Be("237");
            result.IsSystem.Should().BeFalse();
            result.CreatedByUserName.Should().Be("Tester");
        }

        [Test]
        public async Task Create_should_throw_when_compe_already_exists()
        {
            await SeedBankAsync("001", "Banco do Brasil", "BB");

            CreateBankRequest request = new() { Compe = "001", Name = "Duplicado", ShortName = "Dup" };

            Func<Task> act = () => service.Create(request);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("bank.compe.duplicated");
        }

        [Test]
        public async Task Create_should_fallback_to_email_when_username_is_null()
        {
            BankService anonymous = new(db, CurrentUserMock.Create(userName: null, email: "noreply@x"));

            BankModel result = await anonymous.Create(new CreateBankRequest { Compe = "260", Name = "Nubank", ShortName = "Nu" });

            result.CreatedByUserName.Should().Be("noreply@x");
        }

        [Test]
        public async Task Update_should_throw_when_route_id_does_not_match_body_id()
        {
            UpdateBankRequest request = new() { Id = 99, Compe = "001", Name = "x", ShortName = "x" };

            Func<Task> act = () => service.Update(id: 1, request);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("request.route.idMismatch");
        }

        [Test]
        public async Task Update_should_throw_when_bank_not_found()
        {
            UpdateBankRequest request = new() { Id = 99, Compe = "001", Name = "x", ShortName = "x" };

            Func<Task> act = () => service.Update(id: 99, request);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("bank.notFound");
        }

        [Test]
        public async Task Update_should_persist_changes()
        {
            Bank bank = await SeedBankAsync("001", "Banco do Brasil", "BB");

            UpdateBankRequest request = new() { Id = bank.Id, Compe = "001", Name = "BB SA", ShortName = "BB", IsActive = false };

            BankModel result = await service.Update(bank.Id, request);

            result.Name.Should().Be("BB SA");
            result.IsActive.Should().BeFalse();
        }

        [Test]
        public async Task Update_should_throw_when_compe_duplicates_other_bank()
        {
            await SeedBankAsync("001", "BB", "BB");
            Bank target = await SeedBankAsync("237", "Bradesco", "Brad");

            UpdateBankRequest request = new() { Id = target.Id, Compe = "001", Name = "Bradesco", ShortName = "Brad" };

            Func<Task> act = () => service.Update(target.Id, request);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("bank.compe.duplicated");
        }

        [Test]
        public async Task Delete_should_throw_when_bank_not_found()
        {
            Func<Task> act = () => service.Delete(99);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("bank.notFound");
        }

        [Test]
        public async Task Delete_should_throw_when_bank_is_system()
        {
            Bank bank = await SeedBankAsync("001", "BB", "BB", isSystem: true);

            Func<Task> act = () => service.Delete(bank.Id);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("bank.system.cannotDelete");
        }

        [Test]
        public async Task Delete_should_throw_when_bank_has_associated_accounts()
        {
            Bank bank = await SeedBankAsync();
            FinancialAccount account = new("Conta principal", AgencyCampaign.Domain.ValueObjects.FinancialAccountType.Bank, 0m, "#fff", bankId: bank.Id);
            db.Add(account);
            await db.SaveChangesAsync();

            Func<Task> act = () => service.Delete(bank.Id);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("bank.hasAccounts.cannotDelete");
        }

        [Test]
        public async Task Delete_should_remove_bank_when_safe()
        {
            Bank bank = await SeedBankAsync();

            await service.Delete(bank.Id);

            int remaining = await db.Set<Bank>().CountAsync();
            remaining.Should().Be(0);
        }

        [Test]
        public async Task SetLogo_should_throw_when_bank_not_found()
        {
            Func<Task> act = () => service.SetLogo(99, "/logo.png");

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("bank.notFound");
        }

        [Test]
        public async Task SetLogo_should_persist_logo_url()
        {
            Bank bank = await SeedBankAsync();

            BankModel result = await service.SetLogo(bank.Id, "/uploads/banks/001.png");

            result.LogoUrl.Should().Be("/uploads/banks/001.png");
        }

        [Test]
        public async Task RemoveLogo_should_throw_when_bank_not_found()
        {
            Func<Task> act = () => service.RemoveLogo(99);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("bank.notFound");
        }

        [Test]
        public async Task RemoveLogo_should_clear_logo_url()
        {
            Bank bank = await SeedBankAsync();
            await service.SetLogo(bank.Id, "/uploads/banks/001.png");

            BankModel result = await service.RemoveLogo(bank.Id);

            result.LogoUrl.Should().BeNull();
        }
    }
}
