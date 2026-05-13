using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.WhatsApp
{
    public sealed class SendWhatsAppMessageRequest
    {
        [Required]
        [StringLength(4096)]
        public string Message { get; set; } = string.Empty;
    }
}
