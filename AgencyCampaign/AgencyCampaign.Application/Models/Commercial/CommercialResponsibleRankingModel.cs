namespace AgencyCampaign.Application.Models.Commercial
{
    public sealed class CommercialResponsibleRankingModel
    {
        public long? CommercialResponsibleId { get; init; }

        public string Name { get; init; } = string.Empty;

        public int OpenOpportunities { get; init; }

        public decimal OpenValue { get; init; }

        public int WonOpportunities { get; init; }

        public decimal WonValue { get; init; }

        public int LostOpportunities { get; init; }

        public decimal WinRate { get; init; }
    }
}
