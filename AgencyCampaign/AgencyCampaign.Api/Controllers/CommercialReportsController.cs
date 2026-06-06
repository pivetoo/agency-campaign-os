using AgencyCampaign.Application.Services;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace AgencyCampaign.Api.Controllers
{
    [AccessArea("commercialReports.area")]
    public sealed class CommercialReportsController : ApiControllerBase
    {
        private readonly ICommercialReportService service;
        private readonly ICommercialReportExportService exportService;

        public CommercialReportsController(ICommercialReportService service, ICommercialReportExportService exportService)
        {
            this.service = service;
            this.exportService = exportService;
        }

        [RequireAccess("commercialReports.getProposalsFunnel.description")]
        [GetEndpoint("proposals-funnel")]
        public async Task<IActionResult> GetProposalsFunnel([FromQuery] DateTimeOffset from, [FromQuery] DateTimeOffset to, CancellationToken cancellationToken)
        {
            var result = await service.GetProposalsFunnel(from, to, cancellationToken);
            return Http200(result);
        }

        [RequireAccess("commercialReports.getBrandRanking.description")]
        [GetEndpoint("brand-ranking")]
        public async Task<IActionResult> GetBrandRanking([FromQuery] DateTimeOffset from, [FromQuery] DateTimeOffset to, CancellationToken cancellationToken)
        {
            var result = await service.GetBrandRanking(from, to, cancellationToken);
            return Http200(result);
        }

        [RequireAccess("commercialReports.getProposalsFunnel.description")]
        [GetEndpoint("proposals-funnel/export")]
        public async Task<IActionResult> ExportProposalsFunnel([FromQuery] DateTimeOffset from, [FromQuery] DateTimeOffset to, CancellationToken cancellationToken)
        {
            byte[] csv = await exportService.ExportProposalsFunnel(from, to, cancellationToken);
            return SendCsv(csv, "propostas-funil.csv");
        }

        [RequireAccess("commercialReports.getBrandRanking.description")]
        [GetEndpoint("brand-ranking/export")]
        public async Task<IActionResult> ExportBrandRanking([FromQuery] DateTimeOffset from, [FromQuery] DateTimeOffset to, CancellationToken cancellationToken)
        {
            byte[] csv = await exportService.ExportBrandRanking(from, to, cancellationToken);
            return SendCsv(csv, "ranking-marcas.csv");
        }

        [RequireAccess("commercialReports.getFunil.description")]
        [GetEndpoint("funil/pdf")]
        public async Task<IActionResult> ExportFunilPdf([FromQuery] DateTimeOffset from, [FromQuery] DateTimeOffset to, CancellationToken cancellationToken)
        {
            byte[] bytes = await exportService.ExportFunilPdf(from, to, cancellationToken);
            return File(bytes, "application/pdf", "funil-conversao.pdf");
        }

        [RequireAccess("commercialReports.getGanhosPerdas.description")]
        [GetEndpoint("ganhos-perdas/pdf")]
        public async Task<IActionResult> ExportGanhosPerdasPdf([FromQuery] DateTimeOffset from, [FromQuery] DateTimeOffset to, CancellationToken cancellationToken)
        {
            byte[] bytes = await exportService.ExportGanhosPerdasPdf(from, to, cancellationToken);
            return File(bytes, "application/pdf", "ganhos-perdas.pdf");
        }

        [RequireAccess("commercialReports.getForecast.description")]
        [GetEndpoint("forecast/pdf")]
        public async Task<IActionResult> ExportForecastPdf([FromQuery] DateTimeOffset from, [FromQuery] DateTimeOffset to, CancellationToken cancellationToken)
        {
            byte[] bytes = await exportService.ExportForecastPdf(from, to, cancellationToken);
            return File(bytes, "application/pdf", "forecast.pdf");
        }

        [RequireAccess("commercialReports.getMetas.description")]
        [GetEndpoint("metas/pdf")]
        public async Task<IActionResult> ExportMetasPdf([FromQuery] DateTimeOffset referenceDate, CancellationToken cancellationToken)
        {
            byte[] bytes = await exportService.ExportMetasPdf(referenceDate, cancellationToken);
            return File(bytes, "application/pdf", "metas-realizado.pdf");
        }

        [RequireAccess("commercialReports.getProposalsFunnel.description")]
        [GetEndpoint("proposals-funnel/pdf")]
        public async Task<IActionResult> ExportProposalsFunnelPdf([FromQuery] DateTimeOffset from, [FromQuery] DateTimeOffset to, CancellationToken cancellationToken)
        {
            byte[] bytes = await exportService.ExportProposalsFunnelPdf(from, to, cancellationToken);
            return File(bytes, "application/pdf", "propostas-funil.pdf");
        }

        [RequireAccess("commercialReports.getBrandRanking.description")]
        [GetEndpoint("brand-ranking/pdf")]
        public async Task<IActionResult> ExportBrandRankingPdf([FromQuery] DateTimeOffset from, [FromQuery] DateTimeOffset to, CancellationToken cancellationToken)
        {
            byte[] bytes = await exportService.ExportBrandRankingPdf(from, to, cancellationToken);
            return File(bytes, "application/pdf", "ranking-marcas.pdf");
        }
    }
}
