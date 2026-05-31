using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class CommercialPolicy : Entity
    {
        public decimal? MaxDiscountPercent { get; private set; }

        public int? DefaultPaymentTermDays { get; private set; }

        public int? MaxPaymentTermDays { get; private set; }

        public string? Notes { get; private set; }

        private CommercialPolicy()
        {
        }

        public CommercialPolicy(decimal? maxDiscountPercent, int? defaultPaymentTermDays, int? maxPaymentTermDays, string? notes = null)
        {
            Apply(maxDiscountPercent, defaultPaymentTermDays, maxPaymentTermDays, notes);
            CreatedAt = DateTimeOffset.UtcNow;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void Update(decimal? maxDiscountPercent, int? defaultPaymentTermDays, int? maxPaymentTermDays, string? notes)
        {
            Apply(maxDiscountPercent, defaultPaymentTermDays, maxPaymentTermDays, notes);
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        private void Apply(decimal? maxDiscountPercent, int? defaultPaymentTermDays, int? maxPaymentTermDays, string? notes)
        {
            int? defaultDays = ClampDays(defaultPaymentTermDays);
            int? maxDays = ClampDays(maxPaymentTermDays);
            if (defaultDays.HasValue && maxDays.HasValue && defaultDays.Value > maxDays.Value)
            {
                throw new InvalidOperationException("commercialPolicy.paymentTerm.defaultExceedsMax");
            }

            MaxDiscountPercent = ClampPercent(maxDiscountPercent);
            DefaultPaymentTermDays = defaultDays;
            MaxPaymentTermDays = maxDays;
            Notes = Normalize(notes);
        }

        private static decimal? ClampPercent(decimal? value)
        {
            if (!value.HasValue) return null;
            if (value.Value < 0m) return 0m;
            if (value.Value > 100m) return 100m;
            return value.Value;
        }

        private static int? ClampDays(int? value)
        {
            if (!value.HasValue) return null;
            if (value.Value < 0) return 0;
            if (value.Value > 3650) return 3650;
            return value.Value;
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
