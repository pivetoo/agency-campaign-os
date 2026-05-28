using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.CampaignBriefings;
using AgencyCampaign.Application.Services;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Api.Controllers
{
    [AccessArea("campaigns.area")]
    public sealed class CampaignBriefingController : ApiControllerBase
    {
        private readonly ICampaignBriefingService service;
        private new readonly IStringLocalizer<AgencyCampaignResource> Localizer;

        public CampaignBriefingController(ICampaignBriefingService service, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.service = service;
            Localizer = localizer;
        }

        [RequireAccess("campaigns.getById.description")]
        [GetEndpoint("campaign/{campaignId:long}")]
        public async Task<IActionResult> GetByCampaign(long campaignId, CancellationToken cancellationToken)
        {
            return Http200(await service.GetByCampaign(campaignId, cancellationToken));
        }

        [RequireAccess("campaigns.update.description")]
        [PutEndpoint("campaign/{campaignId:long}")]
        public async Task<IActionResult> Upsert(long campaignId, [FromBody] UpsertCampaignBriefingRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            CampaignBriefingModel result = await service.Upsert(campaignId, request, cancellationToken);
            return Http200(result, Localizer["record.updated"]);
        }
    }
}
