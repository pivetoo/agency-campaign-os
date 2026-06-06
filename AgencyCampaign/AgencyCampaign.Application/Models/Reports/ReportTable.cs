namespace AgencyCampaign.Application.Models.Reports
{
    public sealed class ReportKpi
    {
        public string Label { get; init; } = string.Empty;
        public string Value { get; init; } = string.Empty;
    }

    public sealed class ReportTable
    {
        public string Title { get; init; } = string.Empty;
        public string? Subtitle { get; init; }
        public DateTimeOffset GeneratedAt { get; init; }
        public IReadOnlyList<ReportKpi> Kpis { get; init; } = Array.Empty<ReportKpi>();
        public IReadOnlyList<string> Columns { get; init; } = Array.Empty<string>();
        public IReadOnlyList<IReadOnlyList<string>> Rows { get; init; } = Array.Empty<IReadOnlyList<string>>();
    }
}
