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
    [AccessArea("opportunityApprovals.area")]
    public sealed class OpportunityApprovalsController : ApiControllerBase
    {
        private readonly IOpportunityApprovalRequestService approvalRequestService;
        private readonly IOpportunityApprovalCommentService commentService;
        private readonly IOpportunityApprovalReviewerService reviewerService;
        private readonly IOpportunityApprovalDiffService diffService;
        private readonly IOpportunityApprovalImpactService impactService;
        private new readonly IStringLocalizer<AgencyCampaignResource> Localizer;

        public OpportunityApprovalsController(IOpportunityApprovalRequestService approvalRequestService, IOpportunityApprovalCommentService commentService, IOpportunityApprovalReviewerService reviewerService, IOpportunityApprovalDiffService diffService, IOpportunityApprovalImpactService impactService, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.approvalRequestService = approvalRequestService;
            this.commentService = commentService;
            this.reviewerService = reviewerService;
            this.diffService = diffService;
            this.impactService = impactService;
            Localizer = localizer;
        }

        [RequireAccess("opportunityApprovals.get.description")]
        [GetEndpoint]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken)
        {
            var result = await approvalRequestService.GetAllApprovals(request, cancellationToken);
            return Http200(new PagedResult<OpportunityApprovalRequestContract>
            {
                Items = result.Items.Select(OpportunityContractExtensions.MapApprovalWithDetails).ToArray(),
                Pagination = result.Pagination
            });
        }

        [RequireAccess("opportunityApprovals.summary.description")]
        [GetEndpoint]
        public async Task<IActionResult> Summary(CancellationToken cancellationToken)
        {
            return Http200(await approvalRequestService.GetApprovalsSummary(cancellationToken));
        }

        [RequireAccess("opportunityApprovals.getByNegotiation.description")]
        [GetEndpoint("negotiation/{opportunityNegotiationId:long}")]
        public async Task<IActionResult> GetByNegotiation(long opportunityNegotiationId, CancellationToken cancellationToken)
        {
            IReadOnlyCollection<OpportunityApprovalRequest> approvals = await approvalRequestService.GetApprovalsByNegotiationId(opportunityNegotiationId, cancellationToken);
            return Http200(approvals.Select(OpportunityContractExtensions.MapApprovalRequest).ToList());
        }

        [RequireAccess("opportunityApprovals.create.description")]
        [PostEndpoint]
        public async Task<IActionResult> Create([FromBody] CreateOpportunityApprovalRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            OpportunityApprovalRequest approval = await approvalRequestService.CreateOpportunityApprovalRequest(request, cancellationToken);
            return Http201(OpportunityContractExtensions.MapApprovalWithDetails(approval), Localizer["record.created"]);
        }

        [RequireAccess("opportunityApprovals.approve.description")]
        [HttpPost("{id:long}/Approve")]
        public async Task<IActionResult> Approve(long id, [FromBody] DecideOpportunityApprovalRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            OpportunityApprovalRequest approval = await approvalRequestService.Approve(id, request, cancellationToken);
            return Http200(OpportunityContractExtensions.MapApprovalWithDetails(approval), Localizer["record.updated"]);
        }

        [RequireAccess("opportunityApprovals.reject.description")]
        [HttpPost("{id:long}/Reject")]
        public async Task<IActionResult> Reject(long id, [FromBody] DecideOpportunityApprovalRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            OpportunityApprovalRequest approval = await approvalRequestService.Reject(id, request, cancellationToken);
            return Http200(OpportunityContractExtensions.MapApprovalWithDetails(approval), Localizer["record.updated"]);
        }

        [RequireAccess("opportunityApprovals.markInReview.description")]
        [HttpPost("{id:long}/MarkInReview")]
        public async Task<IActionResult> MarkInReview(long id, CancellationToken cancellationToken)
        {
            OpportunityApprovalRequest approval = await approvalRequestService.MarkInReview(id, cancellationToken);
            return Http200(OpportunityContractExtensions.MapApprovalWithDetails(approval), Localizer["record.updated"]);
        }

        [RequireAccess("opportunityApprovals.requestChanges.description")]
        [HttpPost("{id:long}/RequestChanges")]
        public async Task<IActionResult> RequestChanges(long id, [FromBody] DecideOpportunityApprovalRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            OpportunityApprovalRequest approval = await approvalRequestService.RequestChanges(id, request, cancellationToken);
            return Http200(OpportunityContractExtensions.MapApprovalWithDetails(approval), Localizer["record.updated"]);
        }

        [RequireAccess("opportunityApprovals.resubmit.description")]
        [HttpPost("{id:long}/Resubmit")]
        public async Task<IActionResult> Resubmit(long id, [FromBody] ResubmitOpportunityApprovalRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            OpportunityApprovalRequest approval = await approvalRequestService.Resubmit(id, request, cancellationToken);
            return Http200(OpportunityContractExtensions.MapApprovalWithDetails(approval), Localizer["record.updated"]);
        }

        [RequireAccess("opportunityApprovals.markMerged.description")]
        [HttpPost("{id:long}/MarkMerged")]
        public async Task<IActionResult> MarkMerged(long id, CancellationToken cancellationToken)
        {
            OpportunityApprovalRequest approval = await approvalRequestService.MarkMerged(id, cancellationToken);
            return Http200(OpportunityContractExtensions.MapApprovalWithDetails(approval), Localizer["record.updated"]);
        }

        [RequireAccess("opportunityApprovals.getComments.description")]
        [HttpGet("{id:long}/Comments")]
        public async Task<IActionResult> GetComments(long id, CancellationToken cancellationToken)
        {
            return Http200(await commentService.GetByApprovalId(id, cancellationToken));
        }

        [RequireAccess("opportunityApprovals.createComment.description")]
        [HttpPost("{id:long}/Comments")]
        public async Task<IActionResult> CreateComment(long id, [FromBody] CreateOpportunityApprovalCommentRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            var result = await commentService.Create(id, request, cancellationToken);
            return Http201(result, Localizer["record.created"]);
        }

        [RequireAccess("opportunityApprovals.updateComment.description")]
        [HttpPut("Comments/{commentId:long}")]
        public async Task<IActionResult> UpdateComment(long commentId, [FromBody] UpdateOpportunityApprovalCommentRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            var result = await commentService.Update(commentId, request, cancellationToken);
            return Http200(result, Localizer["record.updated"]);
        }

        [RequireAccess("opportunityApprovals.deleteComment.description")]
        [HttpDelete("Comments/{commentId:long}")]
        public async Task<IActionResult> DeleteComment(long commentId, CancellationToken cancellationToken)
        {
            await commentService.Delete(commentId, cancellationToken);
            return Http204();
        }

        [RequireAccess("opportunityApprovals.getReviewers.description")]
        [HttpGet("{id:long}/Reviewers")]
        public async Task<IActionResult> GetReviewers(long id, CancellationToken cancellationToken)
        {
            return Http200(await reviewerService.GetByApprovalId(id, cancellationToken));
        }

        [RequireAccess("opportunityApprovals.addReviewer.description")]
        [HttpPost("{id:long}/Reviewers")]
        public async Task<IActionResult> AddReviewer(long id, [FromBody] AddOpportunityApprovalReviewerRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            var result = await reviewerService.Add(id, request, cancellationToken);
            return Http201(result, Localizer["record.created"]);
        }

        [RequireAccess("opportunityApprovals.removeReviewer.description")]
        [HttpDelete("Reviewers/{reviewerId:long}")]
        public async Task<IActionResult> RemoveReviewer(long reviewerId, CancellationToken cancellationToken)
        {
            await reviewerService.Remove(reviewerId, cancellationToken);
            return Http204();
        }

        [RequireAccess("opportunityApprovals.getDiffs.description")]
        [HttpGet("{id:long}/Diffs")]
        public async Task<IActionResult> GetDiffs(long id, CancellationToken cancellationToken)
        {
            return Http200(await diffService.GetByApprovalId(id, cancellationToken));
        }

        [RequireAccess("opportunityApprovals.addDiff.description")]
        [HttpPost("{id:long}/Diffs")]
        public async Task<IActionResult> AddDiff(long id, [FromBody] AddOpportunityApprovalDiffRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            var result = await diffService.Add(id, request, cancellationToken);
            return Http201(result, Localizer["record.created"]);
        }

        [RequireAccess("opportunityApprovals.removeDiff.description")]
        [HttpDelete("Diffs/{diffId:long}")]
        public async Task<IActionResult> RemoveDiff(long diffId, CancellationToken cancellationToken)
        {
            await diffService.Remove(diffId, cancellationToken);
            return Http204();
        }

        [RequireAccess("opportunityApprovals.getImpacts.description")]
        [HttpGet("{id:long}/Impacts")]
        public async Task<IActionResult> GetImpacts(long id, CancellationToken cancellationToken)
        {
            return Http200(await impactService.GetByApprovalId(id, cancellationToken));
        }

        [RequireAccess("opportunityApprovals.addImpact.description")]
        [HttpPost("{id:long}/Impacts")]
        public async Task<IActionResult> AddImpact(long id, [FromBody] AddOpportunityApprovalImpactRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            var result = await impactService.Add(id, request, cancellationToken);
            return Http201(result, Localizer["record.created"]);
        }

        [RequireAccess("opportunityApprovals.removeImpact.description")]
        [HttpDelete("Impacts/{impactId:long}")]
        public async Task<IActionResult> RemoveImpact(long impactId, CancellationToken cancellationToken)
        {
            await impactService.Remove(impactId, cancellationToken);
            return Http204();
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
                NegotiationAmount = approval.OpportunityNegotiation?.Amount,
                BrandId = approval.OpportunityNegotiation?.Opportunity?.BrandId,
                BrandName = approval.OpportunityNegotiation?.Opportunity?.Brand?.Name,
                BrandLogoUrl = approval.OpportunityNegotiation?.Opportunity?.Brand?.LogoUrl,
            };
        }
    }
}
