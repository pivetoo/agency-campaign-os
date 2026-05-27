namespace AgencyCampaign.Infrastructure.Options
{
    public sealed class ApifyOptions
    {
        public string? Token { get; set; }

        public List<ApifyPlatformProfile> Platforms { get; set; } = [];
    }

    public sealed class ApifyPlatformProfile
    {
        public string Match { get; set; } = string.Empty;

        public string ActorId { get; set; } = string.Empty;

        public string UrlField { get; set; } = "directUrls";

        public bool UrlAsObject { get; set; }

        public string? LikesField { get; set; }

        public string? CommentsField { get; set; }

        public string? ViewsField { get; set; }

        public string? SharesField { get; set; }
    }
}
