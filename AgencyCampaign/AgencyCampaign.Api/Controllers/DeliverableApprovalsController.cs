using AgencyCampaign.Api.Contracts.DeliverableApprovals;
using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.DeliverableApprovals;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Api.Controllers
{
    public sealed class DeliverableApprovalsController : ApiControllerBase
    {
        private readonly IDeliverableApprovalService deliverableApprovalService;
        private new readonly IStringLocalizer<AgencyCampaignResource> Localizer;
        private static readonly Func<DeliverableApproval, DeliverableApprovalContract> MapApproval = DeliverableApprovalContract.Projection.Compile();

        public DeliverableApprovalsController(IDeliverableApprovalService deliverableApprovalService, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.deliverableApprovalService = deliverableApprovalService;
            Localizer = localizer;
        }

        [RequireAccess("Permite listar as aprovações de entregas.")]
        [GetEndpoint("[action]")]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken)
        {
            PagedResult<DeliverableApproval> result = await deliverableApprovalService.GetApprovals(request, cancellationToken);
            return Http200(new PagedResult<DeliverableApprovalContract>
            {
                Items = result.Items.Select(MapApproval).ToArray(),
                Pagination = result.Pagination
            });
        }

        [RequireAccess("Permite consultar os detalhes de uma aprovação de entrega.")]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            DeliverableApproval? approval = await deliverableApprovalService.GetApprovalById(id, cancellationToken);
            return approval is null ? Http404(Localizer["record.notFound"]) : Http200(MapApproval(approval));
        }

        [RequireAccess("Permite listar as aprovações de uma entrega específica.")]
        [GetEndpoint("deliverable/{campaignDeliverableId:long}")]
        public async Task<IActionResult> GetByDeliverable(long campaignDeliverableId, CancellationToken cancellationToken)
        {
            if (campaignDeliverableId <= 0)
            {
                return Http400(Localizer["request.id.required"]);
            }

            List<DeliverableApproval> approvals = await deliverableApprovalService.GetByDeliverable(campaignDeliverableId, cancellationToken);
            return Http200(approvals.Select(MapApproval).ToList());
        }

        [RequireAccess("Permite cadastrar uma aprovação para uma entrega.")]
        [PostEndpoint("[action]")]
        public async Task<IActionResult> Create([FromBody] CreateDeliverableApprovalRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            DeliverableApproval approval = await deliverableApprovalService.CreateApproval(request, cancellationToken);
            return Http201(MapApproval(approval), Localizer["record.created"]);
        }

        [RequireAccess("Permite atualizar uma aprovação de entrega.")]
        [PutEndpoint("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateDeliverableApprovalRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            DeliverableApproval approval = await deliverableApprovalService.UpdateApproval(id, request, cancellationToken);
            return Http200(MapApproval(approval), Localizer["record.updated"]);
        }
    }
}
