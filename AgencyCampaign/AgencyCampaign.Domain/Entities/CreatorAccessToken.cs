using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class CreatorAccessToken : Entity
    {
        public long CreatorId { get; private set; }

        public Creator? Creator { get; private set; }

        public string Token { get; private set; } = string.Empty;

        public DateTimeOffset? ExpiresAt { get; private set; }

        public DateTimeOffset? RevokedAt { get; private set; }

        public DateTimeOffset? LastUsedAt { get; private set; }

        public int UsageCount { get; private set; }

        public string? Note { get; private set; }

        public long? CreatedByUserId { get; private set; }

        public string? CreatedByUserName { get; private set; }

        private CreatorAccessToken()
        {
        }

        public CreatorAccessToken(long creatorId, string token, DateTimeOffset? expiresAt = null, string? note = null, long? createdByUserId = null, string? createdByUserName = null)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(creatorId);
            ArgumentException.ThrowIfNullOrWhiteSpace(token);

            CreatorId = creatorId;
            Token = token.Trim();
            ExpiresAt = expiresAt?.ToUniversalTime();
            Note = Normalize(note);
            CreatedByUserId = createdByUserId;
            CreatedByUserName = Normalize(createdByUserName);
        }

        public bool IsValid(DateTimeOffset now)
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
        }

        public void RegisterUse()
        {
            LastUsedAt = DateTimeOffset.UtcNow;
            UsageCount += 1;
        }

        public void UpdateExpiration(DateTimeOffset? expiresAt)
        {
            ExpiresAt = expiresAt?.ToUniversalTime();
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
