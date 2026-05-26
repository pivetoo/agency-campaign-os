namespace AgencyCampaign.Application.Models.Dashboard
{
    public sealed class CommercialActivityItem
    {
        public string Name { get; init; } = string.Empty;

        public int Criadas { get; init; }

        public int Ganhas { get; init; }

        public int Perdidas { get; init; }
    }
}
