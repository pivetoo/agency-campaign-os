using AgencyCampaign.Domain.ValueObjects;
using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class OpportunityApprovalRequest : Entity
    {
        private readonly List<OpportunityApprovalReviewer> reviewers = [];

        public long ProposalId { get; private set; }

        public Proposal? Proposal { get; private set; }

        public OpportunityApprovalType ApprovalType { get; private set; }

        public OpportunityApprovalStatus Status { get; private set; } = OpportunityApprovalStatus.Pending;

        public string Reason { get; private set; } = string.Empty;

        public long? RequestedByUserId { get; private set; }

        public string RequestedByUserName { get; private set; } = string.Empty;

        public long? ApprovedByUserId { get; private set; }

        public string? ApprovedByUserName { get; private set; }

        public DateTimeOffset RequestedAt { get; private set; }

        public DateTimeOffset? DecidedAt { get; private set; }

        public string? DecisionNotes { get; private set; }

        public IReadOnlyCollection<OpportunityApprovalReviewer> Reviewers => reviewers.AsReadOnly();

        private OpportunityApprovalRequest()
        {
        }

        public OpportunityApprovalRequest(long proposalId, OpportunityApprovalType approvalType, string reason, string requestedByUserName, long? requestedByUserId = null)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(proposalId);
            ArgumentException.ThrowIfNullOrWhiteSpace(reason);
            ArgumentException.ThrowIfNullOrWhiteSpace(requestedByUserName);

            ProposalId = proposalId;
            ApprovalType = approvalType;
            Reason = reason.Trim();
            RequestedByUserId = requestedByUserId;
            RequestedByUserName = requestedByUserName.Trim();
            RequestedAt = DateTimeOffset.UtcNow;
            CreatedAt = DateTimeOffset.UtcNow;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void Approve(string approvedByUserName, string? decisionNotes = null, long? approvedByUserId = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(approvedByUserName);
            EnsurePendingForDirectDecision();
            EnsureNoRequiredReviewers();

            Status = OpportunityApprovalStatus.Approved;
            ApprovedByUserId = approvedByUserId;
            ApprovedByUserName = approvedByUserName.Trim();
            DecisionNotes = Normalize(decisionNotes);
            DecidedAt = DateTimeOffset.UtcNow;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void Reject(string approvedByUserName, string? decisionNotes = null, long? approvedByUserId = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(approvedByUserName);
            EnsurePendingForDirectDecision();
            EnsureNoRequiredReviewers();

            Status = OpportunityApprovalStatus.Rejected;
            ApprovedByUserId = approvedByUserId;
            ApprovedByUserName = approvedByUserName.Trim();
            DecisionNotes = Normalize(decisionNotes);
            DecidedAt = DateTimeOffset.UtcNow;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void MarkInReview()
        {
            if (Status != OpportunityApprovalStatus.Pending && Status != OpportunityApprovalStatus.ChangesRequested)
            {
                throw new InvalidOperationException("opportunityApproval.transition.inReview.invalid");
            }

            Status = OpportunityApprovalStatus.InReview;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void RequestChanges(string requestedByUserName, string? notes = null, long? requestedByUserId = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(requestedByUserName);

            if (Status == OpportunityApprovalStatus.Approved || Status == OpportunityApprovalStatus.Rejected || Status == OpportunityApprovalStatus.Cancelled || Status == OpportunityApprovalStatus.Merged)
            {
                throw new InvalidOperationException("opportunityApproval.transition.changesRequested.invalid");
            }

            Status = OpportunityApprovalStatus.ChangesRequested;
            ApprovedByUserId = requestedByUserId;
            ApprovedByUserName = requestedByUserName.Trim();
            DecisionNotes = Normalize(notes);
            DecidedAt = DateTimeOffset.UtcNow;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void Resubmit(string requestedByUserName, string? newReason = null, long? requestedByUserId = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(requestedByUserName);

            if (Status != OpportunityApprovalStatus.ChangesRequested)
            {
                throw new InvalidOperationException("opportunityApproval.transition.resubmit.invalid");
            }

            Status = OpportunityApprovalStatus.Pending;
            RequestedByUserId = requestedByUserId ?? RequestedByUserId;
            RequestedByUserName = requestedByUserName.Trim();
            if (!string.IsNullOrWhiteSpace(newReason))
            {
                Reason = newReason.Trim();
            }
            DecidedAt = null;
            DecisionNotes = null;
            ApprovedByUserId = null;
            ApprovedByUserName = null;

            // Reabre a votacao: votos da versao anterior nao podem completar o quorum da versao alterada
            foreach (OpportunityApprovalReviewer reviewer in reviewers)
            {
                reviewer.ResetDecision();
            }

            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void MarkMerged()
        {
            if (Status != OpportunityApprovalStatus.Approved)
            {
                throw new InvalidOperationException("opportunityApproval.transition.merged.invalid");
            }

            Status = OpportunityApprovalStatus.Merged;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        // Invalida a aprovacao quando os termos da proposta mudam: a evidencia aprovada nao corresponde
        // mais ao que sera enviado, entao o gate de envio precisa reavaliar a politica do zero.
        public bool Supersede()
        {
            if (Status == OpportunityApprovalStatus.Rejected || Status == OpportunityApprovalStatus.Cancelled)
            {
                return false;
            }

            Status = OpportunityApprovalStatus.Cancelled;
            UpdatedAt = DateTimeOffset.UtcNow;
            return true;
        }

        public OpportunityApprovalReviewer AddReviewer(string userName, string? role, bool required, long? userId = null)
        {
            OpportunityApprovalReviewer reviewer = new(userName, role, required, userId);
            reviewers.Add(reviewer);
            return reviewer;
        }

        public void RegisterReviewerDecision(long decidingUserId, OpportunityApprovalReviewerStatus decision, string? notes = null)
        {
            if (Status != OpportunityApprovalStatus.Pending && Status != OpportunityApprovalStatus.InReview)
            {
                throw new InvalidOperationException("opportunityApproval.notPending");
            }

            OpportunityApprovalReviewer? reviewer = reviewers
                .FirstOrDefault(item => item.UserId == decidingUserId && item.Status == OpportunityApprovalReviewerStatus.Pending);

            if (reviewer is null)
            {
                throw new InvalidOperationException("opportunityApproval.reviewer.notPending");
            }

            reviewer.RecordDecision(decision, notes);
            RecomputeStatusFromReviewers(reviewer);
        }

        private void RecomputeStatusFromReviewers(OpportunityApprovalReviewer lastDecider)
        {
            List<OpportunityApprovalReviewer> required = reviewers
                .Where(item => item.Required)
                .ToList();

            bool anyRequiredRejected = required
                .Any(item => item.Status == OpportunityApprovalReviewerStatus.Rejected);

            if (anyRequiredRejected)
            {
                ApplyDerivedDecision(OpportunityApprovalStatus.Rejected, lastDecider);
                return;
            }

            bool allRequiredApproved = required.Count > 0
                && required.All(item => item.Status == OpportunityApprovalReviewerStatus.Approved);

            if (allRequiredApproved)
            {
                ApplyDerivedDecision(OpportunityApprovalStatus.Approved, lastDecider);
            }
        }

        private void ApplyDerivedDecision(OpportunityApprovalStatus status, OpportunityApprovalReviewer decider)
        {
            Status = status;
            ApprovedByUserId = decider.UserId;
            ApprovedByUserName = decider.UserName;
            DecisionNotes = decider.DecisionNotes;
            DecidedAt = DateTimeOffset.UtcNow;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        private void EnsurePendingForDirectDecision()
        {
            if (Status != OpportunityApprovalStatus.Pending)
            {
                throw new InvalidOperationException("opportunityApproval.notPending");
            }
        }

        private void EnsureNoRequiredReviewers()
        {
            if (reviewers.Any(item => item.Required))
            {
                throw new InvalidOperationException("opportunityApproval.decision.reviewersRequired");
            }
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
