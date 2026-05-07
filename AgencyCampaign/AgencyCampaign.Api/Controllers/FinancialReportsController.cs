using AgencyCampaign.Application.Models.Financial;
using AgencyCampaign.Application.Services;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace AgencyCampaign.Api.Controllers
{
    public sealed class FinancialReportsController : ApiControllerBase
    {
        private readonly IFinancialReportService service;

        public FinancialReportsController(IFinancialReportService service)
        {
            this.service = service;
        }

        [RequireAccess("Permite consultar o fluxo de caixa por período e granularidade.")]
        [GetEndpoint("cashflow")]
        public async Task<IActionResult> GetCashFlow([FromQuery] DateTimeOffset from, [FromQuery] DateTimeOffset to, [FromQuery] int granularity, CancellationToken cancellationToken)
        {
            CashFlowGranularity granularityValue = (CashFlowGranularity)granularity;
            var result = await service.GetCashFlow(from, to, granularityValue, cancellationToken);
            return Http200(result);
        }

        [RequireAccess("Permite consultar o relatório de aging financeiro.")]
        [GetEndpoint("aging")]
        public async Task<IActionResult> GetAging(CancellationToken cancellationToken)
        {
            var result = await service.GetAgingReport(cancellationToken);
            return Http200(result);
        }
    }
}
