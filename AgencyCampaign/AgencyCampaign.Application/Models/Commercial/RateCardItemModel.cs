namespace AgencyCampaign.Application.Models.Commercial
{
    public sealed class RateCardItemModel
    {
        public long Id { get; init; }

        public long CreatorId { get; init; }

        public string Label { get; init; } = string.Empty;

        public decimal UnitPrice { get; init; }

        public int DisplayOrder { get; init; }

        public bool IsActive { get; init; }
    }
}
