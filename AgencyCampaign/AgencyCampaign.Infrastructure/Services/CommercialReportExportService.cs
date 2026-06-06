using System.Globalization;
using System.Text;
using AgencyCampaign.Application.Export;
using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Application.Services;

namespace AgencyCampaign.Infrastructure.Services
{
    // Exportacao CSV dos relatorios comerciais. Reaproveita ICommercialReportService (mesma agregacao
    // da tela, nunca diverge por construcao). Saida UTF-8 COM BOM e decimal/virgula pt-BR para abrir
    // direto no Excel pt-BR. Sempre emite ao menos o cabecalho (bytes nunca vazios).
    public sealed class CommercialReportExportService : ICommercialReportExportService
    {
        private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");
        private static readonly UTF8Encoding Utf8WithBom = new(encoderShouldEmitUTF8Identifier: true);

        private readonly ICommercialReportService reportService;

        public CommercialReportExportService(ICommercialReportService reportService)
        {
            this.reportService = reportService;
        }

        public async Task<byte[]> ExportProposalsFunnel(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
        {
            ProposalsFunnelModel funnel = await reportService.GetProposalsFunnel(from, to, cancellationToken);

            List<IReadOnlyList<string>> rows =
            [
                ["Emitidas", funnel.EmittedCount.ToString(CultureInfo.InvariantCulture), Money(funnel.EmittedValue)],
                ["Aceitas", funnel.AcceptedCount.ToString(CultureInfo.InvariantCulture), Money(funnel.AcceptedValue)],
                ["Rejeitadas", funnel.RejectedCount.ToString(CultureInfo.InvariantCulture), string.Empty],
                ["Taxa de aceite (%)", Money(funnel.AcceptanceRate), string.Empty]
            ];

            string csv = CsvWriter.Build(["Metrica", "Quantidade", "Valor"], rows);
            return Bytes(csv);
        }

        public async Task<byte[]> ExportBrandRanking(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
        {
            BrandRankingModel ranking = await reportService.GetBrandRanking(from, to, cancellationToken);

            List<IReadOnlyList<string>> rows = ranking.Lines
                .Select(line => (IReadOnlyList<string>)
                [
                    line.BrandName,
                    line.WonCount.ToString(CultureInfo.InvariantCulture),
                    line.LostCount.ToString(CultureInfo.InvariantCulture),
                    Money(line.WonValue),
                    Money(line.WinRate)
                ])
                .ToList();

            string csv = CsvWriter.Build(["Marca", "Ganhos", "Perdas", "Valor ganho", "Win rate (%)"], rows);
            return Bytes(csv);
        }

        private static string Money(decimal value)
        {
            return value.ToString("0.00", PtBr);
        }

        private static byte[] Bytes(string csv)
        {
            return Utf8WithBom.GetPreamble().Concat(Utf8WithBom.GetBytes(csv)).ToArray();
        }
    }
}
