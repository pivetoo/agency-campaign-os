namespace AgencyCampaign.Application.Services
{
    public interface IApifySocialMetricsClient
    {
        bool IsConfigured { get; }

        Task<SocialMetricsResult?> FetchAsync(string platformName, string url, CancellationToken cancellationToken = default);

        Task<SocialProfileResult?> FetchProfileAsync(string platformName, string? handle, string? profileUrl, CancellationToken cancellationToken = default);
    }

    public sealed class SocialProfileResult
    {
        public long? Followers { get; init; }

        public decimal? EngagementRate { get; init; }
    }

    public sealed class SocialMetricsResult
    {
        public int? Likes { get; init; }

        public int? Comments { get; init; }

        public long? Views { get; init; }

        public int? Shares { get; init; }
    }
}
