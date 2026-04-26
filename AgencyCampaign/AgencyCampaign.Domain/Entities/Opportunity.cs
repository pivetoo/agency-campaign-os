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

        public long CommercialPipelineStageId { get; private set; }

        public CommercialPipelineStage? CommercialPipelineStage { get; private set; }

        public decimal EstimatedValue { get; private set; }

        public DateTimeOffset? ExpectedCloseAt { get; private set; }

        public long? CommercialResponsibleId { get; private set; }

        public CommercialResponsible? CommercialResponsible { get; private set; }

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

        public Opportunity(long brandId, long commercialPipelineStageId, string name, decimal estimatedValue, DateTimeOffset? expectedCloseAt = null, string? description = null, long? commercialResponsibleId = null, string? contactName = null, string? contactEmail = null, string? notes = null)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(brandId);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(commercialPipelineStageId);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentOutOfRangeException.ThrowIfNegative(estimatedValue);

            BrandId = brandId;
            CommercialPipelineStageId = commercialPipelineStageId;
            Name = name.Trim();
            Description = Normalize(description);
            EstimatedValue = estimatedValue;
            ExpectedCloseAt = expectedCloseAt?.ToUniversalTime();
            CommercialResponsibleId = commercialResponsibleId;
            ContactName = Normalize(contactName);
            ContactEmail = Normalize(contactEmail);
            Notes = Normalize(notes);
            CreatedAt = DateTimeOffset.UtcNow;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void Update(long brandId, string name, decimal estimatedValue, DateTimeOffset? expectedCloseAt, string? description, long? commercialResponsibleId, string? contactName, string? contactEmail, string? notes)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(brandId);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentOutOfRangeException.ThrowIfNegative(estimatedValue);

            BrandId = brandId;
            Name = name.Trim();
            Description = Normalize(description);
            EstimatedValue = estimatedValue;
            ExpectedCloseAt = expectedCloseAt?.ToUniversalTime();
            CommercialResponsibleId = commercialResponsibleId;
            ContactName = Normalize(contactName);
            ContactEmail = Normalize(contactEmail);
            Notes = Normalize(notes);
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void ChangeStage(CommercialPipelineStage stage)
        {
            ArgumentNullException.ThrowIfNull(stage);

            if (!stage.IsActive)
            {
                throw new InvalidOperationException("Inactive pipeline stages cannot be used.");
            }

            CommercialPipelineStageId = stage.Id;
            ApplyStageFinalBehavior(stage, null);
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void CloseAsWon(CommercialPipelineStage stage, string? wonNotes)
        {
            ArgumentNullException.ThrowIfNull(stage);

            if (IsClosed())
            {
                throw new InvalidOperationException("Opportunity is already closed.");
            }

            CommercialPipelineStageId = stage.Id;
            WonNotes = Normalize(wonNotes);
            LossReason = null;
            ClosedAt = DateTimeOffset.UtcNow;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void CloseAsLost(CommercialPipelineStage stage, string lossReason)
        {
            ArgumentNullException.ThrowIfNull(stage);

            if (IsClosed())
            {
                throw new InvalidOperationException("Opportunity is already closed.");
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(lossReason);

            CommercialPipelineStageId = stage.Id;
            LossReason = lossReason.Trim();
            WonNotes = null;
            ClosedAt = DateTimeOffset.UtcNow;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        private bool IsClosed()
        {
            return ClosedAt.HasValue;
        }

        private void ApplyStageFinalBehavior(CommercialPipelineStage stage, string? notes)
        {
            if (stage.FinalBehavior == CommercialPipelineStageFinalBehavior.Won)
            {
                WonNotes = Normalize(notes) ?? "Oportunidade marcada como ganha pelo pipeline comercial.";
                LossReason = null;
                ClosedAt = DateTimeOffset.UtcNow;
                return;
            }

            if (stage.FinalBehavior == CommercialPipelineStageFinalBehavior.Lost)
            {
                WonNotes = null;
                LossReason = Normalize(notes) ?? "Oportunidade marcada como perdida pelo pipeline comercial.";
                ClosedAt = DateTimeOffset.UtcNow;
                return;
            }

            WonNotes = null;
            LossReason = null;
            ClosedAt = null;
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
