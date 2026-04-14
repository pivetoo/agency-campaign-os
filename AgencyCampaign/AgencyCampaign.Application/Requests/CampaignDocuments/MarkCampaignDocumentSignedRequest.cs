using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.CampaignDocuments
{
    public sealed class MarkCampaignDocumentSignedRequest
    {
        [Required]
        public DateTimeOffset SignedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
