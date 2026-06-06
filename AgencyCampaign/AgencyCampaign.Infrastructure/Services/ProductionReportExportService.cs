using System.Globalization;
using System.Text;
using AgencyCampaign.Application.Export;
using AgencyCampaign.Application.Models.Production;
using AgencyCampaign.Application.Services;

namespace AgencyCampaign.Infrastructure.Services
{
    // Exportacao CSV dos relatorios de producao. Reaproveita IProductionReportService (mesma agregacao
    // da tela, nunca diverge por construcao). Saida UTF-8 COM BOM e decimal/virgula pt-BR para abrir
    // direto no Excel pt-BR. Sempre emite ao menos o cabecalho (bytes nunca vazios).
    public sealed class ProductionReportExportService : IProductionReportExportService
    {
        private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");
        private static readonly UTF8Encoding Utf8WithBom = new(encoderShouldEmitUTF8Identifier: true);

        private readonly IProductionReportService reportService;

        public ProductionReportExportService(IProductionReportService reportService)
        {
            this.reportService = reportService;
        }

        public async Task<byte[]> ExportCampaignPerformance(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
        {
            CampaignPerformanceModel report = await reportService.GetCampaignPerformance(from, to, cancellationToken);

            List<IReadOnlyList<string>> rows = report.Lines
                .Select(line => (IReadOnlyList<string>)
                [
                    line.CampaignName,
                    line.BrandName ?? string.Empty,
                    Num(line.Deliverables),
                    Num(line.TotalReach),
                    Num(line.TotalImpressions),
                    Num(line.TotalEngagement),
                    line.AvgEngagementRate.HasValue ? Money(line.AvgEngagementRate.Value) : string.Empty,
                    line.Emv.HasValue ? Money(line.Emv.Value) : string.Empty
                ])
                .ToList();

            string csv = CsvWriter.Build(["Campanha", "Marca", "Entregaveis", "Alcance", "Impressoes", "Engajamento", "Taxa eng. (%)", "EMV"], rows);
            return Bytes(csv);
        }

        public async Task<byte[]> ExportCreatorPerformance(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
        {
            CreatorPerformanceModel report = await reportService.GetCreatorPerformance(from, to, cancellationToken);

            List<IReadOnlyList<string>> rows = report.Lines
                .Select(line => (IReadOnlyList<string>)
                [
                    line.CreatorName,
                    Num(line.Campaigns),
                    Num(line.Deliverables),
                    Num(line.TotalReach),
                    Num(line.TotalEngagement),
                    line.AvgEngagementRate.HasValue ? Money(line.AvgEngagementRate.Value) : string.Empty
                ])
                .ToList();

            string csv = CsvWriter.Build(["Creator", "Campanhas", "Entregaveis", "Alcance", "Engajamento", "Taxa eng. (%)"], rows);
            return Bytes(csv);
        }

        public async Task<byte[]> ExportPlatformProduction(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
        {
            PlatformProductionModel report = await reportService.GetPlatformProduction(from, to, cancellationToken);

            List<IReadOnlyList<string>> rows = report.Lines
                .Select(line => (IReadOnlyList<string>)
                [
                    line.PlatformName,
                    Num(line.Deliverables),
                    Num(line.TotalReach),
                    Num(line.TotalImpressions),
                    Num(line.TotalEngagement),
                    line.AvgEngagementRate.HasValue ? Money(line.AvgEngagementRate.Value) : string.Empty
                ])
                .ToList();

            string csv = CsvWriter.Build(["Plataforma", "Entregaveis", "Alcance", "Impressoes", "Engajamento", "Taxa eng. (%)"], rows);
            return Bytes(csv);
        }

        public async Task<byte[]> ExportDeliverableSla(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
        {
            DeliverableSlaModel report = await reportService.GetDeliverableSla(from, to, cancellationToken);

            List<IReadOnlyList<string>> rows = report.ByCampaign
                .Select(line => (IReadOnlyList<string>)
                [
                    line.CampaignName,
                    Num(line.Total),
                    Num(line.PublishedOnTime),
                    Num(line.PublishedLate),
                    Num(line.Overdue),
                    Num(line.Upcoming)
                ])
                .ToList();

            int overallTotal = report.PublishedOnTime + report.PublishedLate + report.Overdue + report.Upcoming;
            rows.Add(
            [
                "Total",
                Num(overallTotal),
                Num(report.PublishedOnTime),
                Num(report.PublishedLate),
                Num(report.Overdue),
                Num(report.Upcoming)
            ]);

            string csv = CsvWriter.Build(["Campanha", "Total", "No prazo", "Atrasados", "Vencidos", "A vencer"], rows);
            return Bytes(csv);
        }

        public async Task<byte[]> ExportApprovalCycle(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
        {
            ApprovalCycleModel report = await reportService.GetApprovalCycle(from, to, cancellationToken);

            List<IReadOnlyList<string>> rows =
            [
                ["Aprovacoes internas", Num(report.InternalApprovedCount)],
                ["Tempo medio aprovacao interna (dias)", report.AvgInternalApprovalDays.HasValue ? Money(report.AvgInternalApprovalDays.Value) : string.Empty],
                ["Aprovacoes da marca", Num(report.BrandApprovedCount)],
                ["Tempo medio aprovacao marca (dias)", report.AvgBrandApprovalDays.HasValue ? Money(report.AvgBrandApprovalDays.Value) : string.Empty],
                ["Conteudos aprovados", Num(report.ContentApprovedCount)],
                ["Rodadas medias", report.AvgRounds.HasValue ? Money(report.AvgRounds.Value) : string.Empty],
                ["Aprovado na 1a rodada (%)", report.FirstRoundApprovalRate.HasValue ? Money(report.FirstRoundApprovalRate.Value) : string.Empty]
            ];

            string csv = CsvWriter.Build(["Metrica", "Valor"], rows);
            return Bytes(csv);
        }

        public async Task<byte[]> ExportContentLicenses(int expiringSoonDays, CancellationToken cancellationToken = default)
        {
            ContentLicenseReportModel report = await reportService.GetContentLicenses(expiringSoonDays, cancellationToken);

            List<IReadOnlyList<string>> rows = report.Lines
                .Select(line => (IReadOnlyList<string>)
                [
                    line.DeliverableTitle,
                    line.CampaignName ?? string.Empty,
                    LicenseTypeLabel(line.Type),
                    line.Channels ?? string.Empty,
                    Date(line.StartsAt),
                    Date(line.ExpiresAt),
                    line.DaysUntilExpiry.HasValue ? Num(line.DaysUntilExpiry.Value) : string.Empty,
                    LicenseStatusLabel(line.Status)
                ])
                .ToList();

            string csv = CsvWriter.Build(["Entregavel", "Campanha", "Tipo", "Canais", "Inicio", "Expira", "Dias p/ expirar", "Status"], rows);
            return Bytes(csv);
        }

        private static string Money(decimal value)
        {
            return value.ToString("0.00", PtBr);
        }

        private static string Num(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        private static string Num(long value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        private static string Date(DateTimeOffset? value)
        {
            if (!value.HasValue)
            {
                return string.Empty;
            }

            return value.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        }

        private static string LicenseTypeLabel(int type)
        {
            return type switch
            {
                1 => "Reuso UGC",
                2 => "Whitelisting pago",
                3 => "Exclusividade",
                4 => "Outro",
                _ => type.ToString(CultureInfo.InvariantCulture)
            };
        }

        private static string LicenseStatusLabel(int status)
        {
            return status switch
            {
                1 => "Ativa",
                2 => "Expira em breve",
                3 => "Expirada",
                _ => status.ToString(CultureInfo.InvariantCulture)
            };
        }

        private static byte[] Bytes(string csv)
        {
            return Utf8WithBom.GetPreamble().Concat(Utf8WithBom.GetBytes(csv)).ToArray();
        }
    }
}
