using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;

namespace AgencyCampaign.Api.Contracts.CreatorPayments
{
    public sealed class CreatorPaymentEventContract
    {
        public long Id { get; init; }
        public CreatorPaymentEventType EventType { get; init; }
        public DateTimeOffset OccurredAt { get; init; }
        public string? Description { get; init; }
        public string? Metadata { get; init; }

        public static CreatorPaymentEventContract FromEntity(CreatorPaymentEvent evt) => new()
        {
            Id = evt.Id,
            EventType = evt.EventType,
            OccurredAt = evt.OccurredAt,
            Description = evt.Description,
            Metadata = evt.Metadata,
        };
    }
}
