namespace AgencyCampaign.Application.Models.Commercial
{
    public sealed class CommercialPolicyModel
    {
        public long Id { get; init; }

        public decimal? MaxDiscountPercent { get; init; }

        public decimal? MinMarginPercent { get; init; }

        public int? DefaultPaymentTermDays { get; init; }

        public int? MaxPaymentTermDays { get; init; }

        public string? Notes { get; init; }

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset? UpdatedAt { get; init; }
    }
}
