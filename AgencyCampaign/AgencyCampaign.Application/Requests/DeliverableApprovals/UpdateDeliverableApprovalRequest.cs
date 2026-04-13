using AgencyCampaign.Domain.ValueObjects;
using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.DeliverableApprovals
{
    public sealed class UpdateDeliverableApprovalRequest
    {
        [Required]
        public long Id { get; set; }

        [Required]
        [StringLength(150, MinimumLength = 2)]
        public string ReviewerName { get; set; } = string.Empty;

        [Required]
        public DeliverableApprovalStatus Status { get; set; }

        [StringLength(1000)]
        public string? Comment { get; set; }
    }
}
