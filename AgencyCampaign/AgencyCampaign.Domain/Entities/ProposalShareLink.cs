using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class ProposalShareLink : Entity
    {
        private readonly List<ProposalView> views = [];

        public long ProposalId { get; private set; }

        public Proposal? Proposal { get; private set; }

        public string Token { get; private set; } = string.Empty;

        public DateTimeOffset? ExpiresAt { get; private set; }

        public DateTimeOffset? RevokedAt { get; private set; }

        public long? CreatedByUserId { get; private set; }

        public string? CreatedByUserName { get; private set; }

        public DateTimeOffset? LastViewedAt { get; private set; }

        public int ViewCount { get; private set; }

        public IReadOnlyCollection<ProposalView> Views => views.AsReadOnly();

        private ProposalShareLink()
        {
        }

        public ProposalShareLink(long proposalId, string token, DateTimeOffset? expiresAt, long? createdByUserId, string? createdByUserName)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(proposalId);
            ArgumentException.ThrowIfNullOrWhiteSpace(token);

            ProposalId = proposalId;
            Token = token.Trim();
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

        public void RegisterView(string? ipAddress, string? userAgent)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            views.Add(new ProposalView(Id, ipAddress, userAgent));
            LastViewedAt = now;
            ViewCount += 1;
            UpdatedAt = now;
        }
    }
}
