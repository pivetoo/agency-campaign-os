namespace AgencyCampaign.Application.Models.Commercial
{
    public sealed class AgencyIntegrationBindingModel
    {
        public long Id { get; init; }

        public string IntentKey { get; init; } = string.Empty;

        public long ConnectorId { get; init; }

        public long PipelineId { get; init; }

        public bool IsActive { get; init; }

        public string? CreatedByUserName { get; init; }

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset? UpdatedAt { get; init; }
    }
}
