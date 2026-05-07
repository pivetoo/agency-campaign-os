using AgencyCampaign.Application.Models.Dashboard;
using AgencyCampaign.Application.Services;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace AgencyCampaign.Api.Controllers
{
    public sealed class DashboardController : ApiControllerBase
    {
        private readonly IDashboardService dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            this.dashboardService = dashboardService;
        }

        [RequireAccess("Permite consultar a visão geral do dashboard da agência.")]
        [GetEndpoint("[action]")]
        public async Task<IActionResult> Overview(CancellationToken cancellationToken)
        {
            DashboardOverviewModel result = await dashboardService.GetOverview(cancellationToken);
            return Http200(result);
        }
    }
}
