using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class ProposalBlock : Entity
    {
        public string Name { get; private set; } = string.Empty;

        public string Body { get; private set; } = string.Empty;

        public string Category { get; private set; } = string.Empty;

        public bool IsActive { get; private set; } = true;

        public long? CreatedByUserId { get; private set; }

        public string? CreatedByUserName { get; private set; }

        private ProposalBlock()
        {
        }

        public ProposalBlock(string name, string body, string category, long? createdByUserId, string? createdByUserName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(body);
            ArgumentException.ThrowIfNullOrWhiteSpace(category);

            Name = name.Trim();
            Body = body.Trim();
            Category = category.Trim();
            CreatedByUserId = createdByUserId;
            CreatedByUserName = Normalize(createdByUserName);
            CreatedAt = DateTimeOffset.UtcNow;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void Update(string name, string body, string category, bool isActive)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(body);
            ArgumentException.ThrowIfNullOrWhiteSpace(category);

            Name = name.Trim();
            Body = body.Trim();
            Category = category.Trim();
            IsActive = isActive;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
