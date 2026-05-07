using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.DeliverableShareLinks;
using AgencyCampaign.Application.Services;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Api.Controllers
{
    public sealed class DeliverableShareLinksController : ApiControllerBase
    {
        private readonly IDeliverableShareLinkService service;
        private new readonly IStringLocalizer<AgencyCampaignResource> Localizer;

        public DeliverableShareLinksController(IDeliverableShareLinkService service, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.service = service;
            Localizer = localizer;
        }

        [RequireAccess("Permite listar os share links de uma entrega.")]
        [GetEndpoint("deliverable/{deliverableId:long}")]
        public async Task<IActionResult> GetByDeliverable(long deliverableId, CancellationToken cancellationToken)
        {
            return Http200(await service.GetByDeliverable(deliverableId, cancellationToken));
        }

        [RequireAccess("Permite gerar um share link para aprovação da marca.")]
        [PostEndpoint("[action]")]
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

        [RequireAccess("Permite revogar um share link de entrega.")]
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

        [RequireAccess("Permite listar as entregas com aprovação pendente da marca.")]
        [GetEndpoint("pending")]
        public async Task<IActionResult> GetPending(CancellationToken cancellationToken)
        {
            return Http200(await service.GetPending(cancellationToken));
        }
    }
}
