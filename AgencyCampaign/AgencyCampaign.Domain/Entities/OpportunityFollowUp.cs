using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class OpportunityFollowUp : Entity
    {
        public long OpportunityId { get; private set; }

        public Opportunity? Opportunity { get; private set; }

        public string Subject { get; private set; } = string.Empty;

        public DateTimeOffset DueAt { get; private set; }

        public string? Notes { get; private set; }

        public bool IsCompleted { get; private set; }

        public DateTimeOffset? CompletedAt { get; private set; }

        private OpportunityFollowUp()
        {
        }

        public OpportunityFollowUp(long opportunityId, string subject, DateTimeOffset dueAt, string? notes = null)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(opportunityId);
            ArgumentException.ThrowIfNullOrWhiteSpace(subject);

            OpportunityId = opportunityId;
            Subject = subject.Trim();
            DueAt = dueAt.ToUniversalTime();
            Notes = Normalize(notes);
            CreatedAt = DateTimeOffset.UtcNow;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void Update(string subject, DateTimeOffset dueAt, string? notes)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(subject);

            Subject = subject.Trim();
            DueAt = dueAt.ToUniversalTime();
            Notes = Normalize(notes);
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void Complete()
        {
            if (IsCompleted)
            {
                throw new InvalidOperationException("Follow-up is already completed.");
            }

            IsCompleted = true;
            CompletedAt = DateTimeOffset.UtcNow;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
