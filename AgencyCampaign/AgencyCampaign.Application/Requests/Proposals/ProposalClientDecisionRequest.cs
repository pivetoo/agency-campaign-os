using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.Proposals
{
    public sealed class ProposalClientDecisionRequest
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Email { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }
    }
}
