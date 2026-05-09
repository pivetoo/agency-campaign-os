using AgencyCampaign.Domain.ValueObjects;
using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class CampaignDocumentTemplate : Entity
    {
        public string Name { get; private set; } = string.Empty;

        public string? Description { get; private set; }

        public CampaignDocumentType DocumentType { get; private set; }

        public string Body { get; private set; } = string.Empty;

        public bool IsActive { get; private set; } = true;

        public long? CreatedByUserId { get; private set; }

        public string? CreatedByUserName { get; private set; }

        private CampaignDocumentTemplate()
        {
        }

        public CampaignDocumentTemplate(string name, CampaignDocumentType documentType, string body, string? description = null, long? createdByUserId = null, string? createdByUserName = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(body);

            Name = name.Trim();
            DocumentType = documentType;
            Body = body;
            Description = Normalize(description);
            CreatedByUserId = createdByUserId;
            CreatedByUserName = Normalize(createdByUserName);
        }

        public void Update(string name, CampaignDocumentType documentType, string body, string? description, bool isActive)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(body);

            Name = name.Trim();
            DocumentType = documentType;
            Body = body;
            Description = Normalize(description);
            IsActive = isActive;
        }

        public void Deactivate()
        {
            IsActive = false;
        }

        public void Activate()
        {
            IsActive = true;
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
