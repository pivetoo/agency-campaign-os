using AgencyCampaign.Application.Models.Campaigns;
using AgencyCampaign.Application.Services;
using Archon.Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgencyCampaign.Api.Controllers
{
    [AllowAnonymous]
    [Route("api/campaign-report-public")]
    public sealed class CampaignReportPublicController : ApiControllerBase
    {
        private readonly ICampaignReportService service;

        public CampaignReportPublicController(ICampaignReportService service)
        {
            this.service = service;
        }

        [HttpGet("{token}")]
        public async Task<IActionResult> GetByToken(string token, CancellationToken cancellationToken)
        {
            CampaignReportModel? result = await service.GetReportByToken(token, cancellationToken);
            return result is null ? Http404("Link inválido ou expirado.") : Http200(result);
        }
    }
}
