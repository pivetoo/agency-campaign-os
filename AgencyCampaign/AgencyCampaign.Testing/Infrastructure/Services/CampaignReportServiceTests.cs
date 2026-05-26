using AgencyCampaign.Application.Models.Campaigns;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.TestSupport;
using DomainEntities = AgencyCampaign.Domain.Entities;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class CampaignReportServiceTests
    {
        private TestDbContext db = null!;
        private CampaignReportService service = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            service = new CampaignReportService(db, CurrentUserMock.Create());
        }

        [TearDown]
        public void TearDown() => db.Dispose();

        private async Task SeedCampaignWithMetricsAsync()
        {
            db.Add(new Brand("Acme").WithId(1));
            db.Add(new Campaign(1, "Camp", 10000m, DateTimeOffset.UtcNow).WithId(10));
            db.Add(new Creator("Foo").WithId(1));
            db.Add(new DomainEntities.CampaignCreator(10, 1, 1, 100m, 10m).WithId(20));
            db.Add(new Platform("IG").WithId(1));
            db.Add(new DeliverableKind("Story").WithId(1));

            CampaignDeliverable first = new(10, 20, "Post 1", 1, 1, DateTimeOffset.UtcNow.AddDays(5), 1000m, 800m, 100m);
            first.Publish("https://x/1", null, DateTimeOffset.UtcNow);
            first.RegisterMetrics(100, 20, 5000, 4000, 6000, 30, 10, DeliverableMetricsSource.Manual);
            db.Add(first.WithId(30));

            CampaignDeliverable second = new(10, 20, "Post 2", 1, 1, DateTimeOffset.UtcNow.AddDays(6), 1000m, 800m, 100m);
            second.Publish("https://x/2", null, DateTimeOffset.UtcNow);
            second.RegisterMetrics(50, 10, 2000, 1000, 1500, 5, 5, DeliverableMetricsSource.Manual);
            db.Add(second.WithId(31));

            db.Add(new CampaignReportLink(10, "tok123", null, null).WithId(40));
            await db.SaveChangesAsync();
        }

        [Test]
        public async Task GetReportByToken_should_aggregate_totals_and_efficiency()
        {
            await SeedCampaignWithMetricsAsync();

            CampaignReportModel? report = await service.GetReportByToken("tok123");

            report.Should().NotBeNull();
            report!.CampaignName.Should().Be("Camp");
            report.BrandName.Should().Be("Acme");
            report.Totals.DeliverablesCount.Should().Be(2);
            report.Totals.PublishedCount.Should().Be(2);
            report.Totals.TotalReach.Should().Be(5000);
            report.Totals.TotalEngagement.Should().Be(230);
            report.Totals.Investment.Should().Be(10000m);
            report.Totals.Cpm.Should().Be(2000m);
            report.Totals.CostPerEngagement.Should().Be(43.48m);
            report.ByPlatform.Should().HaveCount(1);
            report.Deliverables.Should().HaveCount(2);
        }

        [Test]
        public async Task GetReportByToken_should_register_a_view()
        {
            await SeedCampaignWithMetricsAsync();

            await service.GetReportByToken("tok123");

            CampaignReportLink link = db.Set<CampaignReportLink>().Single();
            link.ViewCount.Should().Be(1);
            link.LastViewedAt.Should().NotBeNull();
        }

        [Test]
        public async Task GetReportByToken_should_return_null_for_invalid_token()
        {
            await SeedCampaignWithMetricsAsync();

            CampaignReportModel? report = await service.GetReportByToken("nope");

            report.Should().BeNull();
        }

        [Test]
        public async Task GetReportByToken_should_return_null_when_link_revoked()
        {
            await SeedCampaignWithMetricsAsync();
            CampaignReportLink link = db.Set<CampaignReportLink>().Single();
            link.Revoke();
            await db.SaveChangesAsync();

            CampaignReportModel? report = await service.GetReportByToken("tok123");

            report.Should().BeNull();
        }
    }
}
