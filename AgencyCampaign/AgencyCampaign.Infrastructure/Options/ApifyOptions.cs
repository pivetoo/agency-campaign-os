namespace AgencyCampaign.Infrastructure.Options
{
    public sealed class ApifyOptions
    {
        public string? Token { get; set; }

        public List<ApifyPlatformProfile> Platforms { get; set; } = [];

        public bool JobEnabled { get; set; } = true;

        public int JobTickHours { get; set; } = 24;

        public int ButtonCooldownMinutes { get; set; } = 60;

        public int PostSyncCooldownDays { get; set; } = 7;

        public int FollowerSyncCooldownDays { get; set; } = 28;
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

        public string? ProfileActorId { get; set; }

        public string ProfileInputField { get; set; } = "usernames";

        public bool ProfileUsesHandle { get; set; }

        public bool ProfileUrlAsObject { get; set; }

        public string? FollowersField { get; set; }

        public string? ProfileEngagementField { get; set; }
    }
}
