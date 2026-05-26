using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class CampaignReportLink : Entity
    {
        public long CampaignId { get; private set; }

        public Campaign? Campaign { get; private set; }

        public string Token { get; private set; } = string.Empty;

        public DateTimeOffset? RevokedAt { get; private set; }

        public long? CreatedByUserId { get; private set; }

        public string? CreatedByUserName { get; private set; }

        public DateTimeOffset? LastViewedAt { get; private set; }

        public int ViewCount { get; private set; }

        private CampaignReportLink()
        {
        }

        public CampaignReportLink(long campaignId, string token, long? createdByUserId, string? createdByUserName)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(campaignId);
            ArgumentException.ThrowIfNullOrWhiteSpace(token);

            CampaignId = campaignId;
            Token = token.Trim();
            CreatedByUserId = createdByUserId;
            CreatedByUserName = string.IsNullOrWhiteSpace(createdByUserName) ? null : createdByUserName.Trim();
            CreatedAt = DateTimeOffset.UtcNow;
            UpdatedAt = CreatedAt;
        }

        public bool IsActive()
        {
            return !RevokedAt.HasValue;
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
