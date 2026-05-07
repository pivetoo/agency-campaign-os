using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class ProposalTemplate : Entity
    {
        private readonly List<ProposalTemplateItem> items = [];

        public string Name { get; private set; } = string.Empty;

        public string? Description { get; private set; }

        public bool IsActive { get; private set; } = true;

        public long? CreatedByUserId { get; private set; }

        public string? CreatedByUserName { get; private set; }

        public IReadOnlyCollection<ProposalTemplateItem> Items => items.AsReadOnly();

        private ProposalTemplate()
        {
        }

        public ProposalTemplate(string name, string? description, long? createdByUserId, string? createdByUserName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            Name = name.Trim();
            Description = Normalize(description);
            CreatedByUserId = createdByUserId;
            CreatedByUserName = Normalize(createdByUserName);
            CreatedAt = DateTimeOffset.UtcNow;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void Update(string name, string? description, bool isActive)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

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
