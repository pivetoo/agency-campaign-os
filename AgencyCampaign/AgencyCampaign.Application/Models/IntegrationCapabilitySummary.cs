namespace AgencyCampaign.Application.Models
{
    public sealed class IntegrationCapabilitySummary
    {
        public string IntentKey { get; set; } = string.Empty;

        public string Label { get; set; } = string.Empty;

        public string CategoryIdentifier { get; set; } = string.Empty;

        public string ServiceContractIdentifier { get; set; } = string.Empty;

        public long? ConfiguredConnectorId { get; set; }

        public bool IsActive { get; set; }

        public List<CapabilityConnectorOption> AvailableConnectors { get; set; } = [];
    }

    public sealed class CapabilityConnectorOption
    {
        public long Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; }
    }
}
