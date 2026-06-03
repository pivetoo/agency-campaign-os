using AgencyCampaign.Application.Models.Financial;
using AgencyCampaign.Application.Services;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace AgencyCampaign.Api.Controllers
{
    [AccessArea("financialReports.area")]
    public sealed class FinancialReportsController : ApiControllerBase
    {
        private readonly IFinancialReportService service;
        private readonly IFinancialReportExportService exportService;

        public FinancialReportsController(IFinancialReportService service, IFinancialReportExportService exportService)
        {
            this.service = service;
            this.exportService = exportService;
        }

        [RequireAccess("financialReports.getCashFlow.description")]
        [GetEndpoint("cashflow")]
        public async Task<IActionResult> GetCashFlow([FromQuery] DateTimeOffset from, [FromQuery] DateTimeOffset to, [FromQuery] int granularity, CancellationToken cancellationToken)
        {
            CashFlowGranularity granularityValue = (CashFlowGranularity)granularity;
            var result = await service.GetCashFlow(from, to, granularityValue, cancellationToken);
            return Http200(result);
        }

        [RequireAccess("financialReports.getAging.description")]
        [GetEndpoint("aging")]
        public async Task<IActionResult> GetAging(CancellationToken cancellationToken)
        {
            var result = await service.GetAgingReport(cancellationToken);
            return Http200(result);
        }

        [RequireAccess("financialReports.getTaxWithholding.description")]
        [GetEndpoint("tax-withholding")]
        public async Task<IActionResult> GetTaxWithholding([FromQuery] DateTimeOffset from, [FromQuery] DateTimeOffset to, CancellationToken cancellationToken)
        {
            var result = await service.GetTaxWithholdingReport(from, to, cancellationToken);
            return Http200(result);
        }

        [RequireAccess("financialReports.getCampaignProfitability.description")]
        [GetEndpoint("campaign-profitability")]
        public async Task<IActionResult> GetCampaignProfitability(CancellationToken cancellationToken)
        {
            var result = await service.GetCampaignProfitability(cancellationToken);
            return Http200(result);
        }

        [RequireAccess("financialReports.getAccrualResult.description")]
        [GetEndpoint("accrual-result")]
        public async Task<IActionResult> GetAccrualResult([FromQuery] DateTimeOffset from, [FromQuery] DateTimeOffset to, CancellationToken cancellationToken)
        {
            var result = await service.GetAccrualResult(from, to, cancellationToken);
            return Http200(result);
        }

        [RequireAccess("financialReports.getCashFlowProjection.description")]
        [GetEndpoint("cashflow-projection")]
        public async Task<IActionResult> GetCashFlowProjection([FromQuery] int weeks, CancellationToken cancellationToken)
        {
            int horizon = weeks <= 0 ? 12 : weeks;
            var result = await service.GetCashFlowProjection(horizon, cancellationToken);
            return Http200(result);
        }

        [RequireAccess("financialReports.getCashFlow.description")]
        [GetEndpoint("cashflow/export")]
        public async Task<IActionResult> ExportCashFlow([FromQuery] DateTimeOffset from, [FromQuery] DateTimeOffset to, [FromQuery] int granularity, CancellationToken cancellationToken)
        {
            byte[] csv = await exportService.ExportCashFlow(from, to, (CashFlowGranularity)granularity, cancellationToken);
            return SendCsv(csv, "fluxo-de-caixa.csv");
        }

        [RequireAccess("financialReports.getAging.description")]
        [GetEndpoint("aging/export")]
        public async Task<IActionResult> ExportAging(CancellationToken cancellationToken)
        {
            byte[] csv = await exportService.ExportAging(cancellationToken);
            return SendCsv(csv, "aging.csv");
        }

        [RequireAccess("financialReports.getTaxWithholding.description")]
        [GetEndpoint("tax-withholding/export")]
        public async Task<IActionResult> ExportTaxWithholding([FromQuery] DateTimeOffset from, [FromQuery] DateTimeOffset to, CancellationToken cancellationToken)
        {
            byte[] csv = await exportService.ExportTaxWithholding(from, to, cancellationToken);
            return SendCsv(csv, "retencoes.csv");
        }

        [RequireAccess("financialReports.getCampaignProfitability.description")]
        [GetEndpoint("campaign-profitability/export")]
        public async Task<IActionResult> ExportCampaignProfitability(CancellationToken cancellationToken)
        {
            byte[] csv = await exportService.ExportCampaignProfitability(cancellationToken);
            return SendCsv(csv, "rentabilidade-campanhas.csv");
        }

        [RequireAccess("financialReports.getAccrualResult.description")]
        [GetEndpoint("accrual-result/export")]
        public async Task<IActionResult> ExportAccrualResult([FromQuery] DateTimeOffset from, [FromQuery] DateTimeOffset to, CancellationToken cancellationToken)
        {
            byte[] csv = await exportService.ExportAccrualResult(from, to, cancellationToken);
            return SendCsv(csv, "resultado-competencia.csv");
        }
    }
}
