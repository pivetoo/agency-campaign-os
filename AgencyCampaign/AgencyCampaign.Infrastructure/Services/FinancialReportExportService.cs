using System.Globalization;
using System.Text;
using AgencyCampaign.Application.Export;
using AgencyCampaign.Application.Models.Financial;
using AgencyCampaign.Application.Models.Reports;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.ValueObjects;

namespace AgencyCampaign.Infrastructure.Services
{
    // Exportacao CSV e PDF dos relatorios financeiros (D7). Reaproveita o IFinancialReportService (mesma agregacao
    // da tela, nunca diverge por construcao). CSV: UTF-8 COM BOM e decimal/virgula pt-BR para abrir direto
    // no Excel pt-BR. Sempre emite ao menos o cabecalho (bytes nunca vazios -> evita Http400 em periodo seco).
    // PDF: delega a IReportPdfService apos montar um ReportTable via BuildXxxTable (puro, testavel sem Chrome).
    public sealed class FinancialReportExportService : IFinancialReportExportService
    {
        private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");
        private static readonly UTF8Encoding Utf8WithBom = new(encoderShouldEmitUTF8Identifier: true);

        private readonly IFinancialReportService reportService;
        private readonly IReportPdfService pdfService;

        public FinancialReportExportService(IFinancialReportService reportService, IReportPdfService pdfService)
        {
            this.reportService = reportService;
            this.pdfService = pdfService;
        }

        public async Task<byte[]> ExportCashFlow(DateTimeOffset from, DateTimeOffset to, CashFlowGranularity granularity, CancellationToken cancellationToken = default)
        {
            CashFlowSeriesModel series = await reportService.GetCashFlow(from, to, granularity, cancellationToken);

            List<IReadOnlyList<string>> rows = [];
            foreach (CashFlowPointModel point in series.Pending)
            {
                rows.Add(["Previsto", Date(point.Bucket), Money(point.Inflow), Money(point.Outflow), Money(point.Net)]);
            }

            foreach (CashFlowPointModel point in series.Settled)
            {
                rows.Add(["Realizado", Date(point.Bucket), Money(point.Inflow), Money(point.Outflow), Money(point.Net)]);
            }

            string csv = CsvWriter.Build(["Tipo", "Periodo", "Entrada", "Saida", "Liquido"], rows);
            return Bytes(csv);
        }

        public async Task<byte[]> ExportAging(CancellationToken cancellationToken = default)
        {
            AgingReportModel report = await reportService.GetAgingReport(cancellationToken);

            List<IReadOnlyList<string>> rows = report.Buckets
                .Select(bucket => (IReadOnlyList<string>)
                [
                    bucket.Label,
                    Money(bucket.TotalReceivable),
                    bucket.ReceivableCount.ToString(CultureInfo.InvariantCulture),
                    Money(bucket.TotalPayable),
                    bucket.PayableCount.ToString(CultureInfo.InvariantCulture)
                ])
                .ToList();

            string csv = CsvWriter.Build(["Faixa", "A receber", "Qtd a receber", "A pagar", "Qtd a pagar"], rows);
            return Bytes(csv);
        }

