namespace AgencyCampaign.Application.Requests.IntegrationPipelines
{
    public sealed class UpdateIntegrationPipelineRequest : CreateIntegrationPipelineRequest
    {
        public long Id { get; set; }

        public bool IsActive { get; set; }
    }
}
