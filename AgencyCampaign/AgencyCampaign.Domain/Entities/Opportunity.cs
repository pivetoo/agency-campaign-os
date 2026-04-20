using AgencyCampaign.Domain.ValueObjects;
using Archon.Core.Entities;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class Opportunity : Entity
    {
        private readonly List<OpportunityNegotiation> negotiations = [];
        private readonly List<OpportunityFollowUp> followUps = [];
        private readonly List<Proposal> proposals = [];

        public long BrandId { get; private set; }

        public Brand? Brand { get; private set; }

        public string Name { get; private set; } = string.Empty;

        public string? Description { get; private set; }

        public OpportunityStage Stage { get; private set; } = OpportunityStage.Lead;

        public decimal EstimatedValue { get; private set; }

        public DateTimeOffset? ExpectedCloseAt { get; private set; }

        public long? InternalOwnerId { get; private set; }

        public string? InternalOwnerName { get; private set; }

        public string? ContactName { get; private set; }

        public string? ContactEmail { get; private set; }

        public string? Notes { get; private set; }

        public DateTimeOffset? ClosedAt { get; private set; }

        public string? LossReason { get; private set; }

        public string? WonNotes { get; private set; }

        public IReadOnlyCollection<OpportunityNegotiation> Negotiations => negotiations.AsReadOnly();

        public IReadOnlyCollection<OpportunityFollowUp> FollowUps => followUps.AsReadOnly();

        public IReadOnlyCollection<Proposal> Proposals => proposals.AsReadOnly();

        [NotMapped]
        public IReadOnlyCollection<OpportunityApprovalRequest> ApprovalRequests => negotiations.SelectMany(item => item.ApprovalRequests).ToArray();

        private Opportunity()
        {
        }

        public Opportunity(long brandId, string name, decimal estimatedValue, DateTimeOffset? expectedCloseAt = null, string? description = null, long? internalOwnerId = null, string? internalOwnerName = null, string? contactName = null, string? contactEmail = null, string? notes = null)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(brandId);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentOutOfRangeException.ThrowIfNegative(estimatedValue);

            BrandId = brandId;
            Name = name.Trim();
            Description = Normalize(description);
            EstimatedValue = estimatedValue;
            ExpectedCloseAt = expectedCloseAt?.ToUniversalTime();
            InternalOwnerId = internalOwnerId;
            InternalOwnerName = Normalize(internalOwnerName);
            ContactName = Normalize(contactName);
            ContactEmail = Normalize(contactEmail);
            Notes = Normalize(notes);
            Stage = OpportunityStage.Lead;
            CreatedAt = DateTimeOffset.UtcNow;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void Update(long brandId, string name, decimal estimatedValue, DateTimeOffset? expectedCloseAt, string? description, long? internalOwnerId, string? internalOwnerName, string? contactName, string? contactEmail, string? notes)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(brandId);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentOutOfRangeException.ThrowIfNegative(estimatedValue);

            BrandId = brandId;
            Name = name.Trim();
            Description = Normalize(description);
            EstimatedValue = estimatedValue;
            ExpectedCloseAt = expectedCloseAt?.ToUniversalTime();
            InternalOwnerId = internalOwnerId;
            InternalOwnerName = Normalize(internalOwnerName);
            ContactName = Normalize(contactName);
            ContactEmail = Normalize(contactEmail);
            Notes = Normalize(notes);
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void ChangeStage(OpportunityStage stage)
        {
            if (IsClosed())
            {
                throw new InvalidOperationException("Closed opportunities cannot change stage.");
            }

            if (stage == OpportunityStage.Won || stage == OpportunityStage.Lost)
            {
                throw new InvalidOperationException("Use the close actions to finish the opportunity.");
            }

            Stage = stage;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void CloseAsWon(string? wonNotes)
        {
            if (IsClosed())
            {
                throw new InvalidOperationException("Opportunity is already closed.");
            }

            Stage = OpportunityStage.Won;
            WonNotes = Normalize(wonNotes);
            LossReason = null;
            ClosedAt = DateTimeOffset.UtcNow;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void CloseAsLost(string lossReason)
        {
            if (IsClosed())
            {
                throw new InvalidOperationException("Opportunity is already closed.");
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(lossReason);

            Stage = OpportunityStage.Lost;
            LossReason = lossReason.Trim();
            WonNotes = null;
            ClosedAt = DateTimeOffset.UtcNow;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        private bool IsClosed()
        {
            return Stage == OpportunityStage.Won || Stage == OpportunityStage.Lost;
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
