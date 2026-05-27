using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class CreatorSocialHandleSnapshot : Entity
    {
        public long CreatorSocialHandleId { get; private set; }

        public CreatorSocialHandle? CreatorSocialHandle { get; private set; }

        public int Year { get; private set; }

        public int Month { get; private set; }

        public long? Followers { get; private set; }

        public decimal? EngagementRate { get; private set; }

        public string Source { get; private set; } = string.Empty;

        public DateTimeOffset CollectedAt { get; private set; }

        private CreatorSocialHandleSnapshot()
        {
        }

        public CreatorSocialHandleSnapshot(long creatorSocialHandleId, int year, int month, long? followers, decimal? engagementRate, string source)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(creatorSocialHandleId);

            CreatorSocialHandleId = creatorSocialHandleId;
            Year = year;
            Month = month;
            Followers = followers;
            EngagementRate = engagementRate;
            Source = string.IsNullOrWhiteSpace(source) ? "apify" : source.Trim();
            CollectedAt = DateTimeOffset.UtcNow;
            CreatedAt = DateTimeOffset.UtcNow;
            UpdatedAt = CreatedAt;
        }

        public void Update(long? followers, decimal? engagementRate, string source)
        {
            Followers = followers;
            EngagementRate = engagementRate;
            Source = string.IsNullOrWhiteSpace(source) ? Source : source.Trim();
            CollectedAt = DateTimeOffset.UtcNow;
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }
}
