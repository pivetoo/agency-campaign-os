using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class Creator : Entity
    {
        public string Name { get; private set; } = string.Empty;

        public string? Email { get; private set; }

        public string? Phone { get; private set; }

        public string? Document { get; private set; }

        public string? PixKey { get; private set; }

        public bool IsActive { get; private set; } = true;

        private Creator()
        {
        }

        public Creator(string name, string? email = null, string? phone = null, string? document = null, string? pixKey = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            Name = name.Trim();
            Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
            Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
            Document = string.IsNullOrWhiteSpace(document) ? null : document.Trim();
            PixKey = string.IsNullOrWhiteSpace(pixKey) ? null : pixKey.Trim();
        }

        public void Update(string name, string? email, string? phone, string? document, string? pixKey, bool isActive)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            Name = name.Trim();
            Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
            Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
            Document = string.IsNullOrWhiteSpace(document) ? null : document.Trim();
            PixKey = string.IsNullOrWhiteSpace(pixKey) ? null : pixKey.Trim();
            IsActive = isActive;
        }
    }
}
