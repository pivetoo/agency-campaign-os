using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.CreatorPortal
{
    public sealed class SubmitDeliverableInsightsRequest
    {
        [Range(0, long.MaxValue)]
        public long? Reach { get; set; }

        [Range(0, long.MaxValue)]
        public long? Impressions { get; set; }

        [Range(0, int.MaxValue)]
        public int? Saves { get; set; }
    }
}
