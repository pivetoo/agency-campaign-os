using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.CampaignBriefings
{
    public sealed class UpsertCampaignBriefingRequest
    {
        [StringLength(2000)]
        public string? KeyMessage { get; set; }

        [StringLength(4000)]
        public string? Dos { get; set; }

        [StringLength(4000)]
        public string? Donts { get; set; }

        [StringLength(1000)]
        public string? Hashtags { get; set; }

        [StringLength(1000)]
        public string? Mentions { get; set; }

        [StringLength(2000)]
        public string? ReferenceLinks { get; set; }
    }
}
