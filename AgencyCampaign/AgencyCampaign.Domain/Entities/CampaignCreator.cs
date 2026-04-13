using AgencyCampaign.Domain.ValueObjects;
using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class CampaignCreator : Entity
    {
        private readonly List<CampaignDeliverable> deliverables = [];

        public long CampaignId { get; private set; }

        public Campaign? Campaign { get; private set; }

        public long CreatorId { get; private set; }

        public Creator? Creator { get; private set; }

        public CampaignCreatorStatus Status { get; private set; } = CampaignCreatorStatus.Invited;

        public decimal AgreedAmount { get; private set; }

        public decimal AgencyFeeAmount { get; private set; }

        public string? Notes { get; private set; }

        public DateTimeOffset? ConfirmedAt { get; private set; }

        public DateTimeOffset? CancelledAt { get; private set; }

        public IReadOnlyCollection<CampaignDeliverable> Deliverables => deliverables.AsReadOnly();

        private CampaignCreator()
        {
        }

        public CampaignCreator(long campaignId, long creatorId, decimal agreedAmount, decimal agencyFeeAmount, string? notes = null)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(campaignId);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(creatorId);
            ArgumentOutOfRangeException.ThrowIfNegative(agreedAmount);
            ArgumentOutOfRangeException.ThrowIfNegative(agencyFeeAmount);

            CampaignId = campaignId;
            CreatorId = creatorId;
            AgreedAmount = agreedAmount;
            AgencyFeeAmount = agencyFeeAmount;
            Notes = Normalize(notes);
        }

        public void Update(decimal agreedAmount, decimal agencyFeeAmount, string? notes)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(agreedAmount);
            ArgumentOutOfRangeException.ThrowIfNegative(agencyFeeAmount);

            AgreedAmount = agreedAmount;
            AgencyFeeAmount = agencyFeeAmount;
            Notes = Normalize(notes);
        }

        public void ChangeStatus(CampaignCreatorStatus status, DateTimeOffset? occurredAt = null)
        {
            Status = status;
            DateTimeOffset timestamp = occurredAt?.ToUniversalTime() ?? DateTimeOffset.UtcNow;

            if (status == CampaignCreatorStatus.Confirmed)
            {
                ConfirmedAt ??= timestamp;
                CancelledAt = null;
                return;
            }

            if (status == CampaignCreatorStatus.Cancelled)
            {
                CancelledAt = timestamp;
                return;
            }

            CancelledAt = null;
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
