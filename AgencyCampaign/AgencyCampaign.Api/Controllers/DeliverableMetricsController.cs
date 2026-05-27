using AgencyCampaign.Application.Services;
using AgencyCampaign.Infrastructure.Options;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AgencyCampaign.Api.Controllers
{
    [AccessArea("campaigns.area")]
    public sealed class DeliverableMetricsController : ApiControllerBase
    {
        private readonly IDeliverableMetricsSyncService syncService;
        private readonly ApifyOptions options;

        public DeliverableMetricsController(IDeliverableMetricsSyncService syncService, IOptions<ApifyOptions> options)
        {
            this.syncService = syncService;
            this.options = options.Value;
        }

        [RequireAccess("campaigns.getById.description")]
        [PostEndpoint("campaign/{campaignId:long}")]
        public async Task<IActionResult> SyncCampaign(long campaignId, CancellationToken cancellationToken)
        {
            int synced = await syncService.SyncCampaign(campaignId, TimeSpan.FromMinutes(options.ButtonCooldownMinutes), cancellationToken);
            return Http200(new { synced });
        }
    }
}
