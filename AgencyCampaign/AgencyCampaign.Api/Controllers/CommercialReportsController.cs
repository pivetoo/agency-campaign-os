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
    }
}
