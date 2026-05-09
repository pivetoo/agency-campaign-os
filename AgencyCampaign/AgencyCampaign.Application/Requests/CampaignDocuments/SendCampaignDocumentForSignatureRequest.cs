using AgencyCampaign.Domain.ValueObjects;
using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.CampaignDocuments
{
    public sealed class SendCampaignDocumentForSignatureRequest
    {
        [Required]
        [Range(1, long.MaxValue)]
        public long ConnectorId { get; set; }

        [Required]
        [Range(1, long.MaxValue)]
        public long PipelineId { get; set; }

        [Required]
        [MinLength(1)]
        public List<SignerInput> Signers { get; set; } = [];

        public sealed class SignerInput
        {
            [Required]
            public CampaignDocumentSignerRole Role { get; set; }

            [Required]
            [StringLength(150, MinimumLength = 2)]
            public string Name { get; set; } = string.Empty;

            [Required]
            [EmailAddress]
            [StringLength(150)]
            public string Email { get; set; } = string.Empty;

            [StringLength(50)]
            public string? DocumentNumber { get; set; }
        }
    }
}
