using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class IntegrationLog : Entity
    {
        public long IntegrationPipelineId { get; private set; }

        public IntegrationPipeline? IntegrationPipeline { get; private set; }

        public int Status { get; private set; }

        public string? Payload { get; private set; }

        public string? Response { get; private set; }

        public long? DurationMs { get; private set; }

        public string? ErrorMessage { get; private set; }

        private IntegrationLog()
        {
        }

        public IntegrationLog(long integrationPipelineId, int status, string? payload = null, string? response = null, long? durationMs = null, string? errorMessage = null)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(integrationPipelineId);

            IntegrationPipelineId = integrationPipelineId;
            Status = status;
            Payload = payload;
            Response = response;
            DurationMs = durationMs;
            ErrorMessage = errorMessage;
            CreatedAt = DateTimeOffset.UtcNow;
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }
}
