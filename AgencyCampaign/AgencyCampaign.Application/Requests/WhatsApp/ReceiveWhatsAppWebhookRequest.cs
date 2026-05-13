using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.WhatsApp
{
    public sealed class ReceiveWhatsAppWebhookRequest
    {
        [Required]
        public string From { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        public long Timestamp { get; set; }
    }
}
