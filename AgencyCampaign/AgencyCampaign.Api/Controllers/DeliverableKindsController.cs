using AgencyCampaign.Api.Contracts.DeliverableKinds;
using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.DeliverableKinds;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Api.Controllers
{
    [AccessArea("deliverableKinds.area")]
    public sealed class DeliverableKindsController : ApiControllerBase
    {
        private readonly IDeliverableKindService deliverableKindService;
        private new readonly IStringLocalizer<AgencyCampaignResource> Localizer;
        private static readonly Func<DeliverableKind, DeliverableKindContract> MapDeliverableKind = DeliverableKindContract.Projection.Compile();

        public DeliverableKindsController(IDeliverableKindService deliverableKindService, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.deliverableKindService = deliverableKindService;
            Localizer = localizer;
        }

        [RequireAccess("deliverableKinds.get.description")]
        [GetEndpoint]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, [FromQuery] string? search, [FromQuery] bool includeInactive, CancellationToken cancellationToken)
        {
            PagedResult<DeliverableKind> result = await deliverableKindService.GetDeliverableKinds(request, search, includeInactive, cancellationToken);
            return Http200(new PagedResult<DeliverableKindContract>
            {
                Items = result.Items.Select(MapDeliverableKind).ToArray(),
                Pagination = result.Pagination
            });
        }

        [RequireAccess("deliverableKinds.getById.description")]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            DeliverableKind? deliverableKind = await deliverableKindService.GetDeliverableKindById(id, cancellationToken);
            return deliverableKind is null ? Http404(Localizer["record.notFound"]) : Http200(MapDeliverableKind(deliverableKind));
        }

        [RequireAccess("deliverableKinds.getActive.description")]
        [GetEndpoint("active")]
        public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
        {
            List<DeliverableKind> deliverableKinds = await deliverableKindService.GetActiveDeliverableKinds(cancellationToken);
            return Http200(deliverableKinds.Select(MapDeliverableKind).ToList());
        }

        [RequireAccess("deliverableKinds.create.description")]
        [PostEndpoint]
        public async Task<IActionResult> Create([FromBody] CreateDeliverableKindRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            DeliverableKind deliverableKind = await deliverableKindService.CreateDeliverableKind(request, cancellationToken);
            return Http201(MapDeliverableKind(deliverableKind), Localizer["record.created"]);
        }

        [RequireAccess("deliverableKinds.update.description")]
        [PutEndpoint("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateDeliverableKindRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            DeliverableKind deliverableKind = await deliverableKindService.UpdateDeliverableKind(id, request, cancellationToken);
            return Http200(MapDeliverableKind(deliverableKind), Localizer["record.updated"]);
        }
    }
}
