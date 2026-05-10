using AgencyCampaign.Application.Models.Dashboard;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.Builders;
using AgencyCampaign.Testing.TestSupport;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class DashboardServiceTests
    {
        private TestDbContext db = null!;
        private DashboardService service = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            service = new DashboardService(db);
        }

        [TearDown]
        public void TearDown() => db.Dispose();

        [Test]
        public async Task GetOverview_should_return_zeros_for_empty_database()
        {
            DashboardOverviewModel result = await service.GetOverview();

            result.Headline.ActiveCampaigns.Should().Be(0);
            result.Headline.ActiveBrands.Should().Be(0);
            result.Headline.ActiveCreators.Should().Be(0);
            result.MonthlyRevenue.Should().HaveCount(12);
            result.Pipeline.Should().BeEmpty();
            result.PlatformDistribution.Should().BeEmpty();
        }

        [Test]
        public async Task GetOverview_should_aggregate_brands_creators_and_pipeline()
        {
            db.Add(new Brand("Acme"));
            db.Add(new Brand("Other"));
            db.Add(new Creator("c1"));
            db.Add(new Creator("c2"));

            CommercialPipelineStage stage = new CommercialPipelineStageBuilder().WithId(1).WithName("Qualificação").Build();
            db.Add(stage);
            db.Add(new Opportunity(1, 1, "deal", 1000m));
            db.Add(new Platform("IG"));
            db.Add(new DeliverableKind("Story"));
            await db.SaveChangesAsync();

            DashboardOverviewModel result = await service.GetOverview();

            result.Headline.ActiveBrands.Should().Be(2);
            result.Headline.ActiveCreators.Should().Be(2);
            result.Pipeline.Should().HaveCount(1);
            result.Pipeline.First().Name.Should().Be("Qualificação");
        }
    }
}
