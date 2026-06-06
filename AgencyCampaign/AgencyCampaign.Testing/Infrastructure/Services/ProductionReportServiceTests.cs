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

        // -------------------------------------------------------------------
        // GetDeliverableSla
        // -------------------------------------------------------------------

        private CampaignDeliverable MakeDeliverableWithDueAt(long campaignId, long campaignCreatorId, long platformId, DateTimeOffset dueAt)
        {
            return new CampaignDeliverable(campaignId, campaignCreatorId, $"Entrega {campaignId}/{dueAt.Day}", 1, platformId, dueAt, 1000m, 800m, 100m);
        }

        [Test]
        public async Task GetDeliverableSla_should_classify_each_deliverable_correctly()
        {
            await SeedBaseAsync();

            DateTimeOffset slaFrom = new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero);
            DateTimeOffset slaTo = new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);

            // Publicado antes do prazo (on time)
            DateTimeOffset dueOnTime = new DateTimeOffset(2026, 5, 20, 0, 0, 0, TimeSpan.Zero);
            CampaignDeliverable onTime = MakeDeliverableWithDueAt(10, 101, 1, dueOnTime);
            onTime.Publish("https://a.com", null, dueOnTime.AddDays(-1));
            db.Add(onTime.WithId(2001));

            // Publicado depois do prazo (late)
            DateTimeOffset dueLate = new DateTimeOffset(2026, 5, 25, 0, 0, 0, TimeSpan.Zero);
            CampaignDeliverable late = MakeDeliverableWithDueAt(10, 101, 1, dueLate);
            late.Publish("https://b.com", null, dueLate.AddDays(2));
            db.Add(late.WithId(2002));

            // Em aberto com prazo já vencido (overdue) — DueAt no passado relativo a UtcNow
            DateTimeOffset dueOverdue = DateTimeOffset.UtcNow.AddDays(-5);
            CampaignDeliverable overdue = MakeDeliverableWithDueAt(10, 101, 1, dueOverdue);
            db.Add(overdue.WithId(2003));

            // Em aberto com prazo no futuro (upcoming)
            DateTimeOffset dueUpcoming = DateTimeOffset.UtcNow.AddDays(10);
            CampaignDeliverable upcoming = MakeDeliverableWithDueAt(10, 101, 1, dueUpcoming);
            db.Add(upcoming.WithId(2004));

            await db.SaveChangesAsync();

            DeliverableSlaModel result = await service.GetDeliverableSla(slaFrom, slaTo);

            result.PublishedOnTime.Should().Be(1);
            result.PublishedLate.Should().Be(1);
            result.Overdue.Should().Be(1);
            result.Upcoming.Should().Be(1);
            result.OnTimeRate.Should().Be(50.00m);
        }

        [Test]
        public async Task GetDeliverableSla_should_exclude_deliverables_outside_window()
        {
            await SeedBaseAsync();

            DateTimeOffset slaFrom = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);
            DateTimeOffset slaTo = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

            // DueAt dentro da janela
            DateTimeOffset dueInside = new DateTimeOffset(2026, 5, 20, 0, 0, 0, TimeSpan.Zero);
            CampaignDeliverable inside = MakeDeliverableWithDueAt(10, 101, 1, dueInside);
            inside.Publish("https://inside.com", null, dueInside.AddDays(-1));
            db.Add(inside.WithId(3001));

            // DueAt fora da janela (depois de slaTo)
            DateTimeOffset dueOutside = new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero);
            CampaignDeliverable outside = MakeDeliverableWithDueAt(10, 101, 1, dueOutside);
            outside.Publish("https://outside.com", null, dueOutside.AddDays(-1));
            db.Add(outside.WithId(3002));

            await db.SaveChangesAsync();

            DeliverableSlaModel result = await service.GetDeliverableSla(slaFrom, slaTo);

            result.PublishedOnTime.Should().Be(1);
            result.PublishedLate.Should().Be(0);
            result.Overdue.Should().Be(0);
            result.Upcoming.Should().Be(0);
        }

        [Test]
        public async Task GetDeliverableSla_should_exclude_cancelled_deliverables()
        {
            await SeedBaseAsync();

            DateTimeOffset slaFrom = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);
            DateTimeOffset slaTo = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

            DateTimeOffset due = new DateTimeOffset(2026, 5, 10, 0, 0, 0, TimeSpan.Zero);

            // Cancelado — deve ser ignorado
            CampaignDeliverable cancelled = MakeDeliverableWithDueAt(10, 101, 1, due);
            cancelled.ChangeStatus(DeliverableStatus.Cancelled);
            db.Add(cancelled.WithId(4001));

            await db.SaveChangesAsync();

            DeliverableSlaModel result = await service.GetDeliverableSla(slaFrom, slaTo);

            result.PublishedOnTime.Should().Be(0);
            result.PublishedLate.Should().Be(0);
            result.Overdue.Should().Be(0);
            result.Upcoming.Should().Be(0);
        }

        [Test]
        public async Task GetDeliverableSla_should_group_by_campaign()
        {
            await SeedBaseAsync();

            DateTimeOffset slaFrom = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);
            DateTimeOffset slaTo = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

            DateTimeOffset due = new DateTimeOffset(2026, 5, 15, 0, 0, 0, TimeSpan.Zero);

            // Campaign 10: 2 entregáveis (1 on time, 1 late)
            CampaignDeliverable c10a = MakeDeliverableWithDueAt(10, 101, 1, due);
            c10a.Publish("https://a.com", null, due.AddDays(-1));
            db.Add(c10a.WithId(5001));

            CampaignDeliverable c10b = MakeDeliverableWithDueAt(10, 101, 1, due);
            c10b.Publish("https://b.com", null, due.AddDays(3));
            db.Add(c10b.WithId(5002));

            // Campaign 11: 1 entregável (on time)
            CampaignDeliverable c11a = MakeDeliverableWithDueAt(11, 103, 1, due);
            c11a.Publish("https://c.com", null, due.AddDays(-2));
            db.Add(c11a.WithId(5003));

            await db.SaveChangesAsync();

            DeliverableSlaModel result = await service.GetDeliverableSla(slaFrom, slaTo);

            result.ByCampaign.Should().HaveCount(2);

            DeliverableSlaCampaignLineModel camp10 = result.ByCampaign.First(line => line.CampaignId == 10);
            camp10.CampaignName.Should().Be("Campaign Alpha");
            camp10.Total.Should().Be(2);
            camp10.PublishedOnTime.Should().Be(1);
            camp10.PublishedLate.Should().Be(1);

            DeliverableSlaCampaignLineModel camp11 = result.ByCampaign.First(line => line.CampaignId == 11);
            camp11.Total.Should().Be(1);
            camp11.PublishedOnTime.Should().Be(1);
        }

        // -------------------------------------------------------------------
        // GetApprovalCycle
        // -------------------------------------------------------------------

        [Test]
        public async Task GetApprovalCycle_should_compute_avg_approval_days_and_counts()
        {
            await SeedBaseAsync();

            // Deliverables isolados (sem constraint de unique por campanha/tipo)
            CampaignDeliverable d1 = MakeDeliverableWithDueAt(10, 101, 1, DateTimeOffset.UtcNow.AddDays(30));
            db.Add(d1.WithId(6001));

            CampaignDeliverable d2 = MakeDeliverableWithDueAt(10, 101, 1, DateTimeOffset.UtcNow.AddDays(30));
            db.Add(d2.WithId(6002));

            await db.SaveChangesAsync();

            DateTimeOffset cycleFrom = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);
            DateTimeOffset cycleTo = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

            // Aprovação interna: criada em 2026-05-10, aprovada em 2026-05-12 (2 dias)
            DateTimeOffset internalCreatedAt = new DateTimeOffset(2026, 5, 10, 0, 0, 0, TimeSpan.Zero);
            DateTimeOffset internalApprovedAt = new DateTimeOffset(2026, 5, 12, 0, 0, 0, TimeSpan.Zero);
            DeliverableApproval internalApproval = new(6001, DeliverableApprovalType.Internal, "Revisor A");
            internalApproval.WithId(7001);
            internalApproval.SetCreatedAt(internalCreatedAt);
            internalApproval.Approve(approvedAt: internalApprovedAt);
            db.Add(internalApproval);

            // Aprovação de marca: criada em 2026-05-10, aprovada em 2026-05-14 (4 dias)
            DateTimeOffset brandCreatedAt = new DateTimeOffset(2026, 5, 10, 0, 0, 0, TimeSpan.Zero);
            DateTimeOffset brandApprovedAt = new DateTimeOffset(2026, 5, 14, 0, 0, 0, TimeSpan.Zero);
            DeliverableApproval brandApproval = new(6002, DeliverableApprovalType.Brand, "Revisor B");
            brandApproval.WithId(7002);
            brandApproval.SetCreatedAt(brandCreatedAt);
            brandApproval.Approve(approvedAt: brandApprovedAt);
            db.Add(brandApproval);

            await db.SaveChangesAsync();

            ApprovalCycleModel result = await service.GetApprovalCycle(cycleFrom, cycleTo);

            result.InternalApprovedCount.Should().Be(1);
            result.BrandApprovedCount.Should().Be(1);
            result.AvgInternalApprovalDays.Should().Be(2.00m);
            result.AvgBrandApprovalDays.Should().Be(4.00m);
        }

        [Test]
        public async Task GetApprovalCycle_should_compute_avg_rounds_and_first_round_rate()
        {
            await SeedBaseAsync();

            CampaignDeliverable d1 = MakeDeliverableWithDueAt(10, 101, 1, DateTimeOffset.UtcNow.AddDays(30));
            db.Add(d1.WithId(8001));

            CampaignDeliverable d2 = MakeDeliverableWithDueAt(10, 101, 1, DateTimeOffset.UtcNow.AddDays(30));
            db.Add(d2.WithId(8002));

            await db.SaveChangesAsync();

            DateTimeOffset cycleFrom = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);
            DateTimeOffset cycleTo = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

            // Versão aprovada na rodada 1 (aprovação interna)
            DeliverableContentVersion v1 = new(8001, 1, ReviewParticipant.Agency, "Agência", null);
            v1.WithId(9001);
            v1.SetCreatedAt(new DateTimeOffset(2026, 5, 15, 0, 0, 0, TimeSpan.Zero));
            v1.ApproveInternally();
            db.Add(v1);

            // Versão aprovada na rodada 3
            DeliverableContentVersion v3 = new(8002, 3, ReviewParticipant.Agency, "Agência", null);
            v3.WithId(9002);
            v3.SetCreatedAt(new DateTimeOffset(2026, 5, 20, 0, 0, 0, TimeSpan.Zero));
            v3.ApproveInternally();
            db.Add(v3);

            // Versão em outra rodada mas CreatedAt fora da janela — deve ser excluída
            // Usa deliverable 8002 (round 2) para não colidir com o índice único (deliverableId, roundNumber)
            DeliverableContentVersion vOut = new(8002, 2, ReviewParticipant.Agency, "Agência", null);
            vOut.WithId(9003);
            vOut.SetCreatedAt(new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero));
            vOut.ApproveInternally();
            db.Add(vOut);

            await db.SaveChangesAsync();

            ApprovalCycleModel result = await service.GetApprovalCycle(cycleFrom, cycleTo);

            result.ContentApprovedCount.Should().Be(2);
            result.AvgRounds.Should().Be(2.00m);
            result.FirstRoundApprovalRate.Should().Be(50.00m);
        }

        [Test]
        public async Task GetApprovalCycle_should_exclude_approvals_outside_window()
        {
            await SeedBaseAsync();

            CampaignDeliverable d1 = MakeDeliverableWithDueAt(10, 101, 1, DateTimeOffset.UtcNow.AddDays(30));
            db.Add(d1.WithId(11001));
            await db.SaveChangesAsync();

            DateTimeOffset cycleFrom = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);
            DateTimeOffset cycleTo = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

            // Aprovação fora da janela
            DeliverableApproval outside = new(11001, DeliverableApprovalType.Internal, "X");
            outside.WithId(11002);
            outside.SetCreatedAt(new DateTimeOffset(2026, 6, 28, 0, 0, 0, TimeSpan.Zero));
            outside.Approve(approvedAt: new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero));
            db.Add(outside);
            await db.SaveChangesAsync();

            ApprovalCycleModel result = await service.GetApprovalCycle(cycleFrom, cycleTo);

            result.InternalApprovedCount.Should().Be(0);
            result.AvgInternalApprovalDays.Should().BeNull();
        }

        // -------------------------------------------------------------------
        // GetContentLicenses
        // -------------------------------------------------------------------

        private async Task<long> SeedDeliverableForLicenseAsync(long deliverableId)
        {
            db.Add(new Brand("Marca").WithId(99));
            db.Add(new Campaign(99, "Campanha Licença", 0m, DateTimeOffset.UtcNow).WithId(200));
            db.Add(new Creator("Criador").WithId(99));
            db.Add(new DomainEntities.CampaignCreator(200, 99, 1, 0m, 0m).WithId(300));
            CampaignDeliverable deliverable = new(200, 300, "Entrega Licença", 1, 1, DateTimeOffset.UtcNow.AddDays(10), 100m, 20m, 0m);
            db.Add(deliverable.WithId(deliverableId));
            await db.SaveChangesAsync();
            return deliverableId;
        }

        [Test]
        public async Task GetContentLicenses_should_classify_expired_expiring_active_and_perpetual()
        {
            await SeedBaseAsync();
            long deliverableId = await SeedDeliverableForLicenseAsync(500);

            DateTimeOffset now = DateTimeOffset.UtcNow;

            // Expirada (ExpiresAt no passado)
            DeliverableContentLicense expired = new(deliverableId, ContentLicenseType.UgcReuse, null, null, now.AddDays(-10), null, null, null);
            db.Add(expired.WithId(601));

            // Expirando em breve (dentro do threshold padrão 30 dias)
            DeliverableContentLicense expiringSoon = new(deliverableId, ContentLicenseType.PaidWhitelisting, "Instagram", null, now.AddDays(15), null, null, null);
            db.Add(expiringSoon.WithId(602));

            // Ativa (ExpiresAt distante)
            DeliverableContentLicense active = new(deliverableId, ContentLicenseType.Exclusivity, null, null, now.AddDays(120), null, null, null);
            db.Add(active.WithId(603));

            // Perpétua (ExpiresAt null → sempre Active)
            DeliverableContentLicense perpetual = new(deliverableId, ContentLicenseType.Other, null, null, null, null, null, null);
            db.Add(perpetual.WithId(604));

            await db.SaveChangesAsync();

            ContentLicenseReportModel result = await service.GetContentLicenses(30);

            result.ExpiredCount.Should().Be(1);
            result.ExpiringSoonCount.Should().Be(1);
            result.ActiveCount.Should().Be(2);
            result.ExpiringSoonDays.Should().Be(30);

            ContentLicenseReportLineModel expiredLine = result.Lines.First(line => line.LicenseId == 601);
            expiredLine.Status.Should().Be((int)ContentLicenseStatus.Expired);
            expiredLine.DaysUntilExpiry.Should().Be(0);

            ContentLicenseReportLineModel soonLine = result.Lines.First(line => line.LicenseId == 602);
            soonLine.Status.Should().Be((int)ContentLicenseStatus.ExpiringSoon);
            soonLine.Channels.Should().Be("Instagram");

            ContentLicenseReportLineModel perpetualLine = result.Lines.First(line => line.LicenseId == 604);
            perpetualLine.Status.Should().Be((int)ContentLicenseStatus.Active);
            perpetualLine.DaysUntilExpiry.Should().BeNull();
            perpetualLine.ExpiresAt.Should().BeNull();
        }

        [Test]
        public async Task GetContentLicenses_should_default_threshold_to_30_when_zero_or_negative()
        {
            await SeedBaseAsync();
            long deliverableId = await SeedDeliverableForLicenseAsync(510);

            DateTimeOffset now = DateTimeOffset.UtcNow;

            DeliverableContentLicense active = new(deliverableId, ContentLicenseType.UgcReuse, null, null, now.AddDays(120), null, null, null);
            db.Add(active.WithId(611));

            await db.SaveChangesAsync();

            ContentLicenseReportModel result = await service.GetContentLicenses(0);

            result.ExpiringSoonDays.Should().Be(30);
        }

        [Test]
        public async Task GetContentLicenses_should_order_lines_with_nulls_last()
        {
            await SeedBaseAsync();
            long deliverableId = await SeedDeliverableForLicenseAsync(520);

            DateTimeOffset now = DateTimeOffset.UtcNow;

            // Perpétua (null ExpiresAt)
            DeliverableContentLicense perpetual = new(deliverableId, ContentLicenseType.Other, null, null, null, null, null, null);
            db.Add(perpetual.WithId(621));

            // Expira em 10 dias
            DeliverableContentLicense soonExpiring = new(deliverableId, ContentLicenseType.UgcReuse, null, null, now.AddDays(10), null, null, null);
            db.Add(soonExpiring.WithId(622));

            // Expira em 60 dias
            DeliverableContentLicense laterExpiring = new(deliverableId, ContentLicenseType.PaidWhitelisting, null, null, now.AddDays(60), null, null, null);
            db.Add(laterExpiring.WithId(623));

            await db.SaveChangesAsync();

            ContentLicenseReportModel result = await service.GetContentLicenses(30);

            // Ordem: 622 (10d), 623 (60d), 621 (null, último)
            result.Lines.First().LicenseId.Should().Be(622);
            result.Lines.Last().LicenseId.Should().Be(621);
        }
    }
}
