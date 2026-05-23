using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class CommercialPolicy : Entity
    {
        public decimal? MaxDiscountPercent { get; private set; }

        public decimal? MinMarginPercent { get; private set; }

        public int? DefaultPaymentTermDays { get; private set; }

        public int? MaxPaymentTermDays { get; private set; }

        public string? Notes { get; private set; }

        private CommercialPolicy()
        {
        }

        public CommercialPolicy(decimal? maxDiscountPercent, decimal? minMarginPercent, int? defaultPaymentTermDays, int? maxPaymentTermDays, string? notes = null)
        {
            MaxDiscountPercent = ClampPercent(maxDiscountPercent);
            MinMarginPercent = ClampPercent(minMarginPercent);
            DefaultPaymentTermDays = ClampDays(defaultPaymentTermDays);
            MaxPaymentTermDays = ClampDays(maxPaymentTermDays);
            Notes = Normalize(notes);
            CreatedAt = DateTimeOffset.UtcNow;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void Update(decimal? maxDiscountPercent, decimal? minMarginPercent, int? defaultPaymentTermDays, int? maxPaymentTermDays, string? notes)
        {
            MaxDiscountPercent = ClampPercent(maxDiscountPercent);
            MinMarginPercent = ClampPercent(minMarginPercent);
            DefaultPaymentTermDays = ClampDays(defaultPaymentTermDays);
            MaxPaymentTermDays = ClampDays(maxPaymentTermDays);
            Notes = Normalize(notes);
            UpdatedAt = DateTimeOffset.UtcNow;
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
