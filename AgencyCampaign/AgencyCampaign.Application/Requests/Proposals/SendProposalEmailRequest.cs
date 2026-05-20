using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.Proposals
{
    public sealed class SendProposalEmailRequest
    {
        [Required]
        [EmailAddress]
        [StringLength(150)]
        public string RecipientEmail { get; set; } = string.Empty;

        [Required]
        [StringLength(200, MinimumLength = 2)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        [StringLength(5000, MinimumLength = 1)]
        public string Body { get; set; } = string.Empty;

        [Required]
        [Range(1, long.MaxValue)]
        public long ConnectorId { get; set; }

        [Required]
        [Range(1, long.MaxValue)]
        public long PipelineId { get; set; }
    }
}
