namespace AgencyCampaign.Application.Models.Creators
{
    public sealed class CreatorSocialHandleModel
    {
        public long Id { get; set; }
        public long CreatorId { get; set; }
        public long PlatformId { get; set; }
        public string PlatformName { get; set; } = string.Empty;
        public string Handle { get; set; } = string.Empty;
        public string? ProfileUrl { get; set; }
        public long? Followers { get; set; }
        public decimal? EngagementRate { get; set; }
        public bool IsPrimary { get; set; }
        public bool IsActive { get; set; }
    }
}
