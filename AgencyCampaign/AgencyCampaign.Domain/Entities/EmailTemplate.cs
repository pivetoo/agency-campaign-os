using AgencyCampaign.Domain.ValueObjects;
using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class EmailTemplate : Entity
    {
        public string Name { get; private set; } = string.Empty;

        public EmailEventType EventType { get; private set; }

        public string Subject { get; private set; } = string.Empty;

        public string HtmlBody { get; private set; } = string.Empty;

        public bool IsActive { get; private set; } = true;

        public long? CreatedByUserId { get; private set; }

        public string? CreatedByUserName { get; private set; }

        private EmailTemplate()
        {
        }

        public EmailTemplate(string name, EmailEventType eventType, string subject, string htmlBody, long? createdByUserId, string? createdByUserName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(subject);
            ArgumentException.ThrowIfNullOrWhiteSpace(htmlBody);

            Name = name.Trim();
            EventType = eventType;
            Subject = subject.Trim();
            HtmlBody = htmlBody;
            CreatedByUserId = createdByUserId;
            CreatedByUserName = Normalize(createdByUserName);
            CreatedAt = DateTimeOffset.UtcNow;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void Update(string name, EmailEventType eventType, string subject, string htmlBody, bool isActive)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(subject);
            ArgumentException.ThrowIfNullOrWhiteSpace(htmlBody);

            Name = name.Trim();
            EventType = eventType;
            Subject = subject.Trim();
            HtmlBody = htmlBody;
            IsActive = isActive;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
