using AgencyCampaign.Application.Services;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace AgencyCampaign.Api.Controllers
{
    [AccessArea("financialEntries.area")]
    public sealed class FinancialMonitorController : ApiControllerBase
    {
        private readonly IFinancialMonitorService financialMonitorService;

        public FinancialMonitorController(IFinancialMonitorService financialMonitorService)
        {
            this.financialMonitorService = financialMonitorService;
        }

        [RequireAccess("financialEntries.getSummary.description")]
        [GetEndpoint]
        public async Task<IActionResult> Get(CancellationToken cancellationToken)
        {
            return Http200(await financialMonitorService.GetMonitor(cancellationToken));
        }
    }
}
