using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using System.Linq.Expressions;

namespace AgencyCampaign.Api.Contracts.Opportunities
{
    public sealed class OpportunityContract
    {
        public long Id { get; init; }

        public long BrandId { get; init; }

        public string Name { get; init; } = string.Empty;

        public string? Description { get; init; }

        public long CommercialPipelineStageId { get; init; }

        public OpportunityStageReferenceContract? CommercialPipelineStage { get; init; }

        public decimal EstimatedValue { get; init; }

        public decimal Probability { get; init; }

        public bool ProbabilityIsManual { get; init; }

        public DateTimeOffset? ExpectedCloseAt { get; init; }

        public long? CommercialResponsibleId { get; init; }

        public CommercialResponsibleReferenceContract? CommercialResponsible { get; init; }

        public string? ContactName { get; init; }

        public string? ContactEmail { get; init; }

        public string? Notes { get; init; }

        public DateTimeOffset? ClosedAt { get; init; }

        public string? LossReason { get; init; }

        public string? WonNotes { get; init; }

        public OpportunityBrandReferenceContract? Brand { get; init; }

        public long? OpportunitySourceId { get; init; }

        public OpportunitySourceReferenceContract? OpportunitySource { get; init; }

        public List<OpportunityTagReferenceContract> Tags { get; init; } = [];

        public List<OpportunityNegotiationContract> Negotiations { get; init; } = [];

        public List<OpportunityFollowUpContract> FollowUps { get; init; } = [];

        public List<OpportunityProposalReferenceContract> Proposals { get; init; } = [];

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset? UpdatedAt { get; init; }

        public static Expression<Func<Opportunity, OpportunityContract>> Projection => item => new OpportunityContract
        {
            Id = item.Id,
            BrandId = item.BrandId,
            Name = item.Name,
            Description = item.Description,
            CommercialPipelineStageId = item.CommercialPipelineStageId,
            EstimatedValue = item.EstimatedValue,
            Probability = item.Probability,
            ProbabilityIsManual = item.ProbabilityIsManual,
            ExpectedCloseAt = item.ExpectedCloseAt,
            CommercialResponsibleId = item.CommercialResponsibleId,
            ContactName = item.ContactName,
            ContactEmail = item.ContactEmail,
            Notes = item.Notes,
            ClosedAt = item.ClosedAt,
            LossReason = item.LossReason,
            WonNotes = item.WonNotes,
            CommercialPipelineStage = item.CommercialPipelineStage == null
                ? null
                : new OpportunityStageReferenceContract
                {
                    Id = item.CommercialPipelineStage.Id,
                    Name = item.CommercialPipelineStage.Name,
                    Color = item.CommercialPipelineStage.Color,
                    DisplayOrder = item.CommercialPipelineStage.DisplayOrder,
                    IsFinal = item.CommercialPipelineStage.IsFinal,
                    FinalBehavior = (int)item.CommercialPipelineStage.FinalBehavior
                },
            CommercialResponsible = item.CommercialResponsible == null
                ? null
                : new CommercialResponsibleReferenceContract
                {
                    Id = item.CommercialResponsible.Id,
                    Name = item.CommercialResponsible.Name
                },
            Brand = item.Brand == null
                ? null
                : new OpportunityBrandReferenceContract
                {
                    Id = item.Brand.Id,
                    Name = item.Brand.Name
                },
            OpportunitySourceId = item.OpportunitySourceId,
            OpportunitySource = item.OpportunitySource == null
                ? null
                : new OpportunitySourceReferenceContract
                {
                    Id = item.OpportunitySource.Id,
                    Name = item.OpportunitySource.Name,
                    Color = item.OpportunitySource.Color
                },
            Tags = item.TagAssignments
                .Where(assignment => assignment.OpportunityTag != null)
                .Select(assignment => new OpportunityTagReferenceContract
                {
                    Id = assignment.OpportunityTag!.Id,
                    Name = assignment.OpportunityTag.Name,
                    Color = assignment.OpportunityTag.Color
                }).ToList(),
            Negotiations = item.Negotiations.ToList().Select(negotiation => new OpportunityNegotiationContract
            {
                Id = negotiation.Id,
                OpportunityId = negotiation.OpportunityId,
                Title = negotiation.Title,
                Amount = negotiation.Amount,
                Status = negotiation.Status,
                NegotiatedAt = negotiation.NegotiatedAt,
                Notes = negotiation.Notes,
                ApprovalRequests = negotiation.ApprovalRequests.ToList().Select(approval => new OpportunityApprovalRequestContract
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
                }).ToList(),
                CreatedAt = negotiation.CreatedAt,
                UpdatedAt = negotiation.UpdatedAt
            }).ToList(),
            FollowUps = item.FollowUps.ToList().Select(followUp => new OpportunityFollowUpContract
            {
                Id = followUp.Id,
                OpportunityId = followUp.OpportunityId,
                Subject = followUp.Subject,
                DueAt = followUp.DueAt,
                Notes = followUp.Notes,
                IsCompleted = followUp.IsCompleted,
                CompletedAt = followUp.CompletedAt,
                CreatedAt = followUp.CreatedAt,
                UpdatedAt = followUp.UpdatedAt
            }).ToList(),
            Proposals = item.Proposals.ToList().Select(proposal => new OpportunityProposalReferenceContract
            {
                Id = proposal.Id,
                Name = proposal.Name,
                Status = proposal.Status,
                TotalValue = proposal.TotalValue,
                ValidityUntil = proposal.ValidityUntil,
                CampaignId = proposal.CampaignId
            }).ToList(),
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
    }

