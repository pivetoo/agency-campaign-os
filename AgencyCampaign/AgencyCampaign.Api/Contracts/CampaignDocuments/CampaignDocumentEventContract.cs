using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;

namespace AgencyCampaign.Api.Contracts.CampaignDocuments
{
    public sealed class CampaignDocumentEventContract
    {
        public long Id { get; init; }
        public CampaignDocumentEventType EventType { get; init; }
        public DateTimeOffset OccurredAt { get; init; }
        public string? Description { get; init; }
        public string? Metadata { get; init; }

        public static CampaignDocumentEventContract FromEntity(CampaignDocumentEvent evt) => new()
        {
            Id = evt.Id,
            EventType = evt.EventType,
            OccurredAt = evt.OccurredAt,
            Description = evt.Description,
            Metadata = evt.Metadata,
        };
    }
}
