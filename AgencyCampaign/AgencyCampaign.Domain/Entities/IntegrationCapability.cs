using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class IntegrationCapability : Entity
    {
        public string IntentKey { get; private set; } = string.Empty;

        public long ConnectorId { get; private set; }

        public bool IsActive { get; private set; } = true;

        private IntegrationCapability()
        {
        }

        public IntegrationCapability(string intentKey, long connectorId, bool isActive = true)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(intentKey);

            if (connectorId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(connectorId));
            }

            IntentKey = intentKey.Trim();
            ConnectorId = connectorId;
            IsActive = isActive;
        }

        public void UpdateConnector(long connectorId)
        {
            if (connectorId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(connectorId));
            }

            ConnectorId = connectorId;
        }

        public void Activate()
        {
            IsActive = true;
        }

        public void Deactivate()
        {
            IsActive = false;
        }
    }
}
