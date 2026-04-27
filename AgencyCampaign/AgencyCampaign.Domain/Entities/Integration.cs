using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class Integration : Entity
    {
        private readonly List<IntegrationPipeline> pipelines = [];

        public string Identifier { get; private set; } = string.Empty;

        public string Name { get; private set; } = string.Empty;

        public string? Description { get; private set; }

        public long CategoryId { get; private set; }

        public bool IsActive { get; private set; } = true;

        public IReadOnlyCollection<IntegrationPipeline> Pipelines => pipelines.AsReadOnly();

        private Integration()
        {
        }

        public Integration(string identifier, string name, long categoryId, string? description = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            Identifier = identifier.Trim();
            Name = name.Trim();
            CategoryId = categoryId;
            Description = Normalize(description);
            CreatedAt = DateTimeOffset.UtcNow;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void Update(string identifier, string name, long categoryId, string? description, bool isActive)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            Identifier = identifier.Trim();
            Name = name.Trim();
            CategoryId = categoryId;
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
