using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.Proposals;
using AgencyCampaign.Application.Services;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Api.Controllers
{
    [AccessArea("rateCardItems.area")]
    public sealed class RateCardItemsController : ApiControllerBase
    {
        private readonly IRateCardItemService service;
        private new readonly IStringLocalizer<AgencyCampaignResource> Localizer;

        public RateCardItemsController(IRateCardItemService service, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.service = service;
            Localizer = localizer;
        }

        [RequireAccess("rateCardItems.getByCreator.description")]
        [GetEndpoint("creator/{creatorId:long}")]
        public async Task<IActionResult> GetByCreator(long creatorId, [FromQuery] bool includeInactive, CancellationToken cancellationToken)
        {
            return Http200(await service.GetByCreator(creatorId, includeInactive, cancellationToken));
        }

        [RequireAccess("rateCardItems.create.description")]
        [PostEndpoint]
        public async Task<IActionResult> Create([FromBody] CreateRateCardItemRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            var result = await service.Create(request, cancellationToken);
            return Http201(result, Localizer["record.created"]);
        }

        [RequireAccess("rateCardItems.update.description")]
        [PutEndpoint("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateRateCardItemRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            var result = await service.Update(id, request, cancellationToken);
            return Http200(result, Localizer["record.updated"]);
        }

        [RequireAccess("rateCardItems.delete.description")]
        [DeleteEndpoint("{id:long}")]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            await service.Delete(id, cancellationToken);
            return Http204();
        }
    }
}
