using AgencyCampaign.Application.Services;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace AgencyCampaign.Api.Controllers
{
    [AccessArea("productionReports.area")]
    public sealed class ProductionReportsController : ApiControllerBase
    {
        private readonly IProductionReportService service;
        private readonly IProductionReportExportService exportService;

        public ProductionReportsController(IProductionReportService service, IProductionReportExportService exportService)
        {
            this.service = service;
            this.exportService = exportService;
        }

        [RequireAccess("productionReports.getCampaignPerformance.description")]
        [GetEndpoint("campaign-performance")]
        public async Task<IActionResult> GetCampaignPerformance([FromQuery] DateTimeOffset from, [FromQuery] DateTimeOffset to, CancellationToken cancellationToken)
        {
            var result = await service.GetCampaignPerformance(from, to, cancellationToken);
            return Http200(result);
        }

        [RequireAccess("productionReports.getCampaignPerformance.description")]
        [GetEndpoint("campaign-performance/export")]
        public async Task<IActionResult> ExportCampaignPerformance([FromQuery] DateTimeOffset from, [FromQuery] DateTimeOffset to, CancellationToken cancellationToken)
        {
            byte[] csv = await exportService.ExportCampaignPerformance(from, to, cancellationToken);
            return SendCsv(csv, "performance-campanhas.csv");
        }

        [RequireAccess("productionReports.getCreatorPerformance.description")]
        [GetEndpoint("creator-performance")]
        public async Task<IActionResult> GetCreatorPerformance([FromQuery] DateTimeOffset from, [FromQuery] DateTimeOffset to, CancellationToken cancellationToken)
        {
            var result = await service.GetCreatorPerformance(from, to, cancellationToken);
            return Http200(result);
        }

        [RequireAccess("productionReports.getCreatorPerformance.description")]
        [GetEndpoint("creator-performance/export")]
        public async Task<IActionResult> ExportCreatorPerformance([FromQuery] DateTimeOffset from, [FromQuery] DateTimeOffset to, CancellationToken cancellationToken)
        {
            byte[] csv = await exportService.ExportCreatorPerformance(from, to, cancellationToken);
            return SendCsv(csv, "performance-creators.csv");
        }

        [RequireAccess("productionReports.getPlatformProduction.description")]
        [GetEndpoint("platform-production")]
        public async Task<IActionResult> GetPlatformProduction([FromQuery] DateTimeOffset from, [FromQuery] DateTimeOffset to, CancellationToken cancellationToken)
        {
            var result = await service.GetPlatformProduction(from, to, cancellationToken);
            return Http200(result);
        }

        [RequireAccess("productionReports.getPlatformProduction.description")]
        [GetEndpoint("platform-production/export")]
        public async Task<IActionResult> ExportPlatformProduction([FromQuery] DateTimeOffset from, [FromQuery] DateTimeOffset to, CancellationToken cancellationToken)
        {
            byte[] csv = await exportService.ExportPlatformProduction(from, to, cancellationToken);
            return SendCsv(csv, "producao-plataforma.csv");
        }

        [RequireAccess("productionReports.getDeliverableSla.description")]
        [GetEndpoint("deliverable-sla")]
        public async Task<IActionResult> GetDeliverableSla([FromQuery] DateTimeOffset from, [FromQuery] DateTimeOffset to, CancellationToken cancellationToken)
        {
            var result = await service.GetDeliverableSla(from, to, cancellationToken);
            return Http200(result);
        }

        [RequireAccess("productionReports.getDeliverableSla.description")]
        [GetEndpoint("deliverable-sla/export")]
        public async Task<IActionResult> ExportDeliverableSla([FromQuery] DateTimeOffset from, [FromQuery] DateTimeOffset to, CancellationToken cancellationToken)
        {
            byte[] csv = await exportService.ExportDeliverableSla(from, to, cancellationToken);
            return SendCsv(csv, "sla-entregaveis.csv");
        }

        [RequireAccess("productionReports.getApprovalCycle.description")]
        [GetEndpoint("approval-cycle")]
        public async Task<IActionResult> GetApprovalCycle([FromQuery] DateTimeOffset from, [FromQuery] DateTimeOffset to, CancellationToken cancellationToken)
        {
            var result = await service.GetApprovalCycle(from, to, cancellationToken);
            return Http200(result);
        }

        [RequireAccess("productionReports.getApprovalCycle.description")]
        [GetEndpoint("approval-cycle/export")]
        public async Task<IActionResult> ExportApprovalCycle([FromQuery] DateTimeOffset from, [FromQuery] DateTimeOffset to, CancellationToken cancellationToken)
        {
            byte[] csv = await exportService.ExportApprovalCycle(from, to, cancellationToken);
            return SendCsv(csv, "ciclo-aprovacao.csv");
        }

        [RequireAccess("productionReports.getContentLicenses.description")]
        [GetEndpoint("content-licenses")]
        public async Task<IActionResult> GetContentLicenses([FromQuery] int expiringSoonDays, CancellationToken cancellationToken)
        {
            var result = await service.GetContentLicenses(expiringSoonDays, cancellationToken);
            return Http200(result);
        }

        [RequireAccess("productionReports.getContentLicenses.description")]
        [GetEndpoint("content-licenses/export")]
        public async Task<IActionResult> ExportContentLicenses([FromQuery] int expiringSoonDays, CancellationToken cancellationToken)
        {
            byte[] csv = await exportService.ExportContentLicenses(expiringSoonDays, cancellationToken);
            return SendCsv(csv, "licencas-conteudo.csv");
        }
    }
}
