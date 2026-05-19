using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.DeliverableShareLinks;
using AgencyCampaign.Application.Services;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Api.Controllers
{
    [AccessArea("deliverableShareLinks.area")]
    public sealed class DeliverableShareLinksController : ApiControllerBase
    {
        private readonly IDeliverableShareLinkService service;
        private new readonly IStringLocalizer<AgencyCampaignResource> Localizer;

        public DeliverableShareLinksController(IDeliverableShareLinkService service, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.service = service;
            Localizer = localizer;
        }

        [RequireAccess("deliverableShareLinks.getByDeliverable.description")]
        [GetEndpoint("deliverable/{deliverableId:long}")]
        public async Task<IActionResult> GetByDeliverable(long deliverableId, CancellationToken cancellationToken)
        {
            return Http200(await service.GetByDeliverable(deliverableId, cancellationToken));
        }

        [RequireAccess("deliverableShareLinks.create.description")]
        [PostEndpoint]
        public async Task<IActionResult> Create([FromBody] CreateDeliverableShareLinkRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            var result = await service.Create(request, cancellationToken);
            return Http201(result, Localizer["record.created"]);
        }

        [RequireAccess("deliverableShareLinks.revoke.description")]
        [PostEndpoint("revoke/{id:long}")]
        public async Task<IActionResult> Revoke(long id, CancellationToken cancellationToken)
        {
            await service.Revoke(id, cancellationToken);
            return Http204();
        }
    }

    public sealed class DeliverablePendingApprovalsController : ApiControllerBase
    {
        private readonly IDeliverableApprovalsService service;

        public DeliverablePendingApprovalsController(IDeliverableApprovalsService service)
        {
            this.service = service;
        }

        [RequireAccess("deliverableShareLinks.getPending.description")]
        [GetEndpoint("pending")]
        public async Task<IActionResult> GetPending(CancellationToken cancellationToken)
        {
            return Http200(await service.GetPending(cancellationToken));
        }
    }
}
