using AgencyCampaign.Application.Services;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace AgencyCampaign.Api.Controllers
{
    [AccessArea("campaigns.area")]
    public sealed class CampaignReportsController : ApiControllerBase
    {
        private readonly ICampaignReportService service;

        public CampaignReportsController(ICampaignReportService service)
        {
            this.service = service;
        }

        [RequireAccess("campaigns.getById.description")]
        [PostEndpoint("campaign/{campaignId:long}")]
        public async Task<IActionResult> CreateOrGetLink(long campaignId, CancellationToken cancellationToken)
        {
            return Http200(await service.CreateOrGetLink(campaignId, cancellationToken));
        }

        [RequireAccess("campaigns.update.description")]
        [PostEndpoint("campaign/{campaignId:long}/revoke")]
        public async Task<IActionResult> RevokeLink(long campaignId, CancellationToken cancellationToken)
        {
            return Http200(await service.RevokeLink(campaignId, cancellationToken));
        }
    }
}
