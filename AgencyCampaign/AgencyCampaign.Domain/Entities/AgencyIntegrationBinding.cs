using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class AgencyIntegrationBinding : Entity
    {
        public string IntentKey { get; private set; } = string.Empty;

        public long ConnectorId { get; private set; }

        public long PipelineId { get; private set; }

        public bool IsActive { get; private set; } = true;

        public long? CreatedByUserId { get; private set; }

        public string? CreatedByUserName { get; private set; }

        private AgencyIntegrationBinding()
        {
        }

        public AgencyIntegrationBinding(string intentKey, long connectorId, long pipelineId, long? createdByUserId, string? createdByUserName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(intentKey);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(connectorId);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pipelineId);

            IntentKey = intentKey.Trim();
            ConnectorId = connectorId;
            PipelineId = pipelineId;
            IsActive = true;
            CreatedByUserId = createdByUserId;
            CreatedByUserName = string.IsNullOrWhiteSpace(createdByUserName) ? null : createdByUserName.Trim();
            CreatedAt = DateTimeOffset.UtcNow;
            UpdatedAt = CreatedAt;
        }

        public void UpdateTargets(long connectorId, long pipelineId)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(connectorId);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pipelineId);

            ConnectorId = connectorId;
            PipelineId = pipelineId;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void Activate()
        {
            if (IsActive)
            {
                return;
            }

            IsActive = true;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void Deactivate()
        {
            if (!IsActive)
            {
                return;
            }

            IsActive = false;
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }
}
