using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class CampaignCreatorStatusHistory : Entity
    {
        public long CampaignCreatorId { get; private set; }

        public CampaignCreator? CampaignCreator { get; private set; }

        public long? FromStatusId { get; private set; }

        public CampaignCreatorStatus? FromStatus { get; private set; }

        public long ToStatusId { get; private set; }

        public CampaignCreatorStatus? ToStatus { get; private set; }

        public DateTimeOffset ChangedAt { get; private set; }

        public long? ChangedByUserId { get; private set; }

        public string? ChangedByUserName { get; private set; }

        public string? Reason { get; private set; }

        private CampaignCreatorStatusHistory()
        {
        }

        public CampaignCreatorStatusHistory(long campaignCreatorId, long? fromStatusId, long toStatusId, long? changedByUserId, string? changedByUserName, string? reason = null)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(campaignCreatorId);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(toStatusId);

            CampaignCreatorId = campaignCreatorId;
            FromStatusId = fromStatusId;
            ToStatusId = toStatusId;
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
