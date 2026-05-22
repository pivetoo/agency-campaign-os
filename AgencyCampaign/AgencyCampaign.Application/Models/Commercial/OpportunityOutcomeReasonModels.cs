namespace AgencyCampaign.Application.Models.Commercial
{
    public sealed class OpportunityWinReasonModel
    {
        public long Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public string Color { get; init; } = "#15803d";

        public int DisplayOrder { get; init; }

        public bool IsActive { get; init; }
    }

    public sealed class OpportunityLossReasonModel
    {
        public long Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public string Color { get; init; } = "#b91c1c";

        public int DisplayOrder { get; init; }

        public bool IsActive { get; init; }
    }
}
