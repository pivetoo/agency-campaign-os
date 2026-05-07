namespace AgencyCampaign.Application.Models.Commercial
{
    public sealed class OpportunitySourceModel
    {
        public long Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public string Color { get; init; } = "#6366f1";

        public int DisplayOrder { get; init; }

        public bool IsActive { get; init; }
    }

    public sealed class OpportunityTagModel
    {
        public long Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public string Color { get; init; } = "#6366f1";

        public bool IsActive { get; init; }
    }
}
