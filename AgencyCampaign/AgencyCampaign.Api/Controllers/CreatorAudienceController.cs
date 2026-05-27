using AgencyCampaign.Application.Services;
using AgencyCampaign.Infrastructure.Options;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AgencyCampaign.Api.Controllers
{
    [AccessArea("creators.area")]
    public sealed class CreatorAudienceController : ApiControllerBase
    {
        private readonly ICreatorAudienceSyncService syncService;
        private readonly ApifyOptions options;

        public CreatorAudienceController(ICreatorAudienceSyncService syncService, IOptions<ApifyOptions> options)
        {
            this.syncService = syncService;
            this.options = options.Value;
        }

        [RequireAccess("creators.getById.description")]
        [PostEndpoint("creator/{creatorId:long}")]
        public async Task<IActionResult> SyncCreator(long creatorId, CancellationToken cancellationToken)
        {
            int synced = await syncService.SyncCreator(creatorId, TimeSpan.FromMinutes(options.ButtonCooldownMinutes), cancellationToken);
            return Http200(new { synced });
        }
    }
}
