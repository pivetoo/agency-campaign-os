using AgencyCampaign.Domain.ValueObjects;
using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.Opportunities
{
    public sealed class CreateOpportunityApprovalRequest
    {
        [Required]
        [Range(1, long.MaxValue)]
        public long OpportunityNegotiationId { get; set; }

        [Required]
        public OpportunityApprovalType ApprovalType { get; set; }

        [Required]
        [StringLength(1000, MinimumLength = 2)]
        public string Reason { get; set; } = string.Empty;

        [Range(1, long.MaxValue)]
        public long? RequestedByUserId { get; set; }

        [Required]
        [StringLength(150, MinimumLength = 2)]
        public string RequestedByUserName { get; set; } = string.Empty;
    }
}
