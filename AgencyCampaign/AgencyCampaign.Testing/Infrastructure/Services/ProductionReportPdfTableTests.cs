using System.Globalization;
using AgencyCampaign.Application.Models.Production;
using AgencyCampaign.Application.Models.Reports;
using AgencyCampaign.Infrastructure.Services;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    // Testa apenas a montagem do ReportTable (puro, sem I/O, sem Chrome).
    // Os metodos BuildXxxTable sao public static em ProductionReportExportService.
    [TestFixture]
    public sealed class ProductionReportPdfTableTests
    {
        private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

        private static readonly DateTimeOffset From = new(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset To = new(2026, 5, 31, 0, 0, 0, TimeSpan.Zero);

        // -----------------------------------------------------------------------
        // BuildCampaignPerformanceTable
        // -----------------------------------------------------------------------

        [Test]
        public void BuildCampaignPerformanceTable_should_set_title_subtitle_and_columns()
        {
            CampaignPerformanceModel model = new() { From = From, To = To, Lines = [] };

            ReportTable table = ProductionReportExportService.BuildCampaignPerformanceTable(model, From, To);

            table.Title.Should().Be("Performance de Campanhas");
            table.Subtitle.Should().Contain("01/05/2026");
            table.Columns.Should().ContainInOrder("Campanha", "Marca", "Entregas", "Alcance", "Impressões", "Engajamento", "Taxa eng.", "EMV");
        }

        [Test]
        public void BuildCampaignPerformanceTable_should_have_four_kpis_with_correct_values()
        {
            CampaignPerformanceModel model = new()
            {
                Lines =
                [
                    new CampaignPerformanceLineModel { CampaignName = "Alpha", BrandName = "Acme", Deliverables = 3, TotalReach = 5000, TotalImpressions = 8000, TotalEngagement = 400, AvgEngagementRate = 8.00m, Emv = 80m },
                    new CampaignPerformanceLineModel { CampaignName = "Beta", BrandName = "Beta Co", Deliverables = 2, TotalReach = 3000, TotalImpressions = 4000, TotalEngagement = 200, AvgEngagementRate = 6.67m, Emv = 40m }
                ]
            };

            ReportTable table = ProductionReportExportService.BuildCampaignPerformanceTable(model, From, To);

            table.Kpis.Should().HaveCount(4);
            table.Kpis[0].Label.Should().Be("Campanhas");
            table.Kpis[0].Value.Should().Be("2");
            table.Kpis[1].Label.Should().Be("Alcance total");
            table.Kpis[1].Value.Should().Be("8000");
            table.Kpis[3].Label.Should().Be("EMV total");
            table.Kpis[3].Value.Should().Be(120m.ToString("C", PtBr));
        }

        [Test]
        public void BuildCampaignPerformanceTable_should_emit_one_row_per_line_with_correct_values()
        {
            CampaignPerformanceModel model = new()
            {
                Lines =
                [
                    new CampaignPerformanceLineModel { CampaignName = "Alpha", BrandName = "Acme", Deliverables = 2, TotalReach = 5000, TotalImpressions = 7000, TotalEngagement = 350, AvgEngagementRate = 7.00m, Emv = 70m }
                ]
            };

            ReportTable table = ProductionReportExportService.BuildCampaignPerformanceTable(model, From, To);

            table.Rows.Should().HaveCount(1);
            table.Rows[0][0].Should().Be("Alpha");
            table.Rows[0][1].Should().Be("Acme");
            table.Rows[0][2].Should().Be("2");
            table.Rows[0][3].Should().Be("5000");
            table.Rows[0][6].Should().Be(7.00m.ToString("0.00", PtBr) + "%");
            table.Rows[0][7].Should().Be(70m.ToString("C", PtBr));
        }

        [Test]
        public void BuildCampaignPerformanceTable_should_use_dash_for_null_brand_and_null_emv()
        {
            CampaignPerformanceModel model = new()
            {
                Lines =
                [
                    new CampaignPerformanceLineModel { CampaignName = "Sem Marca", BrandName = null, Deliverables = 1, TotalReach = 1000, TotalImpressions = 1500, TotalEngagement = 100, AvgEngagementRate = null, Emv = null }
                ]
            };

            ReportTable table = ProductionReportExportService.BuildCampaignPerformanceTable(model, From, To);

            table.Rows[0][1].Should().Be("-");
            table.Rows[0][6].Should().Be("-");
            table.Rows[0][7].Should().Be("-");
        }

        // -----------------------------------------------------------------------
        // BuildCreatorPerformanceTable
        // -----------------------------------------------------------------------

        [Test]
        public void BuildCreatorPerformanceTable_should_set_title_subtitle_and_columns()
        {
            CreatorPerformanceModel model = new() { Lines = [] };

            ReportTable table = ProductionReportExportService.BuildCreatorPerformanceTable(model, From, To);

            table.Title.Should().Be("Desempenho por Creator");
            table.Subtitle.Should().Contain("01/05/2026");
            table.Columns.Should().ContainInOrder("Creator", "Campanhas", "Entregas", "Alcance", "Engajamento", "Taxa eng.");
        }

        [Test]
        public void BuildCreatorPerformanceTable_should_have_three_kpis_and_emit_rows()
        {
            CreatorPerformanceModel model = new()
            {
                Lines =
                [
                    new CreatorPerformanceLineModel { CreatorName = "Alice Star", Campaigns = 2, Deliverables = 5, TotalReach = 8000, TotalEngagement = 600, AvgEngagementRate = 7.50m },
                    new CreatorPerformanceLineModel { CreatorName = "Bob", Campaigns = 1, Deliverables = 2, TotalReach = 3000, TotalEngagement = 200, AvgEngagementRate = 6.67m }
                ]
            };

            ReportTable table = ProductionReportExportService.BuildCreatorPerformanceTable(model, From, To);

            table.Kpis.Should().HaveCount(3);
            table.Kpis[0].Label.Should().Be("Creators");
            table.Kpis[0].Value.Should().Be("2");
            table.Kpis[1].Label.Should().Be("Alcance total");
            table.Kpis[1].Value.Should().Be("11000");

            table.Rows.Should().HaveCount(2);
            table.Rows[0][0].Should().Be("Alice Star");
            table.Rows[0][1].Should().Be("2");
            table.Rows[0][5].Should().Be(7.50m.ToString("0.00", PtBr) + "%");
        }

        // -----------------------------------------------------------------------
        // BuildPlatformProductionTable
        // -----------------------------------------------------------------------

        [Test]
        public void BuildPlatformProductionTable_should_set_title_subtitle_and_columns()
        {
            PlatformProductionModel model = new() { Lines = [] };

            ReportTable table = ProductionReportExportService.BuildPlatformProductionTable(model, From, To);

            table.Title.Should().Be("Produção por Plataforma");
            table.Columns.Should().ContainInOrder("Plataforma", "Entregas", "Alcance", "Impressões", "Engajamento", "Taxa eng.");
        }

        [Test]
        public void BuildPlatformProductionTable_should_have_three_kpis_and_emit_rows()
        {
            PlatformProductionModel model = new()
            {
                Lines =
                [
                    new PlatformProductionLineModel { PlatformName = "Instagram", Deliverables = 4, TotalReach = 10000, TotalImpressions = 15000, TotalEngagement = 800, AvgEngagementRate = 8.00m },
                    new PlatformProductionLineModel { PlatformName = "TikTok", Deliverables = 2, TotalReach = 5000, TotalImpressions = 7000, TotalEngagement = 400, AvgEngagementRate = 8.00m }
                ]
            };

            ReportTable table = ProductionReportExportService.BuildPlatformProductionTable(model, From, To);

            table.Kpis.Should().HaveCount(3);
            table.Kpis[0].Label.Should().Be("Plataformas");
            table.Kpis[0].Value.Should().Be("2");

            table.Rows.Should().HaveCount(2);
            table.Rows[0][0].Should().Be("Instagram");
            table.Rows[0][1].Should().Be("4");
            table.Rows[1][0].Should().Be("TikTok");
        }

        // -----------------------------------------------------------------------
        // BuildDeliverableSlaTable
        // -----------------------------------------------------------------------

        [Test]
        public void BuildDeliverableSlaTable_should_set_title_subtitle_and_columns()
        {
            DeliverableSlaModel model = new() { ByCampaign = [] };

            ReportTable table = ProductionReportExportService.BuildDeliverableSlaTable(model, From, To);

            table.Title.Should().Be("Entregáveis: Prazo × Atraso");
            table.Subtitle.Should().Contain("01/05/2026");
            table.Columns.Should().ContainInOrder("Campanha", "Total", "No prazo", "Atrasados", "Vencidos", "A vencer");
        }

        [Test]
        public void BuildDeliverableSlaTable_should_have_five_kpis_with_on_time_rate()
        {
            DeliverableSlaModel model = new()
            {
                PublishedOnTime = 8,
                PublishedLate = 2,
                Overdue = 1,
                Upcoming = 3,
                OnTimeRate = 80.00m,
                ByCampaign =
                [
                    new DeliverableSlaCampaignLineModel { CampaignName = "Alpha", Total = 10, PublishedOnTime = 8, PublishedLate = 2, Overdue = 0, Upcoming = 0 }
                ]
            };

            ReportTable table = ProductionReportExportService.BuildDeliverableSlaTable(model, From, To);

            table.Kpis.Should().HaveCount(5);
            table.Kpis[0].Label.Should().Be("No prazo");
            table.Kpis[0].Value.Should().Be("8");
            table.Kpis[4].Label.Should().Be("Taxa no prazo");
            table.Kpis[4].Value.Should().Be(80.00m.ToString("0.00", PtBr) + "%");

            table.Rows.Should().HaveCount(1);
            table.Rows[0][0].Should().Be("Alpha");
            table.Rows[0][1].Should().Be("10");
            table.Rows[0][2].Should().Be("8");
        }

        // -----------------------------------------------------------------------
        // BuildApprovalCycleTable
        // -----------------------------------------------------------------------

        [Test]
        public void BuildApprovalCycleTable_should_set_title_subtitle_and_columns()
        {
            ApprovalCycleModel model = new();

            ReportTable table = ProductionReportExportService.BuildApprovalCycleTable(model, From, To);

            table.Title.Should().Be("Aprovação e Rodadas");
            table.Subtitle.Should().Contain("01/05/2026");
            table.Columns.Should().ContainInOrder("Métrica", "Valor");
        }

        [Test]
        public void BuildApprovalCycleTable_should_have_four_kpis_and_seven_rows()
        {
            ApprovalCycleModel model = new()
            {
                InternalApprovedCount = 10,
                BrandApprovedCount = 8,
                AvgInternalApprovalDays = 2.5m,
                AvgBrandApprovalDays = 4.0m,
                ContentApprovedCount = 12,
                AvgRounds = 1.75m,
                FirstRoundApprovalRate = 60.00m
            };

            ReportTable table = ProductionReportExportService.BuildApprovalCycleTable(model, From, To);

            table.Kpis.Should().HaveCount(4);
            table.Kpis[0].Label.Should().Be("Aprov. interna");
            table.Kpis[0].Value.Should().Be(2.5m.ToString("0.0", PtBr) + " dias");
            table.Kpis[2].Label.Should().Be("Rodadas médias");
            table.Kpis[2].Value.Should().Be(1.75m.ToString("0.00", PtBr));
            table.Kpis[3].Label.Should().Be("Aprovado 1ª rodada");
            table.Kpis[3].Value.Should().Be(60.00m.ToString("0.00", PtBr) + "%");

            table.Rows.Should().HaveCount(7);
            table.Rows[0][0].Should().Be("Aprovações internas");
            table.Rows[0][1].Should().Be("10");
            table.Rows[1][0].Should().Be("Tempo médio interna");
            table.Rows[1][1].Should().Be(2.5m.ToString("0.0", PtBr) + " dias");
            table.Rows[5][0].Should().Be("Rodadas médias");
            table.Rows[6][0].Should().Be("Aprovado na 1ª rodada");
        }

        [Test]
        public void BuildApprovalCycleTable_should_use_dash_for_null_metrics()
        {
            ApprovalCycleModel model = new()
            {
                InternalApprovedCount = 0,
                BrandApprovedCount = 0,
                AvgInternalApprovalDays = null,
                AvgBrandApprovalDays = null,
                ContentApprovedCount = 0,
                AvgRounds = null,
                FirstRoundApprovalRate = null
            };

            ReportTable table = ProductionReportExportService.BuildApprovalCycleTable(model, From, To);

            table.Kpis[0].Value.Should().Be("-");
            table.Kpis[1].Value.Should().Be("-");
            table.Kpis[2].Value.Should().Be("-");
            table.Kpis[3].Value.Should().Be("-");
        }

        // -----------------------------------------------------------------------
        // BuildContentLicensesTable
        // -----------------------------------------------------------------------

        [Test]
        public void BuildContentLicensesTable_should_set_title_subtitle_and_columns()
        {
            ContentLicenseReportModel model = new() { ExpiringSoonDays = 30, Lines = [] };

            ReportTable table = ProductionReportExportService.BuildContentLicensesTable(model);

            table.Title.Should().Be("Licenças de Conteúdo");
            table.Subtitle.Should().Be("Vencendo em até 30 dias");
            table.Columns.Should().ContainInOrder("Entregável", "Campanha", "Tipo", "Canais", "Expira", "Dias", "Status");
        }

        [Test]
        public void BuildContentLicensesTable_should_have_three_kpis_with_counts()
        {
            ContentLicenseReportModel model = new()
            {
                ExpiringSoonDays = 30,
                ActiveCount = 5,
                ExpiringSoonCount = 2,
                ExpiredCount = 1,
                Lines = []
            };

            ReportTable table = ProductionReportExportService.BuildContentLicensesTable(model);

            table.Kpis.Should().HaveCount(3);
            table.Kpis[0].Label.Should().Be("Ativas");
            table.Kpis[0].Value.Should().Be("5");
            table.Kpis[1].Label.Should().Be("Expirando");
            table.Kpis[1].Value.Should().Be("2");
            table.Kpis[2].Label.Should().Be("Expiradas");
            table.Kpis[2].Value.Should().Be("1");
        }

        [Test]
        public void BuildContentLicensesTable_should_emit_rows_with_type_and_status_labels()
        {
            DateTimeOffset expiresAt = new(2026, 6, 15, 0, 0, 0, TimeSpan.Zero);
            ContentLicenseReportModel model = new()
            {
                ExpiringSoonDays = 30,
                Lines =
                [
                    new ContentLicenseReportLineModel
                    {
                        DeliverableTitle = "Post Reels",
                        CampaignName = "Summer",
                        Type = 2,
                        Channels = "Instagram",
                        ExpiresAt = expiresAt,
                        DaysUntilExpiry = 9,
                        Status = 2
                    }
                ]
            };

            ReportTable table = ProductionReportExportService.BuildContentLicensesTable(model);

            table.Rows.Should().HaveCount(1);
            table.Rows[0][0].Should().Be("Post Reels");
            table.Rows[0][1].Should().Be("Summer");
            table.Rows[0][2].Should().Be("Whitelisting pago");
            table.Rows[0][3].Should().Be("Instagram");
            table.Rows[0][4].Should().Be("15/06/2026");
            table.Rows[0][5].Should().Be("9");
            table.Rows[0][6].Should().Be("Expira em breve");
        }

        [Test]
        public void BuildContentLicensesTable_should_use_dash_for_null_campaign_channels_and_expiry()
        {
            ContentLicenseReportModel model = new()
            {
                ExpiringSoonDays = 60,
                Lines =
                [
                    new ContentLicenseReportLineModel
                    {
                        DeliverableTitle = "Perpetuo",
                        CampaignName = null,
                        Type = 4,
                        Channels = null,
                        ExpiresAt = null,
                        DaysUntilExpiry = null,
                        Status = 1
                    }
                ]
            };

            ReportTable table = ProductionReportExportService.BuildContentLicensesTable(model);

            table.Rows[0][1].Should().Be("-");
            table.Rows[0][3].Should().Be("-");
            table.Rows[0][4].Should().Be("-");
            table.Rows[0][5].Should().Be("-");
            table.Rows[0][6].Should().Be("Ativa");
        }
    }
}
