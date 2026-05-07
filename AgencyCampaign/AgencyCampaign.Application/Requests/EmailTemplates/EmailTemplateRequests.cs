using AgencyCampaign.Domain.ValueObjects;
using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.EmailTemplates
{
    public sealed class CreateEmailTemplateRequest
    {
        [Required]
        [StringLength(150, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public EmailEventType EventType { get; set; }

        [Required]
        [StringLength(300, MinimumLength = 1)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        [MinLength(1)]
        public string HtmlBody { get; set; } = string.Empty;
    }

    public sealed class UpdateEmailTemplateRequest
    {
        [Required]
        public long Id { get; set; }

        [Required]
        [StringLength(150, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public EmailEventType EventType { get; set; }

        [Required]
        [StringLength(300, MinimumLength = 1)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        [MinLength(1)]
        public string HtmlBody { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }
}
