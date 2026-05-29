using AgencyCampaign.Domain.ValueObjects;
using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class BrandContact : Entity
    {
        public long BrandId { get; private set; }

        public BrandContactType Type { get; private set; }

        public string Value { get; private set; } = string.Empty;

        public string? Label { get; private set; }

        public bool IsPrimary { get; private set; }

        private BrandContact()
        {
        }

        public BrandContact(long brandId, BrandContactType type, string value, string? label, bool isPrimary)
        {
            if (brandId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(brandId));
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(value);

            BrandId = brandId;
            Type = type;
            Value = value.Trim();
            Label = Normalize(label);
            IsPrimary = isPrimary;
        }

        public void Update(string value, string? label)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);

            Value = value.Trim();
            Label = Normalize(label);
        }

        public void SetPrimary(bool isPrimary)
        {
            IsPrimary = isPrimary;
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
