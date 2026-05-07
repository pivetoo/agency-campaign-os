using AgencyCampaign.Domain.ValueObjects;
using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class CampaignStatusHistory : Entity
    {
        public long CampaignId { get; private set; }

        public Campaign? Campaign { get; private set; }

        public CampaignStatus? FromStatus { get; private set; }

        public CampaignStatus ToStatus { get; private set; }

        public DateTimeOffset ChangedAt { get; private set; }

        public long? ChangedByUserId { get; private set; }

        public string? ChangedByUserName { get; private set; }

        public string? Reason { get; private set; }

        private CampaignStatusHistory()
        {
        }

        public CampaignStatusHistory(long campaignId, CampaignStatus? fromStatus, CampaignStatus toStatus, long? changedByUserId, string? changedByUserName, string? reason = null)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(campaignId);

            CampaignId = campaignId;
            FromStatus = fromStatus;
            ToStatus = toStatus;
            ChangedAt = DateTimeOffset.UtcNow;
            ChangedByUserId = changedByUserId;
            ChangedByUserName = Normalize(changedByUserName);
            Reason = Normalize(reason);
            CreatedAt = ChangedAt;
            UpdatedAt = ChangedAt;
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
