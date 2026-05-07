using AgencyCampaign.Domain.ValueObjects;

namespace AgencyCampaign.Application.Models.Commercial
{
    public sealed class EmailTemplateModel
    {
        public long Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public EmailEventType EventType { get; init; }

        public string Subject { get; init; } = string.Empty;

        public string HtmlBody { get; init; } = string.Empty;

        public bool IsActive { get; init; }

        public string? CreatedByUserName { get; init; }

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset? UpdatedAt { get; init; }
    }
}
