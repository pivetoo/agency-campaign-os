using AgencyCampaign.Application.Models.Production;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.Builders;
using AgencyCampaign.Testing.TestSupport;
using DomainEntities = AgencyCampaign.Domain.Entities;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class ProductionReportServiceTests
    {
        private TestDbContext db = null!;
        private IProductionReportService service = null!;

        // Janela de teste
        private static readonly DateTimeOffset WindowFrom = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset WindowTo = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

        // PublishedAt dentro e fora da janela
        private static readonly DateTimeOffset InsideWindow = new DateTimeOffset(2026, 5, 15, 12, 0, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset OutsideWindow = new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero);

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            service = new ProductionReportService(db);
        }

        [TearDown]
        public void TearDown() => db.Dispose();

        // -------------------------------------------------------------------
        // Seeding helpers
        // -------------------------------------------------------------------

        private async Task<(long creatorId1, long creatorId2, long platformId1, long platformId2)> SeedBaseAsync()
        {
            // AgencySettings com EmvCpmRate = 10
            AgencySettings settings = new("Agencia Teste");
            settings.Update("Agencia Teste", null, null, null, null, null, null, null, 10m, null);
            db.Add(settings.WithId(1));

            // Brands
            db.Add(new Brand("Acme").WithId(1));
            db.Add(new Brand("Beta").WithId(2));

            // Platforms
            db.Add(new Platform("Instagram").WithId(1));
            db.Add(new Platform("TikTok").WithId(2));

            // Creators
            db.Add(new Creator("Alice", stageName: "Alice Star").WithId(1));
            db.Add(new Creator("Bob").WithId(2));

            // DeliverableKind
            db.Add(new DeliverableKind("Post").WithId(1));

            // CampaignCreatorStatus
            db.Add(new CampaignCreatorStatusBuilder().WithId(1).Build());

            // Campaigns (campaign 1 vinculada a Brand 1; campaign 2 a Brand 2)
            db.Add(new Campaign(1, "Campaign Alpha", 5000m, InsideWindow).WithId(10));
            db.Add(new Campaign(2, "Campaign Beta", 3000m, InsideWindow).WithId(11));

            // CampaignCreators
            // creator 1 participa de campaign 10
            db.Add(new DomainEntities.CampaignCreator(10, 1, 1, 500m, 10m).WithId(101));
            // creator 2 participa de campaign 10
            db.Add(new DomainEntities.CampaignCreator(10, 2, 1, 300m, 10m).WithId(102));
            // creator 1 participa de campaign 11 tambem
            db.Add(new DomainEntities.CampaignCreator(11, 1, 1, 400m, 10m).WithId(103));

            await db.SaveChangesAsync();

            return (1, 2, 1, 2);
        }

        private static CampaignDeliverable MakeDeliverable(long campaignId, long campaignCreatorId, long platformId, DateTimeOffset? publishedAt, int likes = 100, int comments = 20, long reach = 2000, long impressions = 3000, int saves = 10, int shares = 5)
        {
            CampaignDeliverable d = new(campaignId, campaignCreatorId, $"Post {campaignId}/{campaignCreatorId}/{platformId}", 1, platformId, DateTimeOffset.UtcNow.AddDays(30), 1000m, 800m, 100m);

            if (publishedAt.HasValue)
            {
                d.Publish("https://example.com/post", null, publishedAt.Value);
                d.RegisterMetrics(likes, comments, null, reach, impressions, saves, shares, DeliverableMetricsSource.Manual);
            }

            return d;
        }

        // -------------------------------------------------------------------
        // GetCampaignPerformance
        // -------------------------------------------------------------------

        [Test]
        public async Task GetCampaignPerformance_should_return_sums_for_published_deliverables_in_window()
        {
            await SeedBaseAsync();

            // Deliverable 1: campaign 10, creator 1, platform 1, dentro da janela
            // reach=2000, impressions=3000, likes=100, comments=20, saves=10, shares=5 -> engagement=135
            CampaignDeliverable d1 = MakeDeliverable(10, 101, 1, InsideWindow, likes: 100, comments: 20, reach: 2000, impressions: 3000, saves: 10, shares: 5);
            db.Add(d1.WithId(1001));

            // Deliverable 2: campaign 10, creator 2, platform 2, dentro da janela
            // reach=1000, impressions=1500, likes=50, comments=10, saves=5, shares=2 -> engagement=67
            CampaignDeliverable d2 = MakeDeliverable(10, 102, 2, InsideWindow, likes: 50, comments: 10, reach: 1000, impressions: 1500, saves: 5, shares: 2);
            db.Add(d2.WithId(1002));

            // Deliverable 3: campaign 10, fora da janela (deve ser excluido)
            CampaignDeliverable d3 = MakeDeliverable(10, 101, 1, OutsideWindow);
            db.Add(d3.WithId(1003));

            // Deliverable 4: campaign 11, dentro da janela
            // reach=500, impressions=800, likes=30, comments=5, saves=2, shares=1 -> engagement=38
            CampaignDeliverable d4 = MakeDeliverable(11, 103, 1, InsideWindow, likes: 30, comments: 5, reach: 500, impressions: 800, saves: 2, shares: 1);
            db.Add(d4.WithId(1004));

            // Deliverable nao publicado (deve ser excluido)
            CampaignDeliverable d5 = MakeDeliverable(10, 101, 1, null);
            db.Add(d5.WithId(1005));

            await db.SaveChangesAsync();

            CampaignPerformanceModel result = await service.GetCampaignPerformance(WindowFrom, WindowTo);

            result.Should().NotBeNull();
            result.Lines.Should().HaveCount(2);

            // Campaign 10: reach=3000, impressions=4500, engagement=202 (135+67)
            CampaignPerformanceLineModel camp10 = result.Lines.First(l => l.CampaignId == 10);
            camp10.CampaignName.Should().Be("Campaign Alpha");
            camp10.BrandName.Should().Be("Acme");
            camp10.Deliverables.Should().Be(2);
            camp10.TotalReach.Should().Be(3000);
            camp10.TotalImpressions.Should().Be(4500);
            camp10.TotalEngagement.Should().Be(202);

            // Campaign 11: reach=500, impressions=800, engagement=38
            CampaignPerformanceLineModel camp11 = result.Lines.First(l => l.CampaignId == 11);
            camp11.CampaignName.Should().Be("Campaign Beta");
            camp11.BrandName.Should().Be("Beta");
            camp11.Deliverables.Should().Be(1);
            camp11.TotalReach.Should().Be(500);
            camp11.TotalImpressions.Should().Be(800);
            camp11.TotalEngagement.Should().Be(38);
        }

        [Test]
        public async Task GetCampaignPerformance_should_order_by_total_reach_descending()
        {
            await SeedBaseAsync();

            // Campaign 10: reach menor
            CampaignDeliverable d1 = MakeDeliverable(10, 101, 1, InsideWindow, reach: 500, impressions: 800);
            db.Add(d1.WithId(1001));

            // Campaign 11: reach maior
            CampaignDeliverable d2 = MakeDeliverable(11, 103, 1, InsideWindow, reach: 2000, impressions: 3000);
            db.Add(d2.WithId(1002));

            await db.SaveChangesAsync();

            CampaignPerformanceModel result = await service.GetCampaignPerformance(WindowFrom, WindowTo);

            result.Lines.Should().HaveCount(2);
            result.Lines.First().CampaignId.Should().Be(11);
            result.Lines.Last().CampaignId.Should().Be(10);
        }

        [Test]
        public async Task GetCampaignPerformance_should_compute_emv_from_impressions_when_rate_configured()
        {
            await SeedBaseAsync();

            // impressions=3000 -> EMV = round(3000/1000*10, 2) = 30.00
            CampaignDeliverable d1 = MakeDeliverable(10, 101, 1, InsideWindow, reach: 2000, impressions: 3000);
            db.Add(d1.WithId(1001));
            await db.SaveChangesAsync();

            CampaignPerformanceModel result = await service.GetCampaignPerformance(WindowFrom, WindowTo);

            CampaignPerformanceLineModel line = result.Lines.Single();
            line.Emv.Should().Be(30.00m);
        }

        [Test]
        public async Task GetCampaignPerformance_should_return_null_emv_when_rate_not_configured()
        {
            // Nao seeda AgencySettings
            db.Add(new Brand("Acme").WithId(1));
            db.Add(new Campaign(1, "Campaign Alpha", 5000m, InsideWindow).WithId(10));
            db.Add(new CampaignCreatorStatusBuilder().WithId(1).Build());
            db.Add(new DomainEntities.CampaignCreator(10, 1, 1, 500m, 10m).WithId(101));
            db.Add(new Creator("Alice").WithId(1));
            db.Add(new Platform("Instagram").WithId(1));
            db.Add(new DeliverableKind("Post").WithId(1));
            CampaignDeliverable d1 = MakeDeliverable(10, 101, 1, InsideWindow, reach: 2000, impressions: 3000);
            db.Add(d1.WithId(1001));
            await db.SaveChangesAsync();

            CampaignPerformanceModel result = await service.GetCampaignPerformance(WindowFrom, WindowTo);

            result.Lines.Single().Emv.Should().BeNull();
        }

        [Test]
        public async Task GetCampaignPerformance_should_exclude_deliverables_outside_window()
        {
            await SeedBaseAsync();

            CampaignDeliverable inside = MakeDeliverable(10, 101, 1, InsideWindow, reach: 2000, impressions: 3000);
            db.Add(inside.WithId(1001));

            CampaignDeliverable outside = MakeDeliverable(10, 101, 1, OutsideWindow, reach: 9999, impressions: 9999);
            db.Add(outside.WithId(1002));

            await db.SaveChangesAsync();

            CampaignPerformanceModel result = await service.GetCampaignPerformance(WindowFrom, WindowTo);

            CampaignPerformanceLineModel line = result.Lines.Single();
            line.TotalReach.Should().Be(2000);
            line.Deliverables.Should().Be(1);
        }

        [Test]
        public async Task GetCampaignPerformance_should_return_empty_when_no_published_in_window()
        {
            await SeedBaseAsync();
            await db.SaveChangesAsync();

            CampaignPerformanceModel result = await service.GetCampaignPerformance(WindowFrom, WindowTo);

            result.Lines.Should().BeEmpty();
        }

        // -------------------------------------------------------------------
        // GetCreatorPerformance
        // -------------------------------------------------------------------

        [Test]
        public async Task GetCreatorPerformance_should_aggregate_by_creator_with_correct_campaign_count()
        {
            await SeedBaseAsync();

            // Creator 1 em campaign 10 (cc 101): 2 deliverables
            CampaignDeliverable d1 = MakeDeliverable(10, 101, 1, InsideWindow, reach: 2000, impressions: 3000, likes: 100, comments: 20, saves: 10, shares: 5);
            db.Add(d1.WithId(1001));
            CampaignDeliverable d2 = MakeDeliverable(10, 101, 2, InsideWindow, reach: 1000, impressions: 1500, likes: 50, comments: 10, saves: 5, shares: 2);
            db.Add(d2.WithId(1002));

            // Creator 1 em campaign 11 (cc 103): 1 deliverable
            CampaignDeliverable d3 = MakeDeliverable(11, 103, 1, InsideWindow, reach: 500, impressions: 800, likes: 30, comments: 5, saves: 2, shares: 1);
            db.Add(d3.WithId(1003));

            // Creator 2 em campaign 10 (cc 102): 1 deliverable
            CampaignDeliverable d4 = MakeDeliverable(10, 102, 1, InsideWindow, reach: 3000, impressions: 4000, likes: 200, comments: 40, saves: 20, shares: 8);
            db.Add(d4.WithId(1004));

            await db.SaveChangesAsync();

            CreatorPerformanceModel result = await service.GetCreatorPerformance(WindowFrom, WindowTo);

            result.Lines.Should().HaveCount(2);

            // Creator 1 (Alice Star): 2 campaigns (10 e 11), 3 deliverables, reach=3500
            CreatorPerformanceLineModel creator1 = result.Lines.First(l => l.CreatorId == 1);
            creator1.CreatorName.Should().Be("Alice Star");
            creator1.Campaigns.Should().Be(2);
            creator1.Deliverables.Should().Be(3);
            creator1.TotalReach.Should().Be(3500);

            // Creator 2 (Bob): 1 campaign, 1 deliverable, reach=3000
            CreatorPerformanceLineModel creator2 = result.Lines.First(l => l.CreatorId == 2);
            creator2.CreatorName.Should().Be("Bob");
            creator2.Campaigns.Should().Be(1);
            creator2.Deliverables.Should().Be(1);
            creator2.TotalReach.Should().Be(3000);
        }

        [Test]
        public async Task GetCreatorPerformance_should_order_by_total_reach_descending()
        {
            await SeedBaseAsync();

            // Creator 2 tem reach maior
            CampaignDeliverable d1 = MakeDeliverable(10, 102, 1, InsideWindow, reach: 5000, impressions: 6000);
            db.Add(d1.WithId(1001));

            // Creator 1 tem reach menor
            CampaignDeliverable d2 = MakeDeliverable(10, 101, 1, InsideWindow, reach: 1000, impressions: 1500);
            db.Add(d2.WithId(1002));

            await db.SaveChangesAsync();

            CreatorPerformanceModel result = await service.GetCreatorPerformance(WindowFrom, WindowTo);

            result.Lines.First().CreatorId.Should().Be(2);
            result.Lines.Last().CreatorId.Should().Be(1);
        }

        [Test]
        public async Task GetCreatorPerformance_should_exclude_unpublished_and_out_of_window()
        {
            await SeedBaseAsync();

            CampaignDeliverable inside = MakeDeliverable(10, 101, 1, InsideWindow, reach: 2000, impressions: 3000);
            db.Add(inside.WithId(1001));

            CampaignDeliverable outside = MakeDeliverable(10, 101, 1, OutsideWindow, reach: 9999, impressions: 9999);
            db.Add(outside.WithId(1002));

            CampaignDeliverable unpublished = MakeDeliverable(10, 101, 1, null);
            db.Add(unpublished.WithId(1003));

            await db.SaveChangesAsync();

            CreatorPerformanceModel result = await service.GetCreatorPerformance(WindowFrom, WindowTo);

            CreatorPerformanceLineModel line = result.Lines.Single();
            line.Deliverables.Should().Be(1);
            line.TotalReach.Should().Be(2000);
        }

        [Test]
        public async Task GetCreatorPerformance_should_use_stage_name_when_available()
        {
            await SeedBaseAsync();

            CampaignDeliverable d1 = MakeDeliverable(10, 101, 1, InsideWindow, reach: 1000, impressions: 1500);
            db.Add(d1.WithId(1001));
            await db.SaveChangesAsync();

            CreatorPerformanceModel result = await service.GetCreatorPerformance(WindowFrom, WindowTo);

            result.Lines.Single().CreatorName.Should().Be("Alice Star");
        }

        [Test]
        public async Task GetCreatorPerformance_should_use_name_when_stage_name_is_null()
        {
            await SeedBaseAsync();

            // Creator 2 (Bob) nao tem StageName
            CampaignDeliverable d1 = MakeDeliverable(10, 102, 1, InsideWindow, reach: 1000, impressions: 1500);
            db.Add(d1.WithId(1001));
            await db.SaveChangesAsync();

            CreatorPerformanceModel result = await service.GetCreatorPerformance(WindowFrom, WindowTo);

            result.Lines.Single().CreatorName.Should().Be("Bob");
        }

        // -------------------------------------------------------------------
        // GetPlatformProduction
        // -------------------------------------------------------------------

        [Test]
        public async Task GetPlatformProduction_should_aggregate_by_platform()
        {
            await SeedBaseAsync();

            // Platform 1 (Instagram): 2 deliverables
            // d1: reach=2000, impressions=3000, engagement=135 (100+20+10+5)
            CampaignDeliverable d1 = MakeDeliverable(10, 101, 1, InsideWindow, likes: 100, comments: 20, reach: 2000, impressions: 3000, saves: 10, shares: 5);
            db.Add(d1.WithId(1001));
            // d2: reach=1000, impressions=1500, engagement=67 (50+10+5+2)
            CampaignDeliverable d2 = MakeDeliverable(11, 103, 1, InsideWindow, likes: 50, comments: 10, reach: 1000, impressions: 1500, saves: 5, shares: 2);
            db.Add(d2.WithId(1002));

            // Platform 2 (TikTok): 1 deliverable
            // d3: reach=500, impressions=800, engagement=38 (30+5+2+1)
            CampaignDeliverable d3 = MakeDeliverable(10, 102, 2, InsideWindow, likes: 30, comments: 5, reach: 500, impressions: 800, saves: 2, shares: 1);
            db.Add(d3.WithId(1003));

            await db.SaveChangesAsync();

            PlatformProductionModel result = await service.GetPlatformProduction(WindowFrom, WindowTo);

            result.Lines.Should().HaveCount(2);

            // Platform 1 (Instagram): reach=3000, impressions=4500, engagement=202
            PlatformProductionLineModel ig = result.Lines.First(l => l.PlatformId == 1);
            ig.PlatformName.Should().Be("Instagram");
            ig.Deliverables.Should().Be(2);
            ig.TotalReach.Should().Be(3000);
            ig.TotalImpressions.Should().Be(4500);
            ig.TotalEngagement.Should().Be(202);

            // Platform 2 (TikTok): reach=500, impressions=800, engagement=38
            PlatformProductionLineModel tt = result.Lines.First(l => l.PlatformId == 2);
            tt.PlatformName.Should().Be("TikTok");
            tt.Deliverables.Should().Be(1);
            tt.TotalReach.Should().Be(500);
            tt.TotalImpressions.Should().Be(800);
            tt.TotalEngagement.Should().Be(38);
        }

        [Test]
        public async Task GetPlatformProduction_should_order_by_total_reach_descending()
        {
            await SeedBaseAsync();

            // Platform 2 (TikTok) com reach maior
            CampaignDeliverable d1 = MakeDeliverable(10, 101, 2, InsideWindow, reach: 5000, impressions: 6000);
            db.Add(d1.WithId(1001));

            // Platform 1 (Instagram) com reach menor
            CampaignDeliverable d2 = MakeDeliverable(10, 102, 1, InsideWindow, reach: 1000, impressions: 1500);
            db.Add(d2.WithId(1002));

            await db.SaveChangesAsync();

            PlatformProductionModel result = await service.GetPlatformProduction(WindowFrom, WindowTo);

            result.Lines.First().PlatformId.Should().Be(2);
            result.Lines.Last().PlatformId.Should().Be(1);
        }

        [Test]
        public async Task GetPlatformProduction_should_exclude_unpublished_and_out_of_window()
        {
            await SeedBaseAsync();

            CampaignDeliverable inside = MakeDeliverable(10, 101, 1, InsideWindow, reach: 2000, impressions: 3000);
            db.Add(inside.WithId(1001));

            CampaignDeliverable outside = MakeDeliverable(10, 101, 1, OutsideWindow, reach: 9999, impressions: 9999);
            db.Add(outside.WithId(1002));

            CampaignDeliverable unpublished = MakeDeliverable(10, 101, 1, null);
            db.Add(unpublished.WithId(1003));

            await db.SaveChangesAsync();

            PlatformProductionModel result = await service.GetPlatformProduction(WindowFrom, WindowTo);

            PlatformProductionLineModel line = result.Lines.Single();
            line.Deliverables.Should().Be(1);
            line.TotalReach.Should().Be(2000);
        }

        [Test]
        public async Task GetPlatformProduction_should_return_empty_when_no_published_in_window()
        {
            await SeedBaseAsync();
            await db.SaveChangesAsync();

            PlatformProductionModel result = await service.GetPlatformProduction(WindowFrom, WindowTo);

            result.Lines.Should().BeEmpty();
        }
    }
}
