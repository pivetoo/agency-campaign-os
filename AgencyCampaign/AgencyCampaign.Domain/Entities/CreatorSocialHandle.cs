using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class CreatorSocialHandle : Entity
    {
        public long CreatorId { get; private set; }

        public Creator? Creator { get; private set; }

        public long PlatformId { get; private set; }

        public Platform? Platform { get; private set; }

        public string Handle { get; private set; } = string.Empty;

        public string? ProfileUrl { get; private set; }

        public long? Followers { get; private set; }

        public decimal? EngagementRate { get; private set; }

        public bool IsPrimary { get; private set; }

        public bool IsActive { get; private set; } = true;

        private CreatorSocialHandle()
        {
        }

        public CreatorSocialHandle(long creatorId, long platformId, string handle, string? profileUrl = null, long? followers = null, decimal? engagementRate = null, bool isPrimary = false)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(creatorId);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(platformId);
            ArgumentException.ThrowIfNullOrWhiteSpace(handle);
            EnsureValid(followers, engagementRate);

            CreatorId = creatorId;
            PlatformId = platformId;
            Handle = handle.Trim();
            ProfileUrl = Normalize(profileUrl);
            Followers = followers;
            EngagementRate = engagementRate;
            IsPrimary = isPrimary;
        }

        public void Update(long platformId, string handle, string? profileUrl, long? followers, decimal? engagementRate, bool isPrimary, bool isActive)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(platformId);
            ArgumentException.ThrowIfNullOrWhiteSpace(handle);
            EnsureValid(followers, engagementRate);

            PlatformId = platformId;
            Handle = handle.Trim();
            ProfileUrl = Normalize(profileUrl);
            Followers = followers;
            EngagementRate = engagementRate;
            IsPrimary = isPrimary;
            IsActive = isActive;
        }

        private static void EnsureValid(long? followers, decimal? engagementRate)
        {
            if (followers.HasValue && followers.Value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(followers), "Followers cannot be negative.");
            }

            if (engagementRate.HasValue && (engagementRate.Value < 0 || engagementRate.Value > 100))
            {
                throw new ArgumentOutOfRangeException(nameof(engagementRate), "Engagement rate must be between 0 and 100.");
            }
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
