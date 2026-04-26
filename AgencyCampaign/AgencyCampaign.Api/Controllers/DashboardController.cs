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

        [RequireAccess("Permite consultar os dados dos gráficos do dashboard.")]
        [GetEndpoint("[action]")]
        public async Task<IActionResult> Charts(CancellationToken cancellationToken)
        {
            DashboardChartsModel result = await dashboardService.GetChartsData(cancellationToken);
            return Http200(result);
        }
    }
}
