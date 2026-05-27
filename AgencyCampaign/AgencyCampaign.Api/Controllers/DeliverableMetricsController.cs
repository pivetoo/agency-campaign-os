using AgencyCampaign.Application.Services;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace AgencyCampaign.Api.Controllers
{
    [AccessArea("campaigns.area")]
    public sealed class DeliverableMetricsController : ApiControllerBase
    {
        private readonly IDeliverableMetricsSyncService syncService;

        public DeliverableMetricsController(IDeliverableMetricsSyncService syncService)
        {
            this.syncService = syncService;
        }

        [RequireAccess("campaigns.getById.description")]
        [PostEndpoint("campaign/{campaignId:long}")]
        public async Task<IActionResult> SyncCampaign(long campaignId, CancellationToken cancellationToken)
        {
            int synced = await syncService.SyncCampaign(campaignId, cancellationToken);
            return Http200(new { synced });
        }
    }
}
