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

        public OpportunityStage Stage { get; init; }

        public decimal EstimatedValue { get; init; }

        public DateTimeOffset? ExpectedCloseAt { get; init; }

        public long? InternalOwnerId { get; init; }

        public string? InternalOwnerName { get; init; }

        public string? ContactName { get; init; }

        public string? ContactEmail { get; init; }

        public string? Notes { get; init; }

        public DateTimeOffset? ClosedAt { get; init; }

        public string? LossReason { get; init; }

        public string? WonNotes { get; init; }

        public OpportunityBrandReferenceContract? Brand { get; init; }

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
            Stage = item.Stage,
            EstimatedValue = item.EstimatedValue,
            ExpectedCloseAt = item.ExpectedCloseAt,
            InternalOwnerId = item.InternalOwnerId,
            InternalOwnerName = item.InternalOwnerName,
            ContactName = item.ContactName,
            ContactEmail = item.ContactEmail,
            Notes = item.Notes,
            ClosedAt = item.ClosedAt,
            LossReason = item.LossReason,
            WonNotes = item.WonNotes,
            Brand = item.Brand == null
                ? null
                : new OpportunityBrandReferenceContract
                {
                    Id = item.Brand.Id,
                    Name = item.Brand.Name
                },
            Negotiations = item.Negotiations.ToList().Select(negotiation => new OpportunityNegotiationContract
            {
                Id = negotiation.Id,
                OpportunityId = negotiation.OpportunityId,
                Title = negotiation.Title,
                Amount = negotiation.Amount,
                NegotiatedAt = negotiation.NegotiatedAt,
                Notes = negotiation.Notes,
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

    public sealed class OpportunityNegotiationContract
    {
        public long Id { get; init; }

        public long OpportunityId { get; init; }

        public string Title { get; init; } = string.Empty;

        public decimal Amount { get; init; }

        public DateTimeOffset NegotiatedAt { get; init; }

        public string? Notes { get; init; }

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset? UpdatedAt { get; init; }

        public static Expression<Func<OpportunityNegotiation, OpportunityNegotiationContract>> Projection => item => new OpportunityNegotiationContract
        {
            Id = item.Id,
            OpportunityId = item.OpportunityId,
            Title = item.Title,
            Amount = item.Amount,
            NegotiatedAt = item.NegotiatedAt,
            Notes = item.Notes,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
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
}
