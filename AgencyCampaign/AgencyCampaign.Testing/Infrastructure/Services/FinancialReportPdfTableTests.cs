using System.Globalization;
using AgencyCampaign.Application.Models.Financial;
using AgencyCampaign.Application.Models.Reports;
using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Infrastructure.Services;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    // Testa apenas a montagem do ReportTable (puro, sem I/O, sem Chrome).
    // Os metodos BuildXxxTable sao public static em FinancialReportExportService.
    [TestFixture]
    public sealed class FinancialReportPdfTableTests
    {
        private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

        // -----------------------------------------------------------------------
        // BuildCashFlowTable
        // -----------------------------------------------------------------------

        [Test]
        public void BuildCashFlowTable_should_set_title_subtitle_and_columns()
        {
            DateTimeOffset from = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            DateTimeOffset to = new(2026, 1, 31, 0, 0, 0, TimeSpan.Zero);
            CashFlowSeriesModel series = new()
            {
                From = from,
                To = to,
                Granularity = CashFlowGranularity.Month,
                Pending =
                [
                    new CashFlowPointModel { Bucket = from, Inflow = 5000m, Outflow = 2000m }
                ],
                Settled = []
            };

            ReportTable table = FinancialReportExportService.BuildCashFlowTable(series, from, to);

            table.Title.Should().Be("Fluxo de Caixa");
            table.Subtitle.Should().Contain("01/01/2026");
            table.Columns.Should().ContainInOrder("Tipo", "Período", "Entrada", "Saída", "Líquido");
        }

        [Test]
        public void BuildCashFlowTable_should_emit_pending_and_settled_rows_with_brl_values()
        {
            DateTimeOffset bucket = new(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);
            CashFlowSeriesModel series = new()
            {
                Pending = [new CashFlowPointModel { Bucket = bucket, Inflow = 1200m, Outflow = 300m }],
                Settled = [new CashFlowPointModel { Bucket = bucket, Inflow = 800m, Outflow = 100m }]
            };

            ReportTable table = FinancialReportExportService.BuildCashFlowTable(series, bucket, bucket);

            table.Rows.Should().HaveCount(2);
            table.Rows[0][0].Should().Be("Previsto");
            table.Rows[0][2].Should().Be(1200m.ToString("C", PtBr));
            table.Rows[1][0].Should().Be("Realizado");
            table.Rows[1][2].Should().Be(800m.ToString("C", PtBr));
        }

        [Test]
        public void BuildCashFlowTable_should_have_four_kpis_with_correct_labels()
        {
            CashFlowSeriesModel series = new()
            {
                Pending = [new CashFlowPointModel { Bucket = DateTimeOffset.UtcNow, Inflow = 500m, Outflow = 200m }],
                Settled = [new CashFlowPointModel { Bucket = DateTimeOffset.UtcNow, Inflow = 400m, Outflow = 100m }]
            };

            ReportTable table = FinancialReportExportService.BuildCashFlowTable(series, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

            table.Kpis.Should().HaveCount(4);
            table.Kpis.Select(k => k.Label).Should().ContainInOrder("A receber", "A pagar", "Recebido", "Pago");
            table.Kpis.First(k => k.Label == "A receber").Value.Should().Be(500m.ToString("C", PtBr));
        }

        // -----------------------------------------------------------------------
        // BuildAgingTable
        // -----------------------------------------------------------------------

        [Test]
        public void BuildAgingTable_should_set_title_and_columns()
        {
            AgingReportModel report = new()
            {
                GeneratedAt = new DateTimeOffset(2026, 3, 10, 0, 0, 0, TimeSpan.Zero),
                Buckets =
                [
                    new AgingBucketModel { Label = "Vencido > 90d", TotalReceivable = 2000m, ReceivableCount = 3, TotalPayable = 500m, PayableCount = 1 }
                ]
            };

            ReportTable table = FinancialReportExportService.BuildAgingTable(report);

            table.Title.Should().Be("Aging");
            table.Subtitle.Should().Contain("10/03/2026");
            table.Columns.Should().ContainInOrder("Faixa", "A receber", "Qtd a receber", "A pagar", "Qtd a pagar");
        }

        [Test]
        public void BuildAgingTable_should_emit_one_row_per_bucket_with_brl_values()
        {
            AgingReportModel report = new()
            {
                Buckets =
                [
                    new AgingBucketModel { Label = "Corrente", TotalReceivable = 1000m, ReceivableCount = 2, TotalPayable = 300m, PayableCount = 1 },
                    new AgingBucketModel { Label = "1-30d", TotalReceivable = 400m, ReceivableCount = 1, TotalPayable = 0m, PayableCount = 0 }
                ]
            };

            ReportTable table = FinancialReportExportService.BuildAgingTable(report);

            table.Rows.Should().HaveCount(2);
            table.Rows[0][0].Should().Be("Corrente");
            table.Rows[0][1].Should().Be(1000m.ToString("C", PtBr));
            table.Rows[0][2].Should().Be("2");
        }

        [Test]
        public void BuildAgingTable_should_have_two_kpis_summing_all_buckets()
        {
            AgingReportModel report = new()
            {
                Buckets =
                [
                    new AgingBucketModel { TotalReceivable = 1000m, TotalPayable = 200m },
                    new AgingBucketModel { TotalReceivable = 500m, TotalPayable = 100m }
                ]
            };

            ReportTable table = FinancialReportExportService.BuildAgingTable(report);

            table.Kpis.Should().HaveCount(2);
            table.Kpis[0].Label.Should().Be("Total a receber");
            table.Kpis[0].Value.Should().Be(1500m.ToString("C", PtBr));
            table.Kpis[1].Label.Should().Be("Total a pagar");
            table.Kpis[1].Value.Should().Be(300m.ToString("C", PtBr));
        }

        // -----------------------------------------------------------------------
        // BuildTaxWithholdingTable
        // -----------------------------------------------------------------------

        [Test]
        public void BuildTaxWithholdingTable_should_set_title_subtitle_and_columns()
        {
            DateTimeOffset from = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            DateTimeOffset to = new(2026, 3, 31, 0, 0, 0, TimeSpan.Zero);
            TaxWithholdingReportModel report = new() { TotalGross = 5000m, TotalWithheld = 500m, TotalNet = 4500m };

            ReportTable table = FinancialReportExportService.BuildTaxWithholdingTable(report, from, to);

            table.Title.Should().Be("Retenções Fiscais");
            table.Subtitle.Should().Contain("01/01/2026");
            table.Columns.Should().ContainInOrder("Creator", "Documento", "Regime", "Bruto", "Retido", "Líquido", "Qtd");
        }

        [Test]
        public void BuildTaxWithholdingTable_should_include_lines_and_total_row()
        {
            TaxWithholdingReportModel report = new()
            {
                TotalGross = 3000m,
                TotalWithheld = 300m,
                TotalNet = 2700m,
                Lines =
                [
                    new TaxWithholdingLineModel { CreatorName = "Ana Silva", Document = "123", TaxRegime = TaxRegime.IndividualPF, GrossAmount = 3000m, TaxWithheld = 300m, NetAmount = 2700m, PaymentCount = 2 }
                ]
            };
            DateTimeOffset from = DateTimeOffset.UtcNow;
            DateTimeOffset to = DateTimeOffset.UtcNow;

            ReportTable table = FinancialReportExportService.BuildTaxWithholdingTable(report, from, to);

            table.Rows.Should().HaveCount(2);
            table.Rows[0][0].Should().Be("Ana Silva");
            table.Rows[0][2].Should().Be("Pessoa Fisica");
            table.Rows[0][3].Should().Be(3000m.ToString("C", PtBr));
            table.Rows[1][0].Should().Be("Total");
        }

        [Test]
        public void BuildTaxWithholdingTable_should_have_three_kpis()
        {
            TaxWithholdingReportModel report = new() { TotalGross = 1000m, TotalWithheld = 100m, TotalNet = 900m };

            ReportTable table = FinancialReportExportService.BuildTaxWithholdingTable(report, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

            table.Kpis.Should().HaveCount(3);
            table.Kpis.Select(k => k.Label).Should().ContainInOrder("Bruto total", "Retido total", "Líquido total");
        }

        // -----------------------------------------------------------------------
        // BuildCampaignProfitabilityTable
        // -----------------------------------------------------------------------

        [Test]
        public void BuildCampaignProfitabilityTable_should_set_title_and_columns()
        {
            CampaignProfitabilityReportModel report = new()
            {
                GeneratedAt = new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero)
            };

            ReportTable table = FinancialReportExportService.BuildCampaignProfitabilityTable(report);

            table.Title.Should().Be("Rentabilidade por Campanha");
            table.Columns.Should().ContainInOrder("Campanha", "Receita", "Custo creator", "Outros custos", "Margem", "Margem %");
        }

        [Test]
        public void BuildCampaignProfitabilityTable_should_include_data_rows_and_total_row()
        {
            CampaignProfitabilityReportModel report = new()
            {
                TotalRevenue = 10000m,
                TotalCreatorCost = 4000m,
                TotalOtherCost = 1000m,
                TotalMargin = 5000m,
                Lines =
                [
                    new CampaignProfitabilityLineModel { CampaignName = "Alpha", Revenue = 6000m, CreatorCost = 2400m, OtherCost = 600m, Margin = 3000m, MarginPercent = 50m },
                    new CampaignProfitabilityLineModel { CampaignName = "Beta", Revenue = 4000m, CreatorCost = 1600m, OtherCost = 400m, Margin = 2000m, MarginPercent = 50m }
                ]
            };

            ReportTable table = FinancialReportExportService.BuildCampaignProfitabilityTable(report);

            table.Rows.Should().HaveCount(3);
            table.Rows[0][0].Should().Be("Alpha");
            table.Rows[0][5].Should().Be(50m.ToString("0.00", PtBr) + "%");
            table.Rows[2][0].Should().Be("Total");
            table.Rows[2][1].Should().Be(10000m.ToString("C", PtBr));
        }

        [Test]
        public void BuildCampaignProfitabilityTable_should_have_three_kpis()
        {
            CampaignProfitabilityReportModel report = new() { TotalRevenue = 8000m, TotalCreatorCost = 3000m, TotalMargin = 5000m };

            ReportTable table = FinancialReportExportService.BuildCampaignProfitabilityTable(report);

            table.Kpis.Should().HaveCount(3);
            table.Kpis[0].Label.Should().Be("Receita total");
            table.Kpis[0].Value.Should().Be(8000m.ToString("C", PtBr));
        }

        // -----------------------------------------------------------------------
        // BuildAccrualResultTable
        // -----------------------------------------------------------------------

        [Test]
        public void BuildAccrualResultTable_should_set_title_subtitle_and_single_row()
        {
            DateTimeOffset from = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            DateTimeOffset to = new(2026, 3, 31, 0, 0, 0, TimeSpan.Zero);
            AccrualResultModel result = new()
            {
                From = from,
                To = to,
                Revenue = 20000m,
                Expense = 8000m,
                Result = 12000m
            };

            ReportTable table = FinancialReportExportService.BuildAccrualResultTable(result, from, to);

            table.Title.Should().Be("Resultado (Competência)");
            table.Subtitle.Should().Contain("01/01/2026");
            table.Columns.Should().ContainInOrder("De", "Até", "Receita", "Despesa", "Resultado");
            table.Rows.Should().HaveCount(1);
            table.Rows[0][2].Should().Be(20000m.ToString("C", PtBr));
        }

        [Test]
        public void BuildAccrualResultTable_should_have_three_kpis()
        {
            AccrualResultModel result = new() { Revenue = 5000m, Expense = 2000m, Result = 3000m };

            ReportTable table = FinancialReportExportService.BuildAccrualResultTable(result, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

            table.Kpis.Should().HaveCount(3);
            table.Kpis.Select(k => k.Label).Should().ContainInOrder("Receita", "Despesa", "Resultado");
            table.Kpis[2].Value.Should().Be(3000m.ToString("C", PtBr));
        }

        // -----------------------------------------------------------------------
        // BuildCashFlowProjectionTable
        // -----------------------------------------------------------------------

        [Test]
        public void BuildCashFlowProjectionTable_should_set_title_subtitle_and_columns()
        {
            CashFlowProjectionModel projection = new()
            {
                OpeningBalance = 5000m,
                Weeks = 4,
                Series = []
            };

            ReportTable table = FinancialReportExportService.BuildCashFlowProjectionTable(projection, 4);

            table.Title.Should().Be("Projeção de Fluxo");
            table.Subtitle.Should().Contain("4 semanas");
            table.Columns.Should().ContainInOrder("Semana", "Entrada", "Saída", "Líquido", "Saldo projetado");
        }

        [Test]
        public void BuildCashFlowProjectionTable_should_emit_one_row_per_week_with_brl_values()
        {
            DateTimeOffset week1 = new(2026, 6, 9, 0, 0, 0, TimeSpan.Zero);
            CashFlowProjectionModel projection = new()
            {
                OpeningBalance = 1000m,
                Series =
                [
                    new CashFlowProjectionWeekModel { WeekStart = week1, Inflow = 2000m, Outflow = 500m, ProjectedBalance = 2500m }
                ]
            };

            ReportTable table = FinancialReportExportService.BuildCashFlowProjectionTable(projection, 2);

            table.Rows.Should().HaveCount(1);
            table.Rows[0][1].Should().Be(2000m.ToString("C", PtBr));
            table.Rows[0][4].Should().Be(2500m.ToString("C", PtBr));
        }

        [Test]
        public void BuildCashFlowProjectionTable_should_have_opening_balance_kpi()
        {
            CashFlowProjectionModel projection = new() { OpeningBalance = 7500m, Series = [] };

            ReportTable table = FinancialReportExportService.BuildCashFlowProjectionTable(projection, 8);

            table.Kpis.Should().HaveCount(1);
            table.Kpis[0].Label.Should().Be("Saldo de abertura");
            table.Kpis[0].Value.Should().Be(7500m.ToString("C", PtBr));
        }
    }
}
