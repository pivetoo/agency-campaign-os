using AgencyCampaign.Application.Services;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace AgencyCampaign.Api.Controllers
{
    [AccessArea("creators.area")]
    public sealed class CreatorAudienceController : ApiControllerBase
    {
        private readonly ICreatorAudienceSyncService syncService;

        public CreatorAudienceController(ICreatorAudienceSyncService syncService)
        {
            this.syncService = syncService;
        }

        [RequireAccess("creators.getById.description")]
        [PostEndpoint("creator/{creatorId:long}")]
        public async Task<IActionResult> SyncCreator(long creatorId, CancellationToken cancellationToken)
        {
            int synced = await syncService.SyncCreator(creatorId, cancellationToken);
            return Http200(new { synced });
        }
    }
}
