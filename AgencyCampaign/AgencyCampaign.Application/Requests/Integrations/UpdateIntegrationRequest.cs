namespace AgencyCampaign.Application.Requests.Integrations
{
    public sealed class UpdateIntegrationRequest : CreateIntegrationRequest
    {
        public long Id { get; set; }

        public bool IsActive { get; set; }
    }
}
