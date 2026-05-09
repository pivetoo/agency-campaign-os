using AgencyCampaign.Domain.ValueObjects;
using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class CreatorPaymentEvent : Entity
    {
        public long CreatorPaymentId { get; private set; }

        public CreatorPayment? CreatorPayment { get; private set; }

        public CreatorPaymentEventType EventType { get; private set; }

        public DateTimeOffset OccurredAt { get; private set; }

        public string? Description { get; private set; }

        public string? Metadata { get; private set; }

        private CreatorPaymentEvent()
        {
        }

        public CreatorPaymentEvent(long creatorPaymentId, CreatorPaymentEventType eventType, string? description = null, string? metadata = null, DateTimeOffset? occurredAt = null)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(creatorPaymentId);

            CreatorPaymentId = creatorPaymentId;
            EventType = eventType;
            Description = Normalize(description);
            Metadata = Normalize(metadata);
            OccurredAt = (occurredAt ?? DateTimeOffset.UtcNow).ToUniversalTime();
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
