using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.Proposals
{
    public sealed class SendProposalWhatsappRequest
    {
        [Required]
        [StringLength(50, MinimumLength = 8)]
        public string RecipientPhone { get; set; } = string.Empty;

        [Required]
        [StringLength(5000, MinimumLength = 1)]
        public string Body { get; set; } = string.Empty;
    }
}
