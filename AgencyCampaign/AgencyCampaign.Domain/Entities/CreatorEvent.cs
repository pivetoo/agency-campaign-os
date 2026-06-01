using AgencyCampaign.Domain.ValueObjects;
using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class CreatorEvent : Entity
    {
        public long CreatorId { get; private set; }

        public Creator? Creator { get; private set; }

        public CreatorEventType EventType { get; private set; }

        public DateTimeOffset OccurredAt { get; private set; }

        public string? Description { get; private set; }

        public string? Metadata { get; private set; }

        private CreatorEvent()
        {
        }

        public CreatorEvent(long creatorId, CreatorEventType eventType, string? description = null, string? metadata = null, DateTimeOffset? occurredAt = null)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(creatorId);

            CreatorId = creatorId;
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
