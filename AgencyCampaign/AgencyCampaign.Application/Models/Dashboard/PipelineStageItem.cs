namespace AgencyCampaign.Application.Models.Dashboard
{
    public sealed class PipelineStageItem
    {
        public string Name { get; init; } = string.Empty;

        public int Oportunidades { get; init; }

        public decimal Valor { get; init; }
    }
}
