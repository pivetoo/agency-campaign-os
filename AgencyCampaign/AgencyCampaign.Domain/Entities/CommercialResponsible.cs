using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class CommercialResponsible : Entity
    {
        public long UserId { get; private set; }

        public string Name { get; private set; } = string.Empty;

        public string? Email { get; private set; }

        public string? Phone { get; private set; }

        public string? Notes { get; private set; }

        public bool IsActive { get; private set; } = true;

        private CommercialResponsible()
        {
        }

        public CommercialResponsible(long userId, string name, string? email = null, string? phone = null, string? notes = null)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(userId);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            UserId = userId;
            Name = name.Trim();
            Email = Normalize(email);
            Phone = Normalize(phone);
            Notes = Normalize(notes);
        }

        public void Update(string? notes, bool isActive)
        {
            Notes = Normalize(notes);
            IsActive = isActive;
        }

        public void RefreshFromUser(string name, string? email, string? phone)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            Name = name.Trim();
            Email = Normalize(email);
            Phone = Normalize(phone);
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
