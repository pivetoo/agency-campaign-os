using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Models.Financial;
using AgencyCampaign.Application.Requests.FinancialSubcategories;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class FinancialSubcategoryServiceTests
    {
        private TestDbContext db = null!;
        private FinancialSubcategoryService service = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            service = new FinancialSubcategoryService(db, LocalizerMock.Create<AgencyCampaignResource>());
        }

        [TearDown]
        public void TearDown() => db.Dispose();

        [Test]
        public async Task Create_should_persist()
        {
            FinancialSubcategoryModel result = await service.Create(new CreateFinancialSubcategoryRequest
            {
                Name = "Hospedagem",
                MacroCategory = FinancialEntryCategory.OperationalCost,
                Color = "#fff"
            });

            result.Id.Should().BeGreaterThan(0);
        }

        [Test]
        public async Task Update_should_throw_when_id_mismatch()
        {
            UpdateFinancialSubcategoryRequest request = new() { Id = 5, Name = "x", Color = "#fff", MacroCategory = FinancialEntryCategory.AgencyFee };
            Func<Task> act = () => service.Update(99, request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task Update_should_throw_when_not_found()
        {
            UpdateFinancialSubcategoryRequest request = new() { Id = 99, Name = "x", Color = "#fff", MacroCategory = FinancialEntryCategory.AgencyFee };
            Func<Task> act = () => service.Update(99, request);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task GetAll_should_filter_inactive_when_requested()
        {
            db.Add(new FinancialSubcategory("Active", FinancialEntryCategory.AgencyFee, "#fff"));
            FinancialSubcategory inactive = new("Inactive", FinancialEntryCategory.AgencyFee, "#fff");
            inactive.Update("Inactive", FinancialEntryCategory.AgencyFee, "#fff", false);
            db.Add(inactive);
            await db.SaveChangesAsync();

            (await service.GetAll(includeInactive: false)).Should().ContainSingle();
            (await service.GetAll(includeInactive: true)).Should().HaveCount(2);
        }

        [Test]
        public async Task Delete_should_throw_when_not_found()
        {
            Func<Task> act = () => service.Delete(99);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }
    }
}
