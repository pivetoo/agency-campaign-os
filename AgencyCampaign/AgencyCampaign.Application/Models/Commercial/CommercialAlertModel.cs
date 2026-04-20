namespace AgencyCampaign.Application.Models.Commercial
{
    public sealed class CommercialAlertModel
    {
        public string Type { get; init; } = string.Empty;

        public string Severity { get; init; } = string.Empty;

        public string Title { get; init; } = string.Empty;

        public string Description { get; init; } = string.Empty;

        public long OpportunityId { get; init; }

        public string OpportunityName { get; init; } = string.Empty;

        public long? FollowUpId { get; init; }

        public DateTimeOffset? DueAt { get; init; }
    }
}