        public async Task<byte[]> ExportTaxWithholding(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
        {
            TaxWithholdingReportModel report = await reportService.GetTaxWithholdingReport(from, to, cancellationToken);

            List<IReadOnlyList<string>> rows = report.Lines
                .Select(line => (IReadOnlyList<string>)
                [
                    line.CreatorName ?? string.Empty,
                    line.Document ?? string.Empty,
                    TaxRegimeLabel(line.TaxRegime),
                    Money(line.GrossAmount),
                    Money(line.TaxWithheld),
                    Money(line.NetAmount),
                    line.PaymentCount.ToString(CultureInfo.InvariantCulture)
                ])
                .ToList();

            rows.Add(["Total", string.Empty, string.Empty, Money(report.TotalGross), Money(report.TotalWithheld), Money(report.TotalNet), string.Empty]);

            string csv = CsvWriter.Build(["Creator", "Documento", "Regime", "Bruto", "Retido", "Liquido", "Qtd pagamentos"], rows);
            return Bytes(csv);
        }

        public async Task<byte[]> ExportCampaignProfitability(CancellationToken cancellationToken = default)
        {
            CampaignProfitabilityReportModel report = await reportService.GetCampaignProfitability(cancellationToken);

            List<IReadOnlyList<string>> rows = report.Lines
                .Select(line => (IReadOnlyList<string>)
                [
                    line.CampaignName ?? string.Empty,
                    Money(line.Revenue),
                    Money(line.CreatorCost),
                    Money(line.OtherCost),
                    Money(line.Margin),
                    Money(line.MarginPercent)
                ])
                .ToList();

            rows.Add(["Total", Money(report.TotalRevenue), Money(report.TotalCreatorCost), Money(report.TotalOtherCost), Money(report.TotalMargin), string.Empty]);

            string csv = CsvWriter.Build(["Campanha", "Receita", "Custo creator", "Outros custos", "Margem", "Margem %"], rows);
            return Bytes(csv);
        }

        public async Task<byte[]> ExportAccrualResult(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
        {
            AccrualResultModel result = await reportService.GetAccrualResult(from, to, cancellationToken);

            List<IReadOnlyList<string>> rows =
            [
                [Date(result.From), Date(result.To), Money(result.Revenue), Money(result.Expense), Money(result.Result)]
            ];

            string csv = CsvWriter.Build(["De", "Ate", "Receita", "Despesa", "Resultado"], rows);
            return Bytes(csv);
        }

        public async Task<byte[]> ExportCashFlowProjection(int weeks, CancellationToken cancellationToken = default)
        {
            CashFlowProjectionModel projection = await reportService.GetCashFlowProjection(weeks, cancellationToken);

            List<IReadOnlyList<string>> rows = projection.Series
                .Select(week => (IReadOnlyList<string>)
                [
                    Date(week.WeekStart),
                    Money(week.Inflow),
                    Money(week.Outflow),
                    Money(week.Net),
                    Money(week.ProjectedBalance)
                ])
                .ToList();

            string csv = CsvWriter.Build(["Semana", "Entrada", "Saida", "Liquido", "Saldo projetado"], rows);
            return Bytes(csv);
        }

        private static string Money(decimal value)
        {
            return value.ToString("0.00", PtBr);
        }

        private static string Date(DateTimeOffset value)
        {
            return value.ToUniversalTime().ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        }

        private static string TaxRegimeLabel(TaxRegime? regime)
        {
            return regime switch
            {
                TaxRegime.IndividualPF => "Pessoa Fisica",
                TaxRegime.Mei => "MEI",
                TaxRegime.SimplesNacional => "Simples Nacional",
                TaxRegime.PresumedRealProfit => "Lucro Presumido/Real",
                _ => string.Empty
            };
        }

        private static byte[] Bytes(string csv)
        {
            return Utf8WithBom.GetPreamble().Concat(Utf8WithBom.GetBytes(csv)).ToArray();
        }

        // Formatos display-friendly para PDF (diferente do CSV que usa separador decimal pt-BR sem simbolo)
        private static string Brl(decimal v) => v.ToString("C", PtBr);

        private static string Pct(decimal v) => v.ToString("0.00", PtBr) + "%";

        // PDF exports — chamam reportService, montam ReportTable via BuildXxxTable (puro) e delegam ao pdfService.

        public async Task<byte[]> ExportCashFlowPdf(DateTimeOffset from, DateTimeOffset to, CashFlowGranularity granularity, CancellationToken cancellationToken = default)
        {
            CashFlowSeriesModel series = await reportService.GetCashFlow(from, to, granularity, cancellationToken);
            ReportTable table = BuildCashFlowTable(series, from, to);
            return await pdfService.GenerateAsync(table, cancellationToken);
        }

        public async Task<byte[]> ExportAgingPdf(CancellationToken cancellationToken = default)
        {
            AgingReportModel report = await reportService.GetAgingReport(cancellationToken);
            ReportTable table = BuildAgingTable(report);
            return await pdfService.GenerateAsync(table, cancellationToken);
        }

        public async Task<byte[]> ExportTaxWithholdingPdf(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
        {
            TaxWithholdingReportModel report = await reportService.GetTaxWithholdingReport(from, to, cancellationToken);
            ReportTable table = BuildTaxWithholdingTable(report, from, to);
            return await pdfService.GenerateAsync(table, cancellationToken);
        }

        public async Task<byte[]> ExportCampaignProfitabilityPdf(CancellationToken cancellationToken = default)
        {
            CampaignProfitabilityReportModel report = await reportService.GetCampaignProfitability(cancellationToken);
            ReportTable table = BuildCampaignProfitabilityTable(report);
            return await pdfService.GenerateAsync(table, cancellationToken);
        }

        public async Task<byte[]> ExportAccrualResultPdf(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
        {
            AccrualResultModel result = await reportService.GetAccrualResult(from, to, cancellationToken);
            ReportTable table = BuildAccrualResultTable(result, from, to);
            return await pdfService.GenerateAsync(table, cancellationToken);
        }

        public async Task<byte[]> ExportCashFlowProjectionPdf(int weeks, CancellationToken cancellationToken = default)
        {
            CashFlowProjectionModel projection = await reportService.GetCashFlowProjection(weeks, cancellationToken);
            ReportTable table = BuildCashFlowProjectionTable(projection, weeks);
            return await pdfService.GenerateAsync(table, cancellationToken);
        }

        // Builders puros (sem I/O) — testáveis sem Chrome

        public static ReportTable BuildCashFlowTable(CashFlowSeriesModel series, DateTimeOffset from, DateTimeOffset to)
        {
            List<ReportKpi> kpis =
            [
                new ReportKpi { Label = "A receber", Value = Brl(series.Pending.Sum(p => p.Inflow)) },
                new ReportKpi { Label = "A pagar", Value = Brl(series.Pending.Sum(p => p.Outflow)) },
                new ReportKpi { Label = "Recebido", Value = Brl(series.Settled.Sum(p => p.Inflow)) },
                new ReportKpi { Label = "Pago", Value = Brl(series.Settled.Sum(p => p.Outflow)) }
            ];

            List<IReadOnlyList<string>> rows = [];
            foreach (CashFlowPointModel point in series.Pending)
            {
                rows.Add(["Previsto", Date(point.Bucket), Brl(point.Inflow), Brl(point.Outflow), Brl(point.Net)]);
            }

            foreach (CashFlowPointModel point in series.Settled)
            {
                rows.Add(["Realizado", Date(point.Bucket), Brl(point.Inflow), Brl(point.Outflow), Brl(point.Net)]);
            }

            return new ReportTable
            {
                Title = "Fluxo de Caixa",
                Subtitle = $"Período {Date(from)} a {Date(to)}",
                GeneratedAt = DateTimeOffset.UtcNow,
                Kpis = kpis,
                Columns = ["Tipo", "Período", "Entrada", "Saída", "Líquido"],
                Rows = rows
            };
        }

        public static ReportTable BuildAgingTable(AgingReportModel report)
        {
            List<ReportKpi> kpis =
            [
                new ReportKpi { Label = "Total a receber", Value = Brl(report.Buckets.Sum(b => b.TotalReceivable)) },
                new ReportKpi { Label = "Total a pagar", Value = Brl(report.Buckets.Sum(b => b.TotalPayable)) }
            ];

            List<IReadOnlyList<string>> rows = report.Buckets
                .Select(bucket => (IReadOnlyList<string>)
                [
                    bucket.Label,
                    Brl(bucket.TotalReceivable),
                    bucket.ReceivableCount.ToString(CultureInfo.InvariantCulture),
                    Brl(bucket.TotalPayable),
                    bucket.PayableCount.ToString(CultureInfo.InvariantCulture)
                ])
                .ToList();

            return new ReportTable
            {
                Title = "Aging",
                Subtitle = $"Gerado em {Date(report.GeneratedAt)}",
                GeneratedAt = DateTimeOffset.UtcNow,
                Kpis = kpis,
                Columns = ["Faixa", "A receber", "Qtd a receber", "A pagar", "Qtd a pagar"],
                Rows = rows
            };
        }

        public static ReportTable BuildTaxWithholdingTable(TaxWithholdingReportModel report, DateTimeOffset from, DateTimeOffset to)
        {
            List<ReportKpi> kpis =
            [
                new ReportKpi { Label = "Bruto total", Value = Brl(report.TotalGross) },
                new ReportKpi { Label = "Retido total", Value = Brl(report.TotalWithheld) },
                new ReportKpi { Label = "Líquido total", Value = Brl(report.TotalNet) }
            ];

            List<IReadOnlyList<string>> rows = report.Lines
                .Select(line => (IReadOnlyList<string>)
                [
                    line.CreatorName ?? string.Empty,
                    line.Document ?? string.Empty,
                    TaxRegimeLabel(line.TaxRegime),
                    Brl(line.GrossAmount),
                    Brl(line.TaxWithheld),
                    Brl(line.NetAmount),
                    line.PaymentCount.ToString(CultureInfo.InvariantCulture)
                ])
                .ToList();

            rows.Add(["Total", string.Empty, string.Empty, Brl(report.TotalGross), Brl(report.TotalWithheld), Brl(report.TotalNet), string.Empty]);

            return new ReportTable
            {
                Title = "Retenções Fiscais",
                Subtitle = $"Período {Date(from)} a {Date(to)}",
                GeneratedAt = DateTimeOffset.UtcNow,
                Kpis = kpis,
                Columns = ["Creator", "Documento", "Regime", "Bruto", "Retido", "Líquido", "Qtd"],
                Rows = rows
            };
        }

        public static ReportTable BuildCampaignProfitabilityTable(CampaignProfitabilityReportModel report)
        {
            List<ReportKpi> kpis =
            [
                new ReportKpi { Label = "Receita total", Value = Brl(report.TotalRevenue) },
                new ReportKpi { Label = "Custo creators", Value = Brl(report.TotalCreatorCost) },
                new ReportKpi { Label = "Margem total", Value = Brl(report.TotalMargin) }
            ];

            List<IReadOnlyList<string>> rows = report.Lines
                .Select(line => (IReadOnlyList<string>)
                [
                    line.CampaignName ?? string.Empty,
                    Brl(line.Revenue),
                    Brl(line.CreatorCost),
                    Brl(line.OtherCost),
                    Brl(line.Margin),
                    Pct(line.MarginPercent)
                ])
                .ToList();

            rows.Add(["Total", Brl(report.TotalRevenue), Brl(report.TotalCreatorCost), Brl(report.TotalOtherCost), Brl(report.TotalMargin), string.Empty]);

            return new ReportTable
            {
                Title = "Rentabilidade por Campanha",
                Subtitle = $"Gerado em {Date(report.GeneratedAt)}",
                GeneratedAt = DateTimeOffset.UtcNow,
                Kpis = kpis,
                Columns = ["Campanha", "Receita", "Custo creator", "Outros custos", "Margem", "Margem %"],
                Rows = rows
            };
        }

        public static ReportTable BuildAccrualResultTable(AccrualResultModel result, DateTimeOffset from, DateTimeOffset to)
        {
            List<ReportKpi> kpis =
            [
                new ReportKpi { Label = "Receita", Value = Brl(result.Revenue) },
                new ReportKpi { Label = "Despesa", Value = Brl(result.Expense) },
                new ReportKpi { Label = "Resultado", Value = Brl(result.Result) }
            ];

            List<IReadOnlyList<string>> rows =
            [
                [Date(result.From), Date(result.To), Brl(result.Revenue), Brl(result.Expense), Brl(result.Result)]
            ];

            return new ReportTable
            {
                Title = "Resultado (Competência)",
                Subtitle = $"Período {Date(from)} a {Date(to)}",
                GeneratedAt = DateTimeOffset.UtcNow,
                Kpis = kpis,
                Columns = ["De", "Até", "Receita", "Despesa", "Resultado"],
                Rows = rows
            };
        }

        public static ReportTable BuildCashFlowProjectionTable(CashFlowProjectionModel projection, int weeks)
        {
            List<ReportKpi> kpis =
            [
                new ReportKpi { Label = "Saldo de abertura", Value = Brl(projection.OpeningBalance) }
            ];

            List<IReadOnlyList<string>> rows = projection.Series
                .Select(week => (IReadOnlyList<string>)
                [
                    Date(week.WeekStart),
                    Brl(week.Inflow),
                    Brl(week.Outflow),
                    Brl(week.Net),
                    Brl(week.ProjectedBalance)
                ])
                .ToList();

            return new ReportTable
            {
                Title = "Projeção de Fluxo",
                Subtitle = $"Horizonte de {weeks} semanas",
                GeneratedAt = DateTimeOffset.UtcNow,
                Kpis = kpis,
                Columns = ["Semana", "Entrada", "Saída", "Líquido", "Saldo projetado"],
                Rows = rows
            };
        }
    }
}
