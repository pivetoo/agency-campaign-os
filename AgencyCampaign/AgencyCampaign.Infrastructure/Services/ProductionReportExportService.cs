using System.Globalization;
using System.Text;
using AgencyCampaign.Application.Export;
using AgencyCampaign.Application.Models.Production;
using AgencyCampaign.Application.Models.Reports;
using AgencyCampaign.Application.Services;

namespace AgencyCampaign.Infrastructure.Services
{
    // Exportacao CSV e PDF dos relatorios de producao. Reaproveita IProductionReportService (mesma
    // agregacao da tela, nunca diverge por construcao). CSV: UTF-8 COM BOM e decimal/virgula pt-BR
    // para abrir direto no Excel pt-BR. Sempre emite ao menos o cabecalho (bytes nunca vazios).
    // PDF: delega a IReportPdfService apos montar um ReportTable via BuildXxxTable (puro, testavel sem Chrome).
    public sealed class ProductionReportExportService : IProductionReportExportService
    {
        private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");
        private static readonly UTF8Encoding Utf8WithBom = new(encoderShouldEmitUTF8Identifier: true);

        private readonly IProductionReportService reportService;
        private readonly IReportPdfService pdfService;

        public ProductionReportExportService(IProductionReportService reportService, IReportPdfService pdfService)
        {
            this.reportService = reportService;
            this.pdfService = pdfService;
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

        // Formatos display-friendly para PDF (diferente do CSV que usa separador decimal pt-BR sem simbolo)
        private static string Brl(decimal v) => v.ToString("C", PtBr);

        private static string Pct(decimal v) => v.ToString("0.00", PtBr) + "%";

        private static string Days(decimal? v) => v.HasValue ? v.Value.ToString("0.0", PtBr) + " dias" : "-";

        private static string NumOrDash(decimal? v) => v.HasValue ? v.Value.ToString("0.00", PtBr) : "-";

        private static string RateOrDash(decimal? v) => v.HasValue ? Pct(v.Value) : "-";

        // PDF exports — chamam reportService, montam ReportTable via BuildXxxTable (puro) e delegam ao pdfService.

        public async Task<byte[]> ExportCampaignPerformancePdf(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
        {
            CampaignPerformanceModel report = await reportService.GetCampaignPerformance(from, to, cancellationToken);
            ReportTable table = BuildCampaignPerformanceTable(report, from, to);
            return await pdfService.GenerateAsync(table, cancellationToken);
        }

        public async Task<byte[]> ExportCreatorPerformancePdf(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
        {
            CreatorPerformanceModel report = await reportService.GetCreatorPerformance(from, to, cancellationToken);
            ReportTable table = BuildCreatorPerformanceTable(report, from, to);
            return await pdfService.GenerateAsync(table, cancellationToken);
        }

        public async Task<byte[]> ExportPlatformProductionPdf(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
        {
            PlatformProductionModel report = await reportService.GetPlatformProduction(from, to, cancellationToken);
            ReportTable table = BuildPlatformProductionTable(report, from, to);
            return await pdfService.GenerateAsync(table, cancellationToken);
        }

        public async Task<byte[]> ExportDeliverableSlaPdf(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
        {
            DeliverableSlaModel report = await reportService.GetDeliverableSla(from, to, cancellationToken);
            ReportTable table = BuildDeliverableSlaTable(report, from, to);
            return await pdfService.GenerateAsync(table, cancellationToken);
        }

        public async Task<byte[]> ExportApprovalCyclePdf(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
        {
            ApprovalCycleModel report = await reportService.GetApprovalCycle(from, to, cancellationToken);
            ReportTable table = BuildApprovalCycleTable(report, from, to);
            return await pdfService.GenerateAsync(table, cancellationToken);
        }

        public async Task<byte[]> ExportContentLicensesPdf(int expiringSoonDays, CancellationToken cancellationToken = default)
        {
            ContentLicenseReportModel report = await reportService.GetContentLicenses(expiringSoonDays, cancellationToken);
            ReportTable table = BuildContentLicensesTable(report);
            return await pdfService.GenerateAsync(table, cancellationToken);
        }

        // Builders puros (sem I/O) — testáveis sem Chrome

        public static ReportTable BuildCampaignPerformanceTable(CampaignPerformanceModel model, DateTimeOffset from, DateTimeOffset to)
        {
            List<ReportKpi> kpis =
            [
                new ReportKpi { Label = "Campanhas", Value = Num(model.Lines.Count) },
                new ReportKpi { Label = "Alcance total", Value = Num(model.Lines.Sum(l => l.TotalReach)) },
                new ReportKpi { Label = "Engajamento total", Value = Num(model.Lines.Sum(l => l.TotalEngagement)) },
                new ReportKpi { Label = "EMV total", Value = Brl(model.Lines.Sum(l => l.Emv ?? 0m)) }
            ];

            List<IReadOnlyList<string>> rows = model.Lines
                .Select(line => (IReadOnlyList<string>)
                [
                    line.CampaignName,
                    line.BrandName ?? "-",
                    Num(line.Deliverables),
                    Num(line.TotalReach),
                    Num(line.TotalImpressions),
                    Num(line.TotalEngagement),
                    RateOrDash(line.AvgEngagementRate),
                    line.Emv.HasValue ? Brl(line.Emv.Value) : "-"
                ])
                .ToList();

            return new ReportTable
            {
                Title = "Performance de Campanhas",
                Subtitle = $"Período {Date(from)} a {Date(to)}",
                GeneratedAt = DateTimeOffset.UtcNow,
                Kpis = kpis,
                Columns = ["Campanha", "Marca", "Entregas", "Alcance", "Impressões", "Engajamento", "Taxa eng.", "EMV"],
                Rows = rows
            };
        }

        public static ReportTable BuildCreatorPerformanceTable(CreatorPerformanceModel model, DateTimeOffset from, DateTimeOffset to)
        {
            List<ReportKpi> kpis =
            [
                new ReportKpi { Label = "Creators", Value = Num(model.Lines.Count) },
                new ReportKpi { Label = "Alcance total", Value = Num(model.Lines.Sum(l => l.TotalReach)) },
                new ReportKpi { Label = "Engajamento total", Value = Num(model.Lines.Sum(l => l.TotalEngagement)) }
            ];

            List<IReadOnlyList<string>> rows = model.Lines
                .Select(line => (IReadOnlyList<string>)
                [
                    line.CreatorName,
                    Num(line.Campaigns),
                    Num(line.Deliverables),
                    Num(line.TotalReach),
                    Num(line.TotalEngagement),
                    RateOrDash(line.AvgEngagementRate)
                ])
                .ToList();

            return new ReportTable
            {
                Title = "Desempenho por Creator",
                Subtitle = $"Período {Date(from)} a {Date(to)}",
                GeneratedAt = DateTimeOffset.UtcNow,
                Kpis = kpis,
                Columns = ["Creator", "Campanhas", "Entregas", "Alcance", "Engajamento", "Taxa eng."],
                Rows = rows
            };
        }

        public static ReportTable BuildPlatformProductionTable(PlatformProductionModel model, DateTimeOffset from, DateTimeOffset to)
        {
            List<ReportKpi> kpis =
            [
                new ReportKpi { Label = "Plataformas", Value = Num(model.Lines.Count) },
                new ReportKpi { Label = "Alcance total", Value = Num(model.Lines.Sum(l => l.TotalReach)) },
                new ReportKpi { Label = "Engajamento total", Value = Num(model.Lines.Sum(l => l.TotalEngagement)) }
            ];

            List<IReadOnlyList<string>> rows = model.Lines
                .Select(line => (IReadOnlyList<string>)
                [
                    line.PlatformName,
                    Num(line.Deliverables),
                    Num(line.TotalReach),
                    Num(line.TotalImpressions),
                    Num(line.TotalEngagement),
                    RateOrDash(line.AvgEngagementRate)
                ])
                .ToList();

            return new ReportTable
            {
                Title = "Produção por Plataforma",
                Subtitle = $"Período {Date(from)} a {Date(to)}",
                GeneratedAt = DateTimeOffset.UtcNow,
                Kpis = kpis,
                Columns = ["Plataforma", "Entregas", "Alcance", "Impressões", "Engajamento", "Taxa eng."],
                Rows = rows
            };
        }

        public static ReportTable BuildDeliverableSlaTable(DeliverableSlaModel model, DateTimeOffset from, DateTimeOffset to)
        {
            List<ReportKpi> kpis =
            [
                new ReportKpi { Label = "No prazo", Value = Num(model.PublishedOnTime) },
                new ReportKpi { Label = "Atrasados", Value = Num(model.PublishedLate) },
                new ReportKpi { Label = "Vencidos", Value = Num(model.Overdue) },
                new ReportKpi { Label = "A vencer", Value = Num(model.Upcoming) },
                new ReportKpi { Label = "Taxa no prazo", Value = Pct(model.OnTimeRate) }
            ];

            List<IReadOnlyList<string>> rows = model.ByCampaign
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

            return new ReportTable
            {
                Title = "Entregáveis: Prazo × Atraso",
                Subtitle = $"Período {Date(from)} a {Date(to)}",
                GeneratedAt = DateTimeOffset.UtcNow,
                Kpis = kpis,
                Columns = ["Campanha", "Total", "No prazo", "Atrasados", "Vencidos", "A vencer"],
                Rows = rows
            };
        }

        public static ReportTable BuildApprovalCycleTable(ApprovalCycleModel model, DateTimeOffset from, DateTimeOffset to)
        {
            List<ReportKpi> kpis =
            [
                new ReportKpi { Label = "Aprov. interna", Value = Days(model.AvgInternalApprovalDays) },
                new ReportKpi { Label = "Aprov. marca", Value = Days(model.AvgBrandApprovalDays) },
                new ReportKpi { Label = "Rodadas médias", Value = NumOrDash(model.AvgRounds) },
                new ReportKpi { Label = "Aprovado 1ª rodada", Value = RateOrDash(model.FirstRoundApprovalRate) }
            ];

            List<IReadOnlyList<string>> rows =
            [
                ["Aprovações internas", Num(model.InternalApprovedCount)],
                ["Tempo médio interna", Days(model.AvgInternalApprovalDays)],
                ["Aprovações da marca", Num(model.BrandApprovedCount)],
                ["Tempo médio marca", Days(model.AvgBrandApprovalDays)],
                ["Conteúdos aprovados", Num(model.ContentApprovedCount)],
                ["Rodadas médias", NumOrDash(model.AvgRounds)],
                ["Aprovado na 1ª rodada", RateOrDash(model.FirstRoundApprovalRate)]
            ];

            return new ReportTable
            {
                Title = "Aprovação e Rodadas",
                Subtitle = $"Período {Date(from)} a {Date(to)}",
                GeneratedAt = DateTimeOffset.UtcNow,
                Kpis = kpis,
                Columns = ["Métrica", "Valor"],
                Rows = rows
            };
        }

        public static ReportTable BuildContentLicensesTable(ContentLicenseReportModel model)
        {
            List<ReportKpi> kpis =
            [
                new ReportKpi { Label = "Ativas", Value = Num(model.ActiveCount) },
                new ReportKpi { Label = "Expirando", Value = Num(model.ExpiringSoonCount) },
                new ReportKpi { Label = "Expiradas", Value = Num(model.ExpiredCount) }
            ];

            List<IReadOnlyList<string>> rows = model.Lines
                .Select(line => (IReadOnlyList<string>)
                [
                    line.DeliverableTitle,
                    line.CampaignName ?? "-",
                    LicenseTypeLabel(line.Type),
                    line.Channels ?? "-",
                    line.ExpiresAt.HasValue ? Date(line.ExpiresAt) : "-",
                    line.DaysUntilExpiry?.ToString() ?? "-",
                    LicenseStatusLabel(line.Status)
                ])
                .ToList();

            return new ReportTable
            {
                Title = "Licenças de Conteúdo",
                Subtitle = $"Vencendo em até {model.ExpiringSoonDays} dias",
                GeneratedAt = DateTimeOffset.UtcNow,
                Kpis = kpis,
                Columns = ["Entregável", "Campanha", "Tipo", "Canais", "Expira", "Dias", "Status"],
                Rows = rows
            };
        }
    }
}
