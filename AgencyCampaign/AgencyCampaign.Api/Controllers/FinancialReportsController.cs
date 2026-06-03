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

        public FinancialReportsController(IFinancialReportService service)
        {
            this.service = service;
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
    }
}
