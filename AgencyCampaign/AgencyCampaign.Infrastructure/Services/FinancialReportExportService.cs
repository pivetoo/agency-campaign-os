using System.Globalization;
using System.Text;
using AgencyCampaign.Application.Export;
using AgencyCampaign.Application.Models.Financial;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.ValueObjects;

namespace AgencyCampaign.Infrastructure.Services
{
    // Exportacao CSV dos relatorios financeiros (D7). Reaproveita o IFinancialReportService (mesma agregacao
    // da tela, nunca diverge por construcao). Saida UTF-8 COM BOM e decimal/virgula pt-BR para abrir direto
    // no Excel pt-BR. Sempre emite ao menos o cabecalho (bytes nunca vazios -> evita Http400 em periodo seco).
    public sealed class FinancialReportExportService : IFinancialReportExportService
    {
        private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");
        private static readonly UTF8Encoding Utf8WithBom = new(encoderShouldEmitUTF8Identifier: true);

        private readonly IFinancialReportService reportService;

        public FinancialReportExportService(IFinancialReportService reportService)
        {
            this.reportService = reportService;
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
    }
}
