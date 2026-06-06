using System.Globalization;
using System.Text;
using AgencyCampaign.Application.Export;
using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Application.Models.Reports;
using AgencyCampaign.Application.Services;

namespace AgencyCampaign.Infrastructure.Services
{
    // Exportacao CSV e PDF dos relatorios comerciais. Reaproveita ICommercialReportService (mesma
    // agregacao da tela, nunca diverge por construcao). CSV: UTF-8 COM BOM e decimal/virgula pt-BR
    // para abrir direto no Excel pt-BR. Sempre emite ao menos o cabecalho (bytes nunca vazios).
    // PDF: delega a IReportPdfService apos montar um ReportTable via BuildXxxTable (puro, testavel sem Chrome).
    public sealed class CommercialReportExportService : ICommercialReportExportService
    {
        private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");
        private static readonly UTF8Encoding Utf8WithBom = new(encoderShouldEmitUTF8Identifier: true);

        private readonly ICommercialReportService reportService;
        private readonly IReportPdfService pdfService;
        private readonly IOpportunityService opportunityService;
        private readonly ICommercialGoalService goalService;

        public CommercialReportExportService(
            ICommercialReportService reportService,
            IReportPdfService pdfService,
            IOpportunityService opportunityService,
            ICommercialGoalService goalService)
        {
            this.reportService = reportService;
            this.pdfService = pdfService;
            this.opportunityService = opportunityService;
            this.goalService = goalService;
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

        // PDF exports

        public async Task<byte[]> ExportFunilPdf(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
        {
            CommercialAnalyticsModel analytics = await opportunityService.GetAnalytics(from, to, false, null, cancellationToken);
            ReportTable table = BuildFunilTable(analytics, from, to);
            return await pdfService.GenerateAsync(table, cancellationToken);
        }

        public async Task<byte[]> ExportGanhosPerdasPdf(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
        {
            CommercialAnalyticsModel analytics = await opportunityService.GetAnalytics(from, to, false, null, cancellationToken);
            ReportTable table = BuildGanhosPerdasTable(analytics, from, to);
            return await pdfService.GenerateAsync(table, cancellationToken);
        }

        public async Task<byte[]> ExportForecastPdf(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
        {
            CommercialForecastModel forecast = await opportunityService.GetForecast(from, to, false, null, cancellationToken);
            ReportTable table = BuildForecastTable(forecast, from, to);
            return await pdfService.GenerateAsync(table, cancellationToken);
        }

        public async Task<byte[]> ExportMetasPdf(DateTimeOffset referenceDate, CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<CommercialGoalProgressModel> progress = await goalService.GetProgress(referenceDate, null, null, cancellationToken);
            ReportTable table = BuildMetasTable(progress, referenceDate);
            return await pdfService.GenerateAsync(table, cancellationToken);
        }

        public async Task<byte[]> ExportProposalsFunnelPdf(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
        {
            ProposalsFunnelModel funnel = await reportService.GetProposalsFunnel(from, to, cancellationToken);
            ReportTable table = BuildProposalsFunnelTable(funnel, from, to);
            return await pdfService.GenerateAsync(table, cancellationToken);
        }

        public async Task<byte[]> ExportBrandRankingPdf(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
        {
            BrandRankingModel ranking = await reportService.GetBrandRanking(from, to, cancellationToken);
            ReportTable table = BuildBrandRankingTable(ranking, from, to);
            return await pdfService.GenerateAsync(table, cancellationToken);
        }

        // Builders puros (sem I/O) — testáveis sem Chrome

        public static ReportTable BuildFunilTable(CommercialAnalyticsModel model, DateTimeOffset from, DateTimeOffset to)
        {
            int totalStuck = model.ConversionByStage.Sum(s => s.Stuck);

            List<ReportKpi> kpis =
            [
                new ReportKpi { Label = "Fechados", Value = Num(model.ClosedCount) },
                new ReportKpi { Label = "Win rate", Value = Pct(model.WinRate) },
                new ReportKpi { Label = "Ciclo médio", Value = Days(model.AverageCycleDays) },
                new ReportKpi { Label = "Em andamento", Value = Num(totalStuck) }
            ];

            List<IReadOnlyList<string>> rows = model.ConversionByStage
                .Select(s => (IReadOnlyList<string>)
                [
                    s.StageName,
                    Num(s.Entered),
                    Num(s.Advanced),
                    Num(s.Stuck),
                    Num(s.Lost),
                    Pct(s.ConversionRate)
                ])
                .ToList();

            return new ReportTable
            {
                Title = "Funil de Conversão",
                Subtitle = $"Período {Date(from)} a {Date(to)}",
                GeneratedAt = DateTimeOffset.UtcNow,
                Kpis = kpis,
                Columns = ["Estágio", "Entraram", "Avançaram", "Parados", "Perdidos", "Conversão"],
                Rows = rows
            };
        }

        public static ReportTable BuildGanhosPerdasTable(CommercialAnalyticsModel model, DateTimeOffset from, DateTimeOffset to)
        {
            int totalWins = model.WinReasons.Sum(r => r.Count);
            decimal totalWonValue = model.WinReasons.Sum(r => r.TotalValue);
            int totalLosses = model.LossReasons.Sum(r => r.Count);
            decimal totalLostValue = model.LossReasons.Sum(r => r.TotalValue);

            List<ReportKpi> kpis =
            [
                new ReportKpi { Label = "Ganhos", Value = Num(totalWins) },
                new ReportKpi { Label = "Valor ganho", Value = Brl(totalWonValue) },
                new ReportKpi { Label = "Perdas", Value = Num(totalLosses) },
                new ReportKpi { Label = "Valor perdido", Value = Brl(totalLostValue) }
            ];

            List<IReadOnlyList<string>> rows = [];

            foreach (ReasonAggregateModel r in model.WinReasons)
            {
                rows.Add(["Ganho", r.ReasonName, Num(r.Count), Brl(r.TotalValue)]);
            }

            foreach (ReasonAggregateModel r in model.LossReasons)
            {
                rows.Add(["Perda", r.ReasonName, Num(r.Count), Brl(r.TotalValue)]);
            }

            return new ReportTable
            {
                Title = "Ganhos × Perdas",
                Subtitle = $"Período {Date(from)} a {Date(to)}",
                GeneratedAt = DateTimeOffset.UtcNow,
                Kpis = kpis,
                Columns = ["Tipo", "Motivo", "Quantidade", "Valor"],
                Rows = rows
            };
        }

        public static ReportTable BuildForecastTable(CommercialForecastModel model, DateTimeOffset from, DateTimeOffset to)
        {
            List<ReportKpi> kpis =
            [
                new ReportKpi { Label = "Ponderado", Value = Brl(model.WeightedTotal) },
                new ReportKpi { Label = "Bruto", Value = Brl(model.UnweightedTotal) },
                new ReportKpi { Label = "Ganho", Value = Brl(model.WonTotal) },
                new ReportKpi { Label = "Em aberto", Value = Num(model.OpenCount) }
            ];

            List<IReadOnlyList<string>> rows = model.ByStage
                .Select(s => (IReadOnlyList<string>)
                [
                    s.StageName,
                    Num(s.Count),
                    Brl(s.TotalValue),
                    Brl(s.WeightedValue),
                    Pct(s.AverageProbability)
                ])
                .ToList();

            return new ReportTable
            {
                Title = "Previsão (Forecast)",
                Subtitle = $"Período {Date(from)} a {Date(to)}",
                GeneratedAt = DateTimeOffset.UtcNow,
                Kpis = kpis,
                Columns = ["Estágio", "Qtd", "Valor", "Ponderado", "Prob. média"],
                Rows = rows
            };
        }

        public static ReportTable BuildMetasTable(IReadOnlyCollection<CommercialGoalProgressModel> progress, DateTimeOffset referenceDate)
        {
            List<ReportKpi> kpis =
            [
                new ReportKpi { Label = "Metas", Value = Num(progress.Count) },
                new ReportKpi { Label = "Meta total", Value = Brl(progress.Sum(p => p.TargetAmount)) },
                new ReportKpi { Label = "Realizado total", Value = Brl(progress.Sum(p => p.AchievedAmount)) }
            ];

            List<IReadOnlyList<string>> rows = progress
                .Select(p => (IReadOnlyList<string>)
                [
                    p.UserName ?? "Agência",
                    PeriodLabel(p.PeriodType),
                    Brl(p.TargetAmount),
                    Brl(p.AchievedAmount),
                    Num(p.AchievedDealsCount),
                    Pct(p.PercentAchieved)
                ])
                .ToList();

            return new ReportTable
            {
                Title = "Metas × Realizado",
                Subtitle = $"Referência {Date(referenceDate)}",
                GeneratedAt = DateTimeOffset.UtcNow,
                Kpis = kpis,
                Columns = ["Responsável", "Período", "Meta", "Realizado", "Negócios", "Atingido"],
                Rows = rows
            };
        }

        public static ReportTable BuildProposalsFunnelTable(ProposalsFunnelModel model, DateTimeOffset from, DateTimeOffset to)
        {
            List<ReportKpi> kpis =
            [
                new ReportKpi { Label = "Emitidas", Value = Num(model.EmittedCount) },
                new ReportKpi { Label = "Aceitas", Value = Num(model.AcceptedCount) },
                new ReportKpi { Label = "Taxa de aceite", Value = Pct(model.AcceptanceRate) }
            ];

            List<IReadOnlyList<string>> rows =
            [
                ["Emitidas", Num(model.EmittedCount), Brl(model.EmittedValue)],
                ["Aceitas", Num(model.AcceptedCount), Brl(model.AcceptedValue)],
                ["Rejeitadas", Num(model.RejectedCount), "-"],
                ["Taxa de aceite", Pct(model.AcceptanceRate), "-"]
            ];

            return new ReportTable
            {
                Title = "Propostas: Emitidas × Aceitas",
                Subtitle = $"Período {Date(from)} a {Date(to)}",
                GeneratedAt = DateTimeOffset.UtcNow,
                Kpis = kpis,
                Columns = ["Métrica", "Quantidade", "Valor"],
                Rows = rows
            };
        }

        public static ReportTable BuildBrandRankingTable(BrandRankingModel model, DateTimeOffset from, DateTimeOffset to)
        {
            List<ReportKpi> kpis =
            [
                new ReportKpi { Label = "Marcas", Value = Num(model.Lines.Count) }
            ];

            List<IReadOnlyList<string>> rows = model.Lines
                .Select(line => (IReadOnlyList<string>)
                [
                    line.BrandName,
                    Num(line.WonCount),
                    Num(line.LostCount),
                    Brl(line.WonValue),
                    Pct(line.WinRate)
                ])
                .ToList();

            return new ReportTable
            {
                Title = "Ranking por Marca",
                Subtitle = $"Período {Date(from)} a {Date(to)}",
                GeneratedAt = DateTimeOffset.UtcNow,
                Kpis = kpis,
                Columns = ["Marca", "Ganhos", "Perdas", "Valor ganho", "Win rate"],
                Rows = rows
            };
        }

        private static string Money(decimal value)
        {
            return value.ToString("0.00", PtBr);
        }

        private static string Brl(decimal v) => v.ToString("C", PtBr);

        private static string Pct(decimal v) => v.ToString("0.00", PtBr) + "%";

        private static string Num(int v) => v.ToString("#,##0", PtBr);

        private static string Days(decimal v) => v.ToString("0.0", PtBr) + " dias";

        private static string Date(DateTimeOffset value) => value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

        private static string PeriodLabel(int periodType)
        {
            return periodType switch
            {
                1 => "Mensal",
                2 => "Trimestral",
                3 => "Anual",
                _ => "-"
            };
        }

        private static byte[] Bytes(string csv)
        {
            return Utf8WithBom.GetPreamble().Concat(Utf8WithBom.GetBytes(csv)).ToArray();
        }
    }
}
