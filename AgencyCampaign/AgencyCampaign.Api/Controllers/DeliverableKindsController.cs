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

        [RequireAccess("Permite listar os tipos de entrega cadastrados.")]
        [GetEndpoint("[action]")]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken)
        {
            PagedResult<DeliverableKind> result = await deliverableKindService.GetDeliverableKinds(request, cancellationToken);
            return Http200(new PagedResult<DeliverableKindContract>
            {
                Items = result.Items.Select(MapDeliverableKind).ToArray(),
                Pagination = result.Pagination
            });
        }

        [RequireAccess("Permite consultar os detalhes de um tipo de entrega.")]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            DeliverableKind? deliverableKind = await deliverableKindService.GetDeliverableKindById(id, cancellationToken);
            return deliverableKind is null ? Http404(Localizer["record.notFound"]) : Http200(MapDeliverableKind(deliverableKind));
        }

        [RequireAccess("Permite listar os tipos de entrega ativos.")]
        [GetEndpoint("active")]
        public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
        {
            List<DeliverableKind> deliverableKinds = await deliverableKindService.GetActiveDeliverableKinds(cancellationToken);
            return Http200(deliverableKinds.Select(MapDeliverableKind).ToList());
        }

        [RequireAccess("Permite cadastrar um novo tipo de entrega.")]
        [PostEndpoint("[action]")]
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

        [RequireAccess("Permite atualizar os dados de um tipo de entrega.")]
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
