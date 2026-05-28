namespace AgencyCampaign.Infrastructure.Options
{
    public sealed class ContentLicenseOptions
    {
        public bool JobEnabled { get; set; } = true;

        public int JobTickHours { get; set; } = 24;

        public int ExpiringSoonDays { get; set; } = 30;

        public IReadOnlyList<int> AlertThresholdsDays { get; set; } = new[] { 30, 7 };
    }
}
