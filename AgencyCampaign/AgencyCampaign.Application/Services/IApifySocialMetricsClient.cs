namespace AgencyCampaign.Application.Services
{
    public interface IApifySocialMetricsClient
    {
        bool IsConfigured { get; }

        Task<SocialMetricsResult?> FetchAsync(string platformName, string url, CancellationToken cancellationToken = default);
    }

    public sealed class SocialMetricsResult
    {
        public int? Likes { get; init; }

        public int? Comments { get; init; }

        public long? Views { get; init; }

        public int? Shares { get; init; }
    }
}