    public sealed class OpportunityStageReferenceContract
    {
        public long Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Color { get; init; } = "#6366f1";
        public int DisplayOrder { get; init; }
        public bool IsFinal { get; init; }
        public int FinalBehavior { get; init; }
    }

    public sealed class CommercialResponsibleReferenceContract
    {
        public long Id { get; init; }
        public string Name { get; init; } = string.Empty;
    }

    public sealed class OpportunityNegotiationContract
    {
        public long Id { get; init; }

        public long OpportunityId { get; init; }

        public string Title { get; init; } = string.Empty;

        public decimal Amount { get; init; }

        public OpportunityNegotiationStatus Status { get; init; }

        public DateTimeOffset NegotiatedAt { get; init; }

        public string? Notes { get; init; }

        public List<OpportunityApprovalRequestContract> ApprovalRequests { get; init; } = [];

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset? UpdatedAt { get; init; }

        public static Expression<Func<OpportunityNegotiation, OpportunityNegotiationContract>> Projection => item => new OpportunityNegotiationContract
        {
            Id = item.Id,
            OpportunityId = item.OpportunityId,
            Title = item.Title,
            Amount = item.Amount,
            Status = item.Status,
            NegotiatedAt = item.NegotiatedAt,
            Notes = item.Notes,
            ApprovalRequests = item.ApprovalRequests.ToList().Select(approval => new OpportunityApprovalRequestContract
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
            }).ToList(),
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
    }

    public sealed class OpportunityApprovalRequestContract
    {
        public long Id { get; init; }

        public long OpportunityNegotiationId { get; init; }

        public OpportunityApprovalType ApprovalType { get; init; }

        public OpportunityApprovalStatus Status { get; init; }

        public string Reason { get; init; } = string.Empty;

        public long? RequestedByUserId { get; init; }

        public string RequestedByUserName { get; init; } = string.Empty;

        public long? ApprovedByUserId { get; init; }

        public string? ApprovedByUserName { get; init; }

        public DateTimeOffset RequestedAt { get; init; }

        public DateTimeOffset? DecidedAt { get; init; }

        public string? DecisionNotes { get; init; }

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset? UpdatedAt { get; init; }
    }

    public sealed class OpportunityFollowUpContract
    {
        public long Id { get; init; }

        public long OpportunityId { get; init; }

        public string Subject { get; init; } = string.Empty;

        public DateTimeOffset DueAt { get; init; }

        public string? Notes { get; init; }

        public bool IsCompleted { get; init; }

        public DateTimeOffset? CompletedAt { get; init; }

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset? UpdatedAt { get; init; }

        public static Expression<Func<OpportunityFollowUp, OpportunityFollowUpContract>> Projection => item => new OpportunityFollowUpContract
        {
            Id = item.Id,
            OpportunityId = item.OpportunityId,
            Subject = item.Subject,
            DueAt = item.DueAt,
            Notes = item.Notes,
            IsCompleted = item.IsCompleted,
            CompletedAt = item.CompletedAt,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
    }

    public sealed class OpportunityBrandReferenceContract
    {
        public long Id { get; init; }

        public string Name { get; init; } = string.Empty;
    }

    public sealed class OpportunityProposalReferenceContract
    {
        public long Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public ProposalStatus Status { get; init; }

        public decimal TotalValue { get; init; }

        public DateTimeOffset? ValidityUntil { get; init; }

        public long? CampaignId { get; init; }
    }

    public sealed class OpportunitySourceReferenceContract
    {
        public long Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public string Color { get; init; } = "#6366f1";
    }

    public sealed class OpportunityTagReferenceContract
    {
        public long Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public string Color { get; init; } = "#6366f1";
    }
}
