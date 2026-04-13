using AgencyCampaign.Domain.ValueObjects;
using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.DeliverableApprovals
{
    public sealed class CreateDeliverableApprovalRequest
    {
        [Required]
        [Range(1, long.MaxValue)]
        public long CampaignDeliverableId { get; set; }

        [Required]
        public DeliverableApprovalType ApprovalType { get; set; }

        [Required]
        [StringLength(150, MinimumLength = 2)]
        public string ReviewerName { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Comment { get; set; }
    }
}
