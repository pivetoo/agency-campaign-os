using AgencyCampaign.Domain.ValueObjects;
using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class CampaignDocumentEvent : Entity
    {
        public long CampaignDocumentId { get; private set; }

        public CampaignDocument? CampaignDocument { get; private set; }

        public CampaignDocumentEventType EventType { get; private set; }

        public DateTimeOffset OccurredAt { get; private set; }

        public string? Description { get; private set; }

        public string? Metadata { get; private set; }

        private CampaignDocumentEvent()
        {
        }

        public CampaignDocumentEvent(long campaignDocumentId, CampaignDocumentEventType eventType, string? description = null, string? metadata = null, DateTimeOffset? occurredAt = null)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(campaignDocumentId);

            CampaignDocumentId = campaignDocumentId;
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
