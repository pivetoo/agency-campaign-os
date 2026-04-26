namespace AgencyCampaign.Application.Models.Dashboard
{
    public sealed class MonthlyRevenueItem
    {
        public string Name { get; init; } = string.Empty;

        public decimal Receita { get; init; }

        public decimal Fee { get; init; }
    }
}
