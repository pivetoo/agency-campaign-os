using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class CampaignBriefing : Entity
    {
        public long CampaignId { get; private set; }

        public string? KeyMessage { get; private set; }

        public string? Dos { get; private set; }

        public string? Donts { get; private set; }

        public string? Hashtags { get; private set; }

        public string? Mentions { get; private set; }

        public string? ReferenceLinks { get; private set; }

        private CampaignBriefing()
        {
        }

        public CampaignBriefing(long campaignId, string? keyMessage, string? dos, string? donts, string? hashtags, string? mentions, string? referenceLinks)
        {
            if (campaignId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(campaignId));
            }

            CampaignId = campaignId;
            KeyMessage = Normalize(keyMessage);
            Dos = Normalize(dos);
            Donts = Normalize(donts);
            Hashtags = Normalize(hashtags);
            Mentions = Normalize(mentions);
            ReferenceLinks = Normalize(referenceLinks);
        }

        public void Update(string? keyMessage, string? dos, string? donts, string? hashtags, string? mentions, string? referenceLinks)
        {
            KeyMessage = Normalize(keyMessage);
            Dos = Normalize(dos);
            Donts = Normalize(donts);
            Hashtags = Normalize(hashtags);
            Mentions = Normalize(mentions);
            ReferenceLinks = Normalize(referenceLinks);
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
