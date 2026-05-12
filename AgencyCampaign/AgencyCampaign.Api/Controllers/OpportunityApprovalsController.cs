using AgencyCampaign.Api.Contracts.Opportunities;
using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.Opportunities;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Api.Controllers
{
    public sealed class OpportunityApprovalsController : ApiControllerBase
    {
        private readonly IOpportunityApprovalRequestService approvalRequestService;
        private new readonly IStringLocalizer<AgencyCampaignResource> Localizer;

        public OpportunityApprovalsController(IOpportunityApprovalRequestService approvalRequestService, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.approvalRequestService = approvalRequestService;
            Localizer = localizer;
        }

        [RequireAccess("Permite listar todas as aprovações comerciais.")]
        [GetEndpoint("[action]")]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken)
        {
            var result = await approvalRequestService.GetAllApprovals(request, cancellationToken);
            return Http200(new PagedResult<OpportunityApprovalRequestContract>
            {
                Items = result.Items.Select(OpportunityContractExtensions.MapApprovalWithDetails).ToArray(),
                Pagination = result.Pagination
            });
        }

        [RequireAccess("Permite consultar o resumo de aprovações comerciais.")]
        [GetEndpoint("[action]")]
        public async Task<IActionResult> Summary(CancellationToken cancellationToken)
        {
            return Http200(await approvalRequestService.GetApprovalsSummary(cancellationToken));
        }

        [RequireAccess("Permite listar as aprovações de uma negociação comercial.")]
        [GetEndpoint("negotiation/{opportunityNegotiationId:long}")]
        public async Task<IActionResult> GetByNegotiation(long opportunityNegotiationId, CancellationToken cancellationToken)
        {
            IReadOnlyCollection<OpportunityApprovalRequest> approvals = await approvalRequestService.GetApprovalsByNegotiationId(opportunityNegotiationId, cancellationToken);
            return Http200(approvals.Select(OpportunityContractExtensions.MapApprovalRequest).ToList());
        }

        [RequireAccess("Permite solicitar uma aprovação comercial.")]
        [PostEndpoint("[action]")]
        public async Task<IActionResult> Create([FromBody] CreateOpportunityApprovalRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            OpportunityApprovalRequest approval = await approvalRequestService.CreateOpportunityApprovalRequest(request, cancellationToken);
            return Http201(OpportunityContractExtensions.MapApprovalRequest(approval), Localizer["record.created"]);
        }

        [RequireAccess("Permite aprovar uma solicitação comercial.")]
        [HttpPost("{id:long}/Approve")]
        public async Task<IActionResult> Approve(long id, [FromBody] DecideOpportunityApprovalRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            OpportunityApprovalRequest approval = await approvalRequestService.Approve(id, request, cancellationToken);
            return Http200(OpportunityContractExtensions.MapApprovalRequest(approval), Localizer["record.updated"]);
        }

        [RequireAccess("Permite rejeitar uma solicitação comercial.")]
        [HttpPost("{id:long}/Reject")]
        public async Task<IActionResult> Reject(long id, [FromBody] DecideOpportunityApprovalRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            OpportunityApprovalRequest approval = await approvalRequestService.Reject(id, request, cancellationToken);
            return Http200(OpportunityContractExtensions.MapApprovalRequest(approval), Localizer["record.updated"]);
        }
    }

    internal static class OpportunityContractExtensions
    {
        public static OpportunityApprovalRequestContract MapApprovalRequest(OpportunityApprovalRequest approval)
        {
            return new OpportunityApprovalRequestContract
            {
                Id = approval.Id,
                OpportunityNegotiationId = approval.OpportunityNegotiationId,
                ApprovalType = approval.ApprovalType,
                Status = approval.Status,
                Reason = approval.Reason,
                RequestedByUserId = approval.RequestedByUserId,
                RequestedByUserName = approval.RequestedByUserName,
                ApprovedByUserId = approval.ApprovedByUserId,
                ApprovedByUserName = approval.ApprovedByUserName,
                RequestedAt = approval.RequestedAt,
                DecidedAt = approval.DecidedAt,
                DecisionNotes = approval.DecisionNotes,
                CreatedAt = approval.CreatedAt,
                UpdatedAt = approval.UpdatedAt
            };
        }

        public static OpportunityApprovalRequestContract MapApprovalWithDetails(OpportunityApprovalRequest approval)
        {
            return new OpportunityApprovalRequestContract
            {
                Id = approval.Id,
                OpportunityNegotiationId = approval.OpportunityNegotiationId,
                ApprovalType = approval.ApprovalType,
                Status = approval.Status,
                Reason = approval.Reason,
                RequestedByUserId = approval.RequestedByUserId,
                RequestedByUserName = approval.RequestedByUserName,
                ApprovedByUserId = approval.ApprovedByUserId,
                ApprovedByUserName = approval.ApprovedByUserName,
                RequestedAt = approval.RequestedAt,
                DecidedAt = approval.DecidedAt,
                DecisionNotes = approval.DecisionNotes,
                CreatedAt = approval.CreatedAt,
                UpdatedAt = approval.UpdatedAt,
                OpportunityId = approval.OpportunityNegotiation?.OpportunityId,
                OpportunityName = approval.OpportunityNegotiation?.Opportunity?.Name,
                NegotiationTitle = approval.OpportunityNegotiation?.Title,
            };
        }
    }
}
