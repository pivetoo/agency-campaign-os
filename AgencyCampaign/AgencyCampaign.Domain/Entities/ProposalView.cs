using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class ProposalView : Entity
    {
        public long ProposalShareLinkId { get; private set; }

        public ProposalShareLink? ProposalShareLink { get; private set; }

        public DateTimeOffset ViewedAt { get; private set; }

        public string? IpAddress { get; private set; }

        public string? UserAgent { get; private set; }

        private ProposalView()
        {
        }

        public ProposalView(long proposalShareLinkId, string? ipAddress, string? userAgent)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(proposalShareLinkId);

            ProposalShareLinkId = proposalShareLinkId;
            ViewedAt = DateTimeOffset.UtcNow;
            IpAddress = string.IsNullOrWhiteSpace(ipAddress) ? null : ipAddress.Trim();
            UserAgent = string.IsNullOrWhiteSpace(userAgent) ? null : userAgent[..Math.Min(userAgent.Length, 500)].Trim();
            CreatedAt = ViewedAt;
            UpdatedAt = ViewedAt;
        }
    }
}
