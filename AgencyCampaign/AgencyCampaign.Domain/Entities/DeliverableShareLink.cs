using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class DeliverableShareLink : Entity
    {
        public long CampaignDeliverableId { get; private set; }

        public CampaignDeliverable? CampaignDeliverable { get; private set; }

        public string Token { get; private set; } = string.Empty;

        public string ReviewerName { get; private set; } = string.Empty;

        public DateTimeOffset? ExpiresAt { get; private set; }

        public DateTimeOffset? RevokedAt { get; private set; }

        public long? CreatedByUserId { get; private set; }

        public string? CreatedByUserName { get; private set; }

        public DateTimeOffset? LastViewedAt { get; private set; }

        public int ViewCount { get; private set; }

        private DeliverableShareLink()
        {
        }

        public DeliverableShareLink(long campaignDeliverableId, string token, string reviewerName, DateTimeOffset? expiresAt, long? createdByUserId, string? createdByUserName)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(campaignDeliverableId);
            ArgumentException.ThrowIfNullOrWhiteSpace(token);
            ArgumentException.ThrowIfNullOrWhiteSpace(reviewerName);

            CampaignDeliverableId = campaignDeliverableId;
            Token = token.Trim();
            ReviewerName = reviewerName.Trim();
            ExpiresAt = expiresAt?.ToUniversalTime();
            CreatedByUserId = createdByUserId;
            CreatedByUserName = string.IsNullOrWhiteSpace(createdByUserName) ? null : createdByUserName.Trim();
            CreatedAt = DateTimeOffset.UtcNow;
            UpdatedAt = CreatedAt;
        }

        public bool IsActive(DateTimeOffset now)
        {
            if (RevokedAt.HasValue)
            {
                return false;
            }

            if (ExpiresAt.HasValue && ExpiresAt.Value <= now)
            {
                return false;
            }

            return true;
        }

        public void Revoke()
        {
            if (RevokedAt.HasValue)
            {
                return;
            }

            RevokedAt = DateTimeOffset.UtcNow;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void RegisterView()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            LastViewedAt = now;
            ViewCount += 1;
            UpdatedAt = now;
        }
    }
}
