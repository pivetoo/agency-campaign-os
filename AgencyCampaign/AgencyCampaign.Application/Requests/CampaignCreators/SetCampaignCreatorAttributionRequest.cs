using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.CampaignCreators
{
    public sealed class SetCampaignCreatorAttributionRequest
    {
        [StringLength(100)]
        public string? CouponCode { get; set; }

        [StringLength(1000)]
        public string? TrackingUrl { get; set; }

        [Range(0, int.MaxValue)]
        public int? AttributedOrders { get; set; }

        [Range(0, 9999999999.99)]
        public decimal? AttributedRevenue { get; set; }
    }
}
