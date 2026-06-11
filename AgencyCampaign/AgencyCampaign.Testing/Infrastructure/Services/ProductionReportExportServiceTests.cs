using System.Text;
using AgencyCampaign.Application.Models.Reports;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.Builders;
using AgencyCampaign.Testing.TestSupport;
using Moq;
using DomainEntities = AgencyCampaign.Domain.Entities;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class ProductionReportExportServiceTests
    {
        private TestDbContext db = null!;
        private ProductionReportExportService service = null!;

        private static readonly DateTimeOffset WindowFrom = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset WindowTo = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset InsideWindow = new DateTimeOffset(2026, 5, 15, 12, 0, 0, TimeSpan.Zero);

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            Mock<IReportPdfService> pdfMock = new();
            pdfMock.Setup(s => s.GenerateAsync(It.IsAny<ReportTable>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync([]);
            service = new ProductionReportExportService(new ProductionReportService(db), pdfMock.Object);
        }

        [TearDown]
        public void TearDown() => db.Dispose();

        // -------------------------------------------------------------------
        // Seeding helpers (espelham ProductionReportServiceTests)
        // -------------------------------------------------------------------

        private async Task SeedBaseAsync()
        {
            AgencySettings settings = new("Agencia Teste");
            settings.Update("Agencia Teste", null, null, null, null, null, null, null, 10m, null);
            db.Add(settings.WithId(1));

            db.Add(new Brand("Acme").WithId(1));
            db.Add(new Brand("Beta").WithId(2));

            db.Add(new Platform("Instagram").WithId(1));
            db.Add(new Platform("TikTok").WithId(2));

            db.Add(new Creator("Alice", stageName: "Alice Star").WithId(1));
            db.Add(new Creator("Bob").WithId(2));

            db.Add(new DeliverableKind("Post").WithId(1));
            db.Add(new CampaignCreatorStatusBuilder().WithId(1).Build());

            db.Add(new Campaign(1, "Campaign Alpha", 5000m, InsideWindow).WithId(10));
            db.Add(new Campaign(2, "Campaign Beta", 3000m, InsideWindow).WithId(11));

            db.Add(new DomainEntities.CampaignCreator(10, 1, 1, 500m, 10m).WithId(101));
            db.Add(new DomainEntities.CampaignCreator(10, 2, 1, 300m, 10m).WithId(102));
            db.Add(new DomainEntities.CampaignCreator(11, 1, 1, 400m, 10m).WithId(103));

            await db.SaveChangesAsync();
        }

        // Entregavel ja vencido para cenarios de SLA: o construtor recusa prazo no passado, entao criamos
        // com prazo valido e forcamos o DueAt via reflexao.
        private static CampaignDeliverable MakeDeliverableWithPastDue(long campaignId, long campaignCreatorId, long platformId, string title, DateTimeOffset due)
        {
            CampaignDeliverable deliverable = new(campaignId, campaignCreatorId, title, 1, platformId, DateTimeOffset.UtcNow.AddDays(1), 1000m, 800m, 100m);
            typeof(CampaignDeliverable).GetProperty(nameof(CampaignDeliverable.DueAt))!.SetValue(deliverable, due);
            return deliverable;
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
        // ExportCampaignPerformance
        // -------------------------------------------------------------------

        [Test]
        public async Task ExportCampaignPerformance_should_emit_bom_header_and_campaign_name()
        {
            await SeedBaseAsync();

            CampaignDeliverable d1 = MakeDeliverable(10, 101, 1, InsideWindow, reach: 2000, impressions: 3000);
            db.Add(d1.WithId(1001));
            await db.SaveChangesAsync();

            byte[] bytes = await service.ExportCampaignPerformance(WindowFrom, WindowTo);

            bytes.Take(3).Should().Equal(0xEF, 0xBB, 0xBF);
            string csv = Encoding.UTF8.GetString(bytes);
            csv.Should().Contain("Campanha");
            csv.Should().Contain("Campaign Alpha");
        }

        [Test]
        public async Task ExportCampaignPerformance_should_include_brand_name_and_counts()
        {
            await SeedBaseAsync();

            CampaignDeliverable d1 = MakeDeliverable(10, 101, 1, InsideWindow, reach: 2000, impressions: 3000);
            db.Add(d1.WithId(1002));
            await db.SaveChangesAsync();

            byte[] bytes = await service.ExportCampaignPerformance(WindowFrom, WindowTo);
            string csv = Encoding.UTF8.GetString(bytes);

            csv.Should().Contain("Acme");
            csv.Should().Contain("2000");
        }

        // -------------------------------------------------------------------
        // ExportCreatorPerformance
        // -------------------------------------------------------------------

        [Test]
        public async Task ExportCreatorPerformance_should_emit_bom_header_and_creator_name()
        {
            await SeedBaseAsync();

            CampaignDeliverable d1 = MakeDeliverable(10, 101, 1, InsideWindow, reach: 1000, impressions: 1500);
            db.Add(d1.WithId(2001));
            await db.SaveChangesAsync();

            byte[] bytes = await service.ExportCreatorPerformance(WindowFrom, WindowTo);

            bytes.Take(3).Should().Equal(0xEF, 0xBB, 0xBF);
            string csv = Encoding.UTF8.GetString(bytes);
            csv.Should().Contain("Creator");
            csv.Should().Contain("Alice Star");
        }

        [Test]
        public async Task ExportCreatorPerformance_should_include_reach_count()
        {
            await SeedBaseAsync();

            CampaignDeliverable d1 = MakeDeliverable(10, 102, 1, InsideWindow, reach: 3000, impressions: 4000);
            db.Add(d1.WithId(2002));
            await db.SaveChangesAsync();

            byte[] bytes = await service.ExportCreatorPerformance(WindowFrom, WindowTo);
            string csv = Encoding.UTF8.GetString(bytes);

            csv.Should().Contain("Bob");
            csv.Should().Contain("3000");
        }

        // -------------------------------------------------------------------
        // ExportPlatformProduction
        // -------------------------------------------------------------------

        [Test]
        public async Task ExportPlatformProduction_should_emit_bom_header_and_platform_name()
        {
            await SeedBaseAsync();

            CampaignDeliverable d1 = MakeDeliverable(10, 101, 1, InsideWindow, reach: 2000, impressions: 3000);
            db.Add(d1.WithId(3001));
            await db.SaveChangesAsync();

            byte[] bytes = await service.ExportPlatformProduction(WindowFrom, WindowTo);

            bytes.Take(3).Should().Equal(0xEF, 0xBB, 0xBF);
            string csv = Encoding.UTF8.GetString(bytes);
            csv.Should().Contain("Plataforma");
            csv.Should().Contain("Instagram");
        }

        [Test]
        public async Task ExportPlatformProduction_should_include_reach_count()
        {
            await SeedBaseAsync();

            CampaignDeliverable d1 = MakeDeliverable(10, 102, 2, InsideWindow, reach: 5000, impressions: 6000);
            db.Add(d1.WithId(3002));
            await db.SaveChangesAsync();

            byte[] bytes = await service.ExportPlatformProduction(WindowFrom, WindowTo);
            string csv = Encoding.UTF8.GetString(bytes);

            csv.Should().Contain("TikTok");
            csv.Should().Contain("5000");
        }

        // -------------------------------------------------------------------
        // ExportDeliverableSla
        // -------------------------------------------------------------------

        [Test]
        public async Task ExportDeliverableSla_should_emit_bom_header_and_campaign_name()
        {
            await SeedBaseAsync();

            DateTimeOffset due = new DateTimeOffset(2026, 5, 20, 0, 0, 0, TimeSpan.Zero);
            CampaignDeliverable onTime = MakeDeliverableWithPastDue(10, 101, 1, "Entrega SLA", due);
            onTime.Publish("https://a.com", null, due.AddDays(-1));
            db.Add(onTime.WithId(4001));
            await db.SaveChangesAsync();

            byte[] bytes = await service.ExportDeliverableSla(WindowFrom, WindowTo);

            bytes.Take(3).Should().Equal(0xEF, 0xBB, 0xBF);
            string csv = Encoding.UTF8.GetString(bytes);
            csv.Should().Contain("Campanha");
            csv.Should().Contain("Campaign Alpha");
        }

        [Test]
        public async Task ExportDeliverableSla_should_include_total_row()
        {
            await SeedBaseAsync();

            DateTimeOffset due = new DateTimeOffset(2026, 5, 20, 0, 0, 0, TimeSpan.Zero);
            CampaignDeliverable onTime = MakeDeliverableWithPastDue(10, 101, 1, "Entrega SLA 2", due);
            onTime.Publish("https://b.com", null, due.AddDays(-1));
            db.Add(onTime.WithId(4002));
            await db.SaveChangesAsync();

            byte[] bytes = await service.ExportDeliverableSla(WindowFrom, WindowTo);
            string csv = Encoding.UTF8.GetString(bytes);

            csv.Should().Contain("Total");
            csv.Should().Contain("1");
        }

        // -------------------------------------------------------------------
        // ExportApprovalCycle
        // -------------------------------------------------------------------

        [Test]
        public async Task ExportApprovalCycle_should_emit_bom_header_and_metric_rows()
        {
            await SeedBaseAsync();

            byte[] bytes = await service.ExportApprovalCycle(WindowFrom, WindowTo);

            bytes.Take(3).Should().Equal(0xEF, 0xBB, 0xBF);
            string csv = Encoding.UTF8.GetString(bytes);
            csv.Should().Contain("Metrica");
            csv.Should().Contain("Aprovacoes internas");
        }

        [Test]
        public async Task ExportApprovalCycle_should_include_approval_counts()
        {
            await SeedBaseAsync();

            CampaignDeliverable d1 = new(10, 101, "Entrega Aprovacao", 1, 1, DateTimeOffset.UtcNow.AddDays(30), 1000m, 800m, 100m);
            db.Add(d1.WithId(6001));
            await db.SaveChangesAsync();

            DateTimeOffset createdAt = new DateTimeOffset(2026, 5, 10, 0, 0, 0, TimeSpan.Zero);
            DateTimeOffset approvedAt = new DateTimeOffset(2026, 5, 12, 0, 0, 0, TimeSpan.Zero);
            DeliverableApproval approval = new(6001, DeliverableApprovalType.Internal, "Revisor");
            approval.WithId(7001);
            approval.SetCreatedAt(createdAt);
            approval.Approve(approvedAt: approvedAt);
            db.Add(approval);
            await db.SaveChangesAsync();

            byte[] bytes = await service.ExportApprovalCycle(WindowFrom, WindowTo);
            string csv = Encoding.UTF8.GetString(bytes);

            csv.Should().Contain("Conteudos aprovados");
            csv.Should().Contain("2,00");
        }

        // -------------------------------------------------------------------
        // ExportContentLicenses
        // -------------------------------------------------------------------

        [Test]
        public async Task ExportContentLicenses_should_emit_bom_header_and_deliverable_title()
        {
            await SeedBaseAsync();

            db.Add(new Brand("Marca Licenca").WithId(99));
            db.Add(new Campaign(99, "Campanha Licenca", 0m, DateTimeOffset.UtcNow).WithId(200));
            db.Add(new Creator("Criador").WithId(99));
            db.Add(new DomainEntities.CampaignCreator(200, 99, 1, 0m, 0m).WithId(300));
            CampaignDeliverable deliverable = new(200, 300, "Entrega Licenca", 1, 1, DateTimeOffset.UtcNow.AddDays(10), 100m, 20m, 0m);
            db.Add(deliverable.WithId(500));
            await db.SaveChangesAsync();

            DeliverableContentLicense active = new(500, ContentLicenseType.UgcReuse, null, null, DateTimeOffset.UtcNow.AddDays(120), null, null, null);
            db.Add(active.WithId(601));
            await db.SaveChangesAsync();

            byte[] bytes = await service.ExportContentLicenses(30);

            bytes.Take(3).Should().Equal(0xEF, 0xBB, 0xBF);
            string csv = Encoding.UTF8.GetString(bytes);
            csv.Should().Contain("Entregavel");
            csv.Should().Contain("Entrega Licenca");
        }

        [Test]
        public async Task ExportContentLicenses_should_include_type_and_status_labels()
        {
            await SeedBaseAsync();

            db.Add(new Brand("Marca Licenca2").WithId(98));
            db.Add(new Campaign(98, "Campanha Licenca 2", 0m, DateTimeOffset.UtcNow).WithId(201));
            db.Add(new Creator("Criador2").WithId(98));
            db.Add(new DomainEntities.CampaignCreator(201, 98, 1, 0m, 0m).WithId(301));
            CampaignDeliverable deliverable = new(201, 301, "Entrega Status", 1, 1, DateTimeOffset.UtcNow.AddDays(10), 100m, 20m, 0m);
            db.Add(deliverable.WithId(501));
            await db.SaveChangesAsync();

            DeliverableContentLicense expired = new(501, ContentLicenseType.PaidWhitelisting, null, null, DateTimeOffset.UtcNow.AddDays(-5), null, null, null);
            db.Add(expired.WithId(602));
            await db.SaveChangesAsync();

            byte[] bytes = await service.ExportContentLicenses(30);
            string csv = Encoding.UTF8.GetString(bytes);

            csv.Should().Contain("Whitelisting pago");
            csv.Should().Contain("Expirada");
        }
    }
}
