using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Application.Notifications;
using AgencyCampaign.Application.Requests.Opportunities;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using Archon.Application.Abstractions;
using Archon.Application.Services;
using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using Archon.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class OpportunityApprovalRequestService : CrudService<OpportunityApprovalRequest>, IOpportunityApprovalRequestService
    {
        private readonly INotificationService notificationService;
        private readonly IPolicyEvaluator policyEvaluator;
        private readonly ICurrentUser currentUser;
        private readonly ILogger<OpportunityApprovalRequestService>? logger;

        public OpportunityApprovalRequestService(DbContext dbContext, INotificationService notificationService, IPolicyEvaluator policyEvaluator, ICurrentUser currentUser, ILogger<OpportunityApprovalRequestService>? logger = null) : base(dbContext)
        {
            this.notificationService = notificationService;
            this.policyEvaluator = policyEvaluator;
            this.currentUser = currentUser;
            this.logger = logger;
        }

        public async Task<OpportunityApprovalRequest?> GetOpportunityApprovalRequestById(long id, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        }

        public async Task<OpportunityApprovalRequest> CreateOpportunityApprovalRequest(CreateOpportunityApprovalRequest request, CancellationToken cancellationToken = default)
        {
            await EnsureProposalExists(request.ProposalId, cancellationToken);

            OpportunityApprovalRequest approvalRequest = new(
                request.ProposalId,
                request.ApprovalType,
                request.Reason,
                request.RequestedByUserName,
                request.RequestedByUserId);

            List<ApproverRequest> approvers = (request.Approvers ?? [])
                .Where(item => item.UserId > 0 && item.UserId != request.RequestedByUserId && !string.IsNullOrWhiteSpace(item.UserName))
                .GroupBy(item => item.UserId)
                .Select(group => group.First())
                .ToList();

            foreach (ApproverRequest approver in approvers)
            {
                approvalRequest.AddReviewer(approver.UserName, null, required: true, approver.UserId);
            }

            bool success = await Insert(cancellationToken, approvalRequest);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            // Re-busca approval fresca: o Insert limpa o ChangeTracker, entao o auto-populate
            // recarrega a entidade rastreada antes de salvar diffs/impactos.
            await PopulateFromPolicy(approvalRequest.Id, cancellationToken);

            (long? opportunityId, string opportunityName) = await ResolveOpportunityFromProposalAsync(request.ProposalId, cancellationToken);

            if (approvers.Count == 0)
            {
                await TryNotify(KanvasNotifications.OpportunityApprovalRequested(approvalRequest, opportunityId, opportunityName), cancellationToken);
            }
            else
            {
                foreach (ApproverRequest approver in approvers)
                {
                    await TryNotify(KanvasNotifications.OpportunityApprovalRequested(approvalRequest, opportunityId, opportunityName, approver.UserId), cancellationToken);
                }
            }

            return await GetOpportunityApprovalRequestById(approvalRequest.Id, cancellationToken) ?? approvalRequest;
        }

        public async Task<OpportunityApprovalRequest> Approve(long id, DecideOpportunityApprovalRequest request, CancellationToken cancellationToken = default)
        {
            OpportunityApprovalRequest approvalRequest = await GetTrackedApproval(id, cancellationToken);
            approvalRequest.Approve(RequireCurrentUserName(), request.DecisionNotes, currentUser.UserId);

            await DbContext.SaveChangesAsync(cancellationToken);

            (long? opportunityId, string opportunityName) = await ResolveOpportunityFromProposalAsync(approvalRequest.ProposalId, cancellationToken);
            await TryNotify(KanvasNotifications.OpportunityApprovalDecided(approvalRequest, opportunityId, opportunityName, approved: true), cancellationToken);

            return await GetOpportunityApprovalRequestById(id, cancellationToken) ?? approvalRequest;
        }

        public async Task<OpportunityApprovalRequest> Reject(long id, DecideOpportunityApprovalRequest request, CancellationToken cancellationToken = default)
        {
            OpportunityApprovalRequest approvalRequest = await GetTrackedApproval(id, cancellationToken);
            approvalRequest.Reject(RequireCurrentUserName(), request.DecisionNotes, currentUser.UserId);

            await DbContext.SaveChangesAsync(cancellationToken);

            (long? opportunityId, string opportunityName) = await ResolveOpportunityFromProposalAsync(approvalRequest.ProposalId, cancellationToken);
            await TryNotify(KanvasNotifications.OpportunityApprovalDecided(approvalRequest, opportunityId, opportunityName, approved: false), cancellationToken);

            return await GetOpportunityApprovalRequestById(id, cancellationToken) ?? approvalRequest;
        }

        public async Task<OpportunityApprovalRequest> RecordReviewerDecision(long id, OpportunityApprovalReviewerStatus decision, string? notes = null, CancellationToken cancellationToken = default)
        {
            if (currentUser.UserId is null)
            {
                throw new InvalidOperationException("opportunityApproval.reviewer.notPending");
            }

            OpportunityApprovalRequest approvalRequest = await DbContext.Set<OpportunityApprovalRequest>()
                .AsTracking()
                .Include(item => item.Reviewers)
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
                ?? throw new InvalidOperationException("record.notFound");

            OpportunityApprovalStatus previousStatus = approvalRequest.Status;
            approvalRequest.RegisterReviewerDecision(currentUser.UserId.Value, decision, notes);

            bool resolvedNow = previousStatus != approvalRequest.Status
                && (approvalRequest.Status == OpportunityApprovalStatus.Approved || approvalRequest.Status == OpportunityApprovalStatus.Rejected);

            await DbContext.SaveChangesAsync(cancellationToken);

            if (resolvedNow)
            {
                (long? opportunityId, string opportunityName) = await ResolveOpportunityFromProposalAsync(approvalRequest.ProposalId, cancellationToken);
                await TryNotify(KanvasNotifications.OpportunityApprovalDecided(approvalRequest, opportunityId, opportunityName, approved: approvalRequest.Status == OpportunityApprovalStatus.Approved), cancellationToken);
            }

            return await GetOpportunityApprovalRequestById(id, cancellationToken) ?? approvalRequest;
        }

        public async Task<OpportunityApprovalRequest> MarkInReview(long id, CancellationToken cancellationToken = default)
        {
            OpportunityApprovalRequest approvalRequest = await GetTrackedApproval(id, cancellationToken);
            approvalRequest.MarkInReview();
            await DbContext.SaveChangesAsync(cancellationToken);
            return await GetOpportunityApprovalRequestById(id, cancellationToken) ?? approvalRequest;
        }

        public async Task<OpportunityApprovalRequest> RequestChanges(long id, DecideOpportunityApprovalRequest request, CancellationToken cancellationToken = default)
        {
            OpportunityApprovalRequest approvalRequest = await GetTrackedApproval(id, cancellationToken);
            approvalRequest.RequestChanges(RequireCurrentUserName(), request.DecisionNotes, currentUser.UserId);
            await DbContext.SaveChangesAsync(cancellationToken);
            return await GetOpportunityApprovalRequestById(id, cancellationToken) ?? approvalRequest;
        }

        public async Task<OpportunityApprovalRequest> Resubmit(long id, ResubmitOpportunityApprovalRequest request, CancellationToken cancellationToken = default)
        {
            OpportunityApprovalRequest approvalRequest = await GetTrackedApproval(id, cancellationToken);
            approvalRequest.Resubmit(request.RequestedByUserName, request.Reason, request.RequestedByUserId);
            await DbContext.SaveChangesAsync(cancellationToken);
            return await GetOpportunityApprovalRequestById(id, cancellationToken) ?? approvalRequest;
        }

        public async Task<OpportunityApprovalRequest> MarkMerged(long id, CancellationToken cancellationToken = default)
        {
            OpportunityApprovalRequest approvalRequest = await GetTrackedApproval(id, cancellationToken);
            approvalRequest.MarkMerged();
            await DbContext.SaveChangesAsync(cancellationToken);
            return await GetOpportunityApprovalRequestById(id, cancellationToken) ?? approvalRequest;
        }

        private async Task<(long? opportunityId, string opportunityName)> ResolveOpportunityFromProposalAsync(long proposalId, CancellationToken cancellationToken)
        {
            var info = await DbContext.Set<Proposal>()
                .AsNoTracking()
                .Where(item => item.Id == proposalId)
                .Select(item => new { item.OpportunityId, OpportunityName = item.Opportunity!.Name })
                .FirstOrDefaultAsync(cancellationToken);

            return info is null ? (null, "oportunidade") : (info.OpportunityId, info.OpportunityName);
        }

        private async Task TryNotify(Archon.Core.Notifications.CreateNotificationRequest request, CancellationToken cancellationToken)
        {
            try
            {
                await notificationService.Create(request, cancellationToken);
            }
            catch (Exception exception)
            {
                logger?.LogWarning(exception, "Failed to create opportunity-approval notification.");
            }
        }

        public async Task<IReadOnlyCollection<OpportunityApprovalRequest>> GetApprovalsByProposalId(long proposalId, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .Where(item => item.ProposalId == proposalId)
                .OrderByDescending(item => item.RequestedAt)
                .ThenByDescending(item => item.Id)
                .ToListAsync(cancellationToken);
        }

        public async Task<PagedResult<OpportunityApprovalRequest>> GetAllApprovals(PagedRequest request, CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<OpportunityApprovalRequest>()
                .AsNoTracking()
                .Include(item => item.Proposal)
                    .ThenInclude(p => p!.Opportunity)
                        .ThenInclude(o => o!.Brand)
                .OrderByDescending(item => item.RequestedAt)
                .ThenByDescending(item => item.Id)
                .ToPagedResultAsync(request, cancellationToken);
        }

        public async Task<ApprovalSummaryModel> GetApprovalsSummary(CancellationToken cancellationToken = default)
        {
            var counts = await DbContext.Set<OpportunityApprovalRequest>()
                .AsNoTracking()
                .GroupBy(item => item.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            return new ApprovalSummaryModel
            {
                Pending = counts.FirstOrDefault(c => c.Status == OpportunityApprovalStatus.Pending)?.Count ?? 0,
                Approved = counts.FirstOrDefault(c => c.Status == OpportunityApprovalStatus.Approved)?.Count ?? 0,
                Rejected = counts.FirstOrDefault(c => c.Status == OpportunityApprovalStatus.Rejected)?.Count ?? 0,
            };
        }

        private async Task<OpportunityApprovalRequest> GetTrackedApproval(long id, CancellationToken cancellationToken)
        {
            OpportunityApprovalRequest? approvalRequest = await DbContext.Set<OpportunityApprovalRequest>()
                .AsTracking()
                .Include(item => item.Reviewers)
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (approvalRequest is null)
            {
                throw new InvalidOperationException("record.notFound");
            }

            return approvalRequest;
        }

        private string RequireCurrentUserName()
        {
            if (string.IsNullOrWhiteSpace(currentUser.UserName))
            {
                throw new InvalidOperationException("opportunityApproval.decision.unauthenticated");
            }

            return currentUser.UserName;
        }

        private async Task EnsureProposalExists(long proposalId, CancellationToken cancellationToken)
        {
            bool exists = await DbContext.Set<Proposal>()
                .AsNoTracking()
                .AnyAsync(item => item.Id == proposalId, cancellationToken);

            if (!exists)
            {
                throw new InvalidOperationException("record.notFound");
            }
        }

        public async Task PopulateFromPolicy(long id, CancellationToken cancellationToken = default)
        {
            OpportunityApprovalRequest approval = await GetTrackedApproval(id, cancellationToken);

            Proposal? proposal = await DbContext.Set<Proposal>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == approval.ProposalId, cancellationToken);

            List<OpportunityApprovalDiff> autoDiffs = await DbContext.Set<OpportunityApprovalDiff>()
                .Where(item => item.OpportunityApprovalRequestId == id && item.IsAutoGenerated)
                .ToListAsync(cancellationToken);
            DbContext.Set<OpportunityApprovalDiff>().RemoveRange(autoDiffs);

            List<OpportunityApprovalImpact> autoImpacts = await DbContext.Set<OpportunityApprovalImpact>()
                .Where(item => item.OpportunityApprovalRequestId == id && item.IsAutoGenerated)
                .ToListAsync(cancellationToken);
            DbContext.Set<OpportunityApprovalImpact>().RemoveRange(autoImpacts);

            await DbContext.SaveChangesAsync(cancellationToken);

            if (proposal is null)
            {
                return;
            }

            await AutoPopulateFromPolicyAsync(approval, proposal, cancellationToken);
        }

        private async Task AutoPopulateFromPolicyAsync(OpportunityApprovalRequest approval, Proposal proposal, CancellationToken cancellationToken)
        {
            try
            {
                PolicyEvaluationModel evaluation = await policyEvaluator.EvaluateProposalAsync(proposal, cancellationToken);
                if (evaluation.Deviations.Count == 0)
                {
                    return;
                }

                int index = 0;
                foreach (PolicyDeviationModel deviation in evaluation.Deviations)
                {
                    DbContext.Set<OpportunityApprovalDiff>().Add(new OpportunityApprovalDiff(
                        approval.Id,
                        deviation.Field,
                        deviation.PolicyValue,
                        deviation.RequestedValue,
                        (OpportunityApprovalDiffKind)deviation.Kind,
                        deviation.Delta,
                        index++,
                        isAutoGenerated: true));
                }

                int impactIndex = 0;
                foreach (PolicyImpactModel impact in evaluation.Impacts)
                {
                    DbContext.Set<OpportunityApprovalImpact>().Add(new OpportunityApprovalImpact(
                        approval.Id,
                        impact.Label,
                        impact.Value,
                        impact.IsGood,
                        impactIndex++,
                        isAutoGenerated: true));
                }

                await DbContext.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                // auto-populate é best-effort; falha aqui não invalida criação da aprovação
            }
        }

        private IQueryable<OpportunityApprovalRequest> QueryWithDetails()
        {
            return DbContext.Set<OpportunityApprovalRequest>()
                .AsNoTracking()
                .Include(item => item.Reviewers)
                .Include(item => item.Proposal)
                    .ThenInclude(p => p!.Opportunity)
                        .ThenInclude(o => o!.Brand);
        }
    }
}
