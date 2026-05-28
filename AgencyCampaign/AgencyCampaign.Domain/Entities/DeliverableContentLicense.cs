using AgencyCampaign.Domain.ValueObjects;
using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class DeliverableContentLicense : Entity
    {
        public long CampaignDeliverableId { get; private set; }
        public ContentLicenseType Type { get; private set; }
        public string? Channels { get; private set; }
        public DateTimeOffset? StartsAt { get; private set; }
        public DateTimeOffset? ExpiresAt { get; private set; }
        public decimal? Value { get; private set; }
        public string? Notes { get; private set; }
        public long? CampaignDocumentId { get; private set; }
        public int? LastAlertedThresholdDays { get; private set; }

        private DeliverableContentLicense()
        {
        }

        public DeliverableContentLicense(long campaignDeliverableId, ContentLicenseType type, string? channels, DateTimeOffset? startsAt, DateTimeOffset? expiresAt, decimal? value, string? notes, long? campaignDocumentId)
        {
            if (campaignDeliverableId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(campaignDeliverableId));
            }

            if (value.HasValue && value.Value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            CampaignDeliverableId = campaignDeliverableId;
            Type = type;
            Channels = Normalize(channels);
            StartsAt = startsAt?.ToUniversalTime();
            ExpiresAt = expiresAt?.ToUniversalTime();
            Value = value;
            Notes = Normalize(notes);
            CampaignDocumentId = campaignDocumentId;
        }

        public void Update(ContentLicenseType type, string? channels, DateTimeOffset? startsAt, DateTimeOffset? expiresAt, decimal? value, string? notes, long? campaignDocumentId)
        {
            if (value.HasValue && value.Value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            DateTimeOffset? newExpiresAt = expiresAt?.ToUniversalTime();
            if (ExpiresAt != newExpiresAt)
            {
                LastAlertedThresholdDays = null;
            }

            Type = type;
            Channels = Normalize(channels);
            StartsAt = startsAt?.ToUniversalTime();
            ExpiresAt = newExpiresAt;
            Value = value;
            Notes = Normalize(notes);
            CampaignDocumentId = campaignDocumentId;
        }

        public bool IsExpired(DateTimeOffset now)
        {
            return ExpiresAt.HasValue && ExpiresAt.Value <= now;
        }

        public int? DaysUntilExpiry(DateTimeOffset now)
        {
            if (!ExpiresAt.HasValue)
            {
                return null;
            }

            double days = Math.Ceiling((ExpiresAt.Value - now).TotalDays);
            return days < 0 ? 0 : (int)days;
        }

        public ContentLicenseStatus ComputeStatus(DateTimeOffset now, int expiringSoonDays)
        {
            if (!ExpiresAt.HasValue)
            {
                return ContentLicenseStatus.Active;
            }

            if (IsExpired(now))
            {
                return ContentLicenseStatus.Expired;
            }

            int? days = DaysUntilExpiry(now);
            if (days.HasValue && days.Value <= expiringSoonDays)
            {
                return ContentLicenseStatus.ExpiringSoon;
            }

            return ContentLicenseStatus.Active;
        }

        public void MarkAlerted(int thresholdDays)
        {
            LastAlertedThresholdDays = thresholdDays;
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
