using System.Globalization;
using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Application.Models.Reports;
using AgencyCampaign.Infrastructure.Services;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    // Testa apenas a montagem do ReportTable (puro, sem I/O, sem Chrome).
    // Os metodos BuildXxxTable sao public static em CommercialReportExportService.
    [TestFixture]
    public sealed class CommercialReportPdfTableTests
    {
        private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

        private static readonly DateTimeOffset From = new(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset To = new(2026, 5, 31, 0, 0, 0, TimeSpan.Zero);

        // -----------------------------------------------------------------------
        // BuildFunilTable
        // -----------------------------------------------------------------------

        [Test]
        public void BuildFunilTable_should_set_title_subtitle_and_columns()
        {
            CommercialAnalyticsModel model = new()
            {
                ConversionByStage = [],
                WinReasons = [],
                LossReasons = []
            };

            ReportTable table = CommercialReportExportService.BuildFunilTable(model, From, To);

            table.Title.Should().Be("Funil de Conversão");
            table.Subtitle.Should().Contain("01/05/2026");
            table.Subtitle.Should().Contain("31/05/2026");
            table.Columns.Should().ContainInOrder("Estágio", "Entraram", "Avançaram", "Parados", "Perdidos", "Conversão");
        }

        [Test]
        public void BuildFunilTable_should_have_four_kpis_with_correct_values()
        {
            CommercialAnalyticsModel model = new()
            {
                ClosedCount = 10,
                WinRate = 60.00m,
                AverageCycleDays = 14.5m,
                ConversionByStage =
                [
                    new StageConversionModel { StageId = 1, StageName = "Qualificação", Entered = 20, Advanced = 15, Stuck = 3, Lost = 2, ConversionRate = 75.00m },
                    new StageConversionModel { StageId = 2, StageName = "Proposta", Entered = 15, Advanced = 10, Stuck = 2, Lost = 3, ConversionRate = 66.67m }
                ],
                WinReasons = [],
                LossReasons = []
            };

            ReportTable table = CommercialReportExportService.BuildFunilTable(model, From, To);

            table.Kpis.Should().HaveCount(4);
            table.Kpis[0].Label.Should().Be("Fechados");
            table.Kpis[0].Value.Should().Be("10");
            table.Kpis[1].Label.Should().Be("Win rate");
            table.Kpis[1].Value.Should().Be(60.00m.ToString("0.00", PtBr) + "%");
            table.Kpis[2].Label.Should().Be("Ciclo médio");
            table.Kpis[2].Value.Should().Be(14.5m.ToString("0.0", PtBr) + " dias");
            table.Kpis[3].Label.Should().Be("Em andamento");
            table.Kpis[3].Value.Should().Be("5");
        }

        [Test]
        public void BuildFunilTable_should_emit_one_row_per_stage_with_correct_values()
        {
            CommercialAnalyticsModel model = new()
            {
                ConversionByStage =
                [
                    new StageConversionModel { StageId = 1, StageName = "Qualificação", Entered = 20, Advanced = 15, Stuck = 3, Lost = 2, ConversionRate = 75.00m }
                ],
                WinReasons = [],
                LossReasons = []
            };

            ReportTable table = CommercialReportExportService.BuildFunilTable(model, From, To);

            table.Rows.Should().HaveCount(1);
            table.Rows[0][0].Should().Be("Qualificação");
            table.Rows[0][1].Should().Be("20");
            table.Rows[0][2].Should().Be("15");
            table.Rows[0][3].Should().Be("3");
            table.Rows[0][4].Should().Be("2");
            table.Rows[0][5].Should().Be(75.00m.ToString("0.00", PtBr) + "%");
        }

        // -----------------------------------------------------------------------
        // BuildGanhosPerdasTable
        // -----------------------------------------------------------------------

        [Test]
        public void BuildGanhosPerdasTable_should_set_title_subtitle_and_columns()
        {
            CommercialAnalyticsModel model = new()
            {
                ConversionByStage = [],
                WinReasons = [],
                LossReasons = []
            };

            ReportTable table = CommercialReportExportService.BuildGanhosPerdasTable(model, From, To);

            table.Title.Should().Be("Ganhos × Perdas");
            table.Subtitle.Should().Contain("01/05/2026");
            table.Columns.Should().ContainInOrder("Tipo", "Motivo", "Quantidade", "Valor");
        }

        [Test]
        public void BuildGanhosPerdasTable_should_have_four_kpis_summing_reasons()
        {
            CommercialAnalyticsModel model = new()
            {
                ConversionByStage = [],
                WinReasons =
                [
                    new ReasonAggregateModel { ReasonName = "Preço", Count = 5, TotalValue = 10000m },
                    new ReasonAggregateModel { ReasonName = "Qualidade", Count = 3, TotalValue = 6000m }
                ],
                LossReasons =
                [
                    new ReasonAggregateModel { ReasonName = "Concorrente", Count = 2, TotalValue = 4000m }
                ]
            };

            ReportTable table = CommercialReportExportService.BuildGanhosPerdasTable(model, From, To);

            table.Kpis.Should().HaveCount(4);
            table.Kpis[0].Label.Should().Be("Ganhos");
            table.Kpis[0].Value.Should().Be("8");
            table.Kpis[1].Label.Should().Be("Valor ganho");
            table.Kpis[1].Value.Should().Be(16000m.ToString("C", PtBr));
            table.Kpis[2].Label.Should().Be("Perdas");
            table.Kpis[2].Value.Should().Be("2");
            table.Kpis[3].Label.Should().Be("Valor perdido");
            table.Kpis[3].Value.Should().Be(4000m.ToString("C", PtBr));
        }

        [Test]
        public void BuildGanhosPerdasTable_should_emit_win_rows_before_loss_rows()
        {
            CommercialAnalyticsModel model = new()
            {
                ConversionByStage = [],
                WinReasons =
                [
                    new ReasonAggregateModel { ReasonName = "Preço", Count = 5, TotalValue = 10000m }
                ],
                LossReasons =
                [
                    new ReasonAggregateModel { ReasonName = "Concorrente", Count = 2, TotalValue = 4000m }
                ]
            };

            ReportTable table = CommercialReportExportService.BuildGanhosPerdasTable(model, From, To);

            table.Rows.Should().HaveCount(2);
            table.Rows[0][0].Should().Be("Ganho");
            table.Rows[0][1].Should().Be("Preço");
            table.Rows[0][2].Should().Be("5");
            table.Rows[0][3].Should().Be(10000m.ToString("C", PtBr));
            table.Rows[1][0].Should().Be("Perda");
            table.Rows[1][1].Should().Be("Concorrente");
        }

        // -----------------------------------------------------------------------
        // BuildForecastTable
        // -----------------------------------------------------------------------

        [Test]
        public void BuildForecastTable_should_set_title_subtitle_and_columns()
        {
            CommercialForecastModel model = new() { ByStage = [] };

            ReportTable table = CommercialReportExportService.BuildForecastTable(model, From, To);

            table.Title.Should().Be("Previsão (Forecast)");
            table.Subtitle.Should().Contain("01/05/2026");
            table.Columns.Should().ContainInOrder("Estágio", "Qtd", "Valor", "Ponderado", "Prob. média");
        }

        [Test]
        public void BuildForecastTable_should_have_four_kpis_with_correct_values()
        {
            CommercialForecastModel model = new()
            {
                WeightedTotal = 15000m,
                UnweightedTotal = 30000m,
                WonTotal = 8000m,
                OpenCount = 12,
                ByStage = []
            };

            ReportTable table = CommercialReportExportService.BuildForecastTable(model, From, To);

            table.Kpis.Should().HaveCount(4);
            table.Kpis[0].Label.Should().Be("Ponderado");
            table.Kpis[0].Value.Should().Be(15000m.ToString("C", PtBr));
            table.Kpis[1].Label.Should().Be("Bruto");
            table.Kpis[1].Value.Should().Be(30000m.ToString("C", PtBr));
            table.Kpis[2].Label.Should().Be("Ganho");
            table.Kpis[2].Value.Should().Be(8000m.ToString("C", PtBr));
            table.Kpis[3].Label.Should().Be("Em aberto");
            table.Kpis[3].Value.Should().Be("12");
        }

        [Test]
        public void BuildForecastTable_should_emit_one_row_per_stage()
        {
            CommercialForecastModel model = new()
            {
                ByStage =
                [
                    new CommercialForecastStageBreakdown { StageName = "Proposta", Count = 5, TotalValue = 20000m, WeightedValue = 10000m, AverageProbability = 50.00m }
                ]
            };

            ReportTable table = CommercialReportExportService.BuildForecastTable(model, From, To);

            table.Rows.Should().HaveCount(1);
            table.Rows[0][0].Should().Be("Proposta");
            table.Rows[0][1].Should().Be("5");
            table.Rows[0][2].Should().Be(20000m.ToString("C", PtBr));
            table.Rows[0][3].Should().Be(10000m.ToString("C", PtBr));
            table.Rows[0][4].Should().Be(50.00m.ToString("0.00", PtBr) + "%");
        }

        // -----------------------------------------------------------------------
        // BuildMetasTable
        // -----------------------------------------------------------------------

        [Test]
        public void BuildMetasTable_should_set_title_subtitle_and_columns()
        {
            ReportTable table = CommercialReportExportService.BuildMetasTable([], From);

            table.Title.Should().Be("Metas × Realizado");
            table.Subtitle.Should().Contain("01/05/2026");
            table.Columns.Should().ContainInOrder("Responsável", "Período", "Meta", "Realizado", "Negócios", "Atingido");
        }

        [Test]
        public void BuildMetasTable_should_have_three_kpis_summing_amounts()
        {
            IReadOnlyCollection<CommercialGoalProgressModel> progress =
            [
                new CommercialGoalProgressModel { UserName = "Alice", PeriodType = 1, TargetAmount = 5000m, AchievedAmount = 4000m, AchievedDealsCount = 3, PercentAchieved = 80.00m },
                new CommercialGoalProgressModel { UserName = "Bob", PeriodType = 2, TargetAmount = 8000m, AchievedAmount = 6000m, AchievedDealsCount = 5, PercentAchieved = 75.00m }
            ];

            ReportTable table = CommercialReportExportService.BuildMetasTable(progress, From);

            table.Kpis.Should().HaveCount(3);
            table.Kpis[0].Label.Should().Be("Metas");
            table.Kpis[0].Value.Should().Be("2");
            table.Kpis[1].Label.Should().Be("Meta total");
            table.Kpis[1].Value.Should().Be(13000m.ToString("C", PtBr));
            table.Kpis[2].Label.Should().Be("Realizado total");
            table.Kpis[2].Value.Should().Be(10000m.ToString("C", PtBr));
        }

        [Test]
        public void BuildMetasTable_should_emit_rows_with_period_labels_and_fallback_username()
        {
            IReadOnlyCollection<CommercialGoalProgressModel> progress =
            [
                new CommercialGoalProgressModel { UserName = null, PeriodType = 1, TargetAmount = 3000m, AchievedAmount = 2500m, AchievedDealsCount = 2, PercentAchieved = 83.33m },
                new CommercialGoalProgressModel { UserName = "Alice", PeriodType = 2, TargetAmount = 5000m, AchievedAmount = 5000m, AchievedDealsCount = 4, PercentAchieved = 100.00m },
                new CommercialGoalProgressModel { UserName = "Bob", PeriodType = 3, TargetAmount = 10000m, AchievedAmount = 7000m, AchievedDealsCount = 6, PercentAchieved = 70.00m }
            ];

            ReportTable table = CommercialReportExportService.BuildMetasTable(progress, From);

            table.Rows.Should().HaveCount(3);
            table.Rows[0][0].Should().Be("Agência");
            table.Rows[0][1].Should().Be("Mensal");
            table.Rows[0][2].Should().Be(3000m.ToString("C", PtBr));
            table.Rows[0][3].Should().Be(2500m.ToString("C", PtBr));
            table.Rows[0][4].Should().Be("2");
            table.Rows[0][5].Should().Be(83.33m.ToString("0.00", PtBr) + "%");
            table.Rows[1][0].Should().Be("Alice");
            table.Rows[1][1].Should().Be("Trimestral");
            table.Rows[2][1].Should().Be("Anual");
        }

        [Test]
        public void BuildMetasTable_should_use_dash_for_unknown_period_type()
        {
            IReadOnlyCollection<CommercialGoalProgressModel> progress =
            [
                new CommercialGoalProgressModel { UserName = "Teste", PeriodType = 99, TargetAmount = 1000m, AchievedAmount = 500m, AchievedDealsCount = 1, PercentAchieved = 50.00m }
            ];

            ReportTable table = CommercialReportExportService.BuildMetasTable(progress, From);

            table.Rows[0][1].Should().Be("-");
        }

        // -----------------------------------------------------------------------
        // BuildProposalsFunnelTable
        // -----------------------------------------------------------------------

        [Test]
        public void BuildProposalsFunnelTable_should_set_title_subtitle_and_columns()
        {
            ProposalsFunnelModel model = new();

            ReportTable table = CommercialReportExportService.BuildProposalsFunnelTable(model, From, To);

            table.Title.Should().Be("Propostas: Emitidas × Aceitas");
            table.Subtitle.Should().Contain("01/05/2026");
            table.Columns.Should().ContainInOrder("Métrica", "Quantidade", "Valor");
        }

        [Test]
        public void BuildProposalsFunnelTable_should_have_three_kpis_and_four_rows()
        {
            ProposalsFunnelModel model = new()
            {
                EmittedCount = 10,
                EmittedValue = 50000m,
                AcceptedCount = 6,
                AcceptedValue = 30000m,
                RejectedCount = 4,
                AcceptanceRate = 60.00m
            };

            ReportTable table = CommercialReportExportService.BuildProposalsFunnelTable(model, From, To);

            table.Kpis.Should().HaveCount(3);
            table.Kpis[0].Label.Should().Be("Emitidas");
            table.Kpis[0].Value.Should().Be("10");
            table.Kpis[1].Label.Should().Be("Aceitas");
            table.Kpis[1].Value.Should().Be("6");
            table.Kpis[2].Label.Should().Be("Taxa de aceite");
            table.Kpis[2].Value.Should().Be(60.00m.ToString("0.00", PtBr) + "%");

            table.Rows.Should().HaveCount(4);
            table.Rows[0][0].Should().Be("Emitidas");
            table.Rows[0][1].Should().Be("10");
            table.Rows[0][2].Should().Be(50000m.ToString("C", PtBr));
            table.Rows[1][0].Should().Be("Aceitas");
            table.Rows[1][2].Should().Be(30000m.ToString("C", PtBr));
            table.Rows[2][0].Should().Be("Rejeitadas");
            table.Rows[2][1].Should().Be("4");
            table.Rows[2][2].Should().Be("-");
            table.Rows[3][0].Should().Be("Taxa de aceite");
            table.Rows[3][2].Should().Be("-");
        }

        // -----------------------------------------------------------------------
        // BuildBrandRankingTable
        // -----------------------------------------------------------------------

        [Test]
        public void BuildBrandRankingTable_should_set_title_subtitle_and_columns()
        {
            BrandRankingModel model = new() { Lines = [] };

            ReportTable table = CommercialReportExportService.BuildBrandRankingTable(model, From, To);

            table.Title.Should().Be("Ranking por Marca");
            table.Subtitle.Should().Contain("01/05/2026");
            table.Columns.Should().ContainInOrder("Marca", "Ganhos", "Perdas", "Valor ganho", "Win rate");
        }

        [Test]
        public void BuildBrandRankingTable_should_have_one_kpi_with_brand_count_and_emit_rows()
        {
            BrandRankingModel model = new()
            {
                Lines =
                [
                    new BrandRankingLineModel { BrandName = "Acme", WonCount = 5, LostCount = 2, WonValue = 25000m, WinRate = 71.43m },
                    new BrandRankingLineModel { BrandName = "Beta", WonCount = 3, LostCount = 1, WonValue = 12000m, WinRate = 75.00m }
                ]
            };

            ReportTable table = CommercialReportExportService.BuildBrandRankingTable(model, From, To);

            table.Kpis.Should().HaveCount(1);
            table.Kpis[0].Label.Should().Be("Marcas");
            table.Kpis[0].Value.Should().Be("2");

            table.Rows.Should().HaveCount(2);
            table.Rows[0][0].Should().Be("Acme");
            table.Rows[0][1].Should().Be("5");
            table.Rows[0][2].Should().Be("2");
            table.Rows[0][3].Should().Be(25000m.ToString("C", PtBr));
            table.Rows[0][4].Should().Be(71.43m.ToString("0.00", PtBr) + "%");
            table.Rows[1][0].Should().Be("Beta");
        }
    }
}
