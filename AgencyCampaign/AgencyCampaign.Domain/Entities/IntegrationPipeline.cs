using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class IntegrationPipeline : Entity
    {
        public string Identifier { get; private set; } = string.Empty;

        public string Name { get; private set; } = string.Empty;

        public string? Description { get; private set; }

        public long IntegrationId { get; private set; }

        public Integration? Integration { get; private set; }

        public bool IsActive { get; private set; } = true;

        private IntegrationPipeline()
        {
        }

        public IntegrationPipeline(long integrationId, string identifier, string name, string? description = null)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(integrationId);
            ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            IntegrationId = integrationId;
            Identifier = identifier.Trim();
            Name = name.Trim();
            Description = Normalize(description);
            CreatedAt = DateTimeOffset.UtcNow;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void Update(string identifier, string name, string? description, bool isActive)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            Identifier = identifier.Trim();
            Name = name.Trim();
            Description = Normalize(description);
            IsActive = isActive;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
