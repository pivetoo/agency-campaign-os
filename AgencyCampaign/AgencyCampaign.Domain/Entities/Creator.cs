using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class Creator : Entity
    {
        public string Name { get; private set; } = string.Empty;

        public string? StageName { get; private set; }

        public string? Email { get; private set; }

        public string? Phone { get; private set; }

        public string? Document { get; private set; }

        public string? PixKey { get; private set; }

        public string? PrimaryNiche { get; private set; }

        public string? City { get; private set; }

        public string? State { get; private set; }

        public string? Notes { get; private set; }

        public decimal DefaultAgencyFeePercent { get; private set; }

        public bool IsActive { get; private set; } = true;

        private Creator()
        {
        }

        public Creator(string name, string? stageName = null, string? email = null, string? phone = null, string? document = null, string? pixKey = null, string? primaryNiche = null, string? city = null, string? state = null, string? notes = null, decimal defaultAgencyFeePercent = 0)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentOutOfRangeException.ThrowIfNegative(defaultAgencyFeePercent);

            Name = name.Trim();
            StageName = Normalize(stageName);
            Email = Normalize(email);
            Phone = Normalize(phone);
            Document = Normalize(document);
            PixKey = Normalize(pixKey);
            PrimaryNiche = Normalize(primaryNiche);
            City = Normalize(city);
            State = Normalize(state);
            Notes = Normalize(notes);
            DefaultAgencyFeePercent = defaultAgencyFeePercent;
        }

        public void Update(string name, string? stageName, string? email, string? phone, string? document, string? pixKey, string? primaryNiche, string? city, string? state, string? notes, decimal defaultAgencyFeePercent, bool isActive)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentOutOfRangeException.ThrowIfNegative(defaultAgencyFeePercent);

            Name = name.Trim();
            StageName = Normalize(stageName);
            Email = Normalize(email);
            Phone = Normalize(phone);
            Document = Normalize(document);
            PixKey = Normalize(pixKey);
            PrimaryNiche = Normalize(primaryNiche);
            City = Normalize(city);
            State = Normalize(state);
            Notes = Normalize(notes);
            DefaultAgencyFeePercent = defaultAgencyFeePercent;
            IsActive = isActive;
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
