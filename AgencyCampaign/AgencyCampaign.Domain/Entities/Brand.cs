using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class Brand : Entity
    {
        public string Name { get; private set; } = string.Empty;

        public string? ContactName { get; private set; }

        public string? ContactEmail { get; private set; }

        public bool IsActive { get; private set; } = true;

        private Brand()
        {
        }

        public Brand(string name, string? contactName = null, string? contactEmail = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            Name = name.Trim();
            ContactName = string.IsNullOrWhiteSpace(contactName) ? null : contactName.Trim();
            ContactEmail = string.IsNullOrWhiteSpace(contactEmail) ? null : contactEmail.Trim();
        }

        public void Update(string name, string? contactName, string? contactEmail, bool isActive)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            Name = name.Trim();
            ContactName = string.IsNullOrWhiteSpace(contactName) ? null : contactName.Trim();
            ContactEmail = string.IsNullOrWhiteSpace(contactEmail) ? null : contactEmail.Trim();
            IsActive = isActive;
        }
    }
}
