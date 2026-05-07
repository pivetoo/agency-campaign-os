using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class OpportunityComment : Entity
    {
        public long OpportunityId { get; private set; }

        public Opportunity? Opportunity { get; private set; }

        public long? AuthorUserId { get; private set; }

        public string AuthorName { get; private set; } = string.Empty;

        public string Body { get; private set; } = string.Empty;

        private OpportunityComment()
        {
        }

        public OpportunityComment(long opportunityId, string body, long? authorUserId, string authorName)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(opportunityId);
            ArgumentException.ThrowIfNullOrWhiteSpace(body);
            ArgumentException.ThrowIfNullOrWhiteSpace(authorName);

            OpportunityId = opportunityId;
            Body = body.Trim();
            AuthorUserId = authorUserId;
            AuthorName = authorName.Trim();
            CreatedAt = DateTimeOffset.UtcNow;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void Update(string body, long? requestingUserId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(body);

            if (AuthorUserId.HasValue && requestingUserId.HasValue && AuthorUserId.Value != requestingUserId.Value)
            {
                throw new InvalidOperationException("Only the comment author can edit it.");
            }

            Body = body.Trim();
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public bool CanBeDeletedBy(long? requestingUserId)
        {
            if (!AuthorUserId.HasValue || !requestingUserId.HasValue)
            {
                return true;
            }

            return AuthorUserId.Value == requestingUserId.Value;
        }
    }
}
