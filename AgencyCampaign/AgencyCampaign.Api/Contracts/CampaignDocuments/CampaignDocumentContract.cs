using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using System.Linq.Expressions;

namespace AgencyCampaign.Api.Contracts.CampaignDocuments
{
    public sealed class CampaignDocumentContract
    {
        public long Id { get; init; }
        public long CampaignId { get; init; }
        public long? CampaignCreatorId { get; init; }
        public long? TemplateId { get; init; }
        public string? TemplateName { get; init; }
        public CampaignDocumentType DocumentType { get; init; }
        public string Title { get; init; } = string.Empty;
        public string? DocumentUrl { get; init; }
        public string? Body { get; init; }
        public string? Provider { get; init; }
        public string? ProviderDocumentId { get; init; }
        public string? SignedDocumentUrl { get; init; }
        public CampaignDocumentStatus Status { get; init; }
        public string? RecipientEmail { get; init; }
        public string? EmailSubject { get; init; }
        public string? EmailBody { get; init; }
        public DateTimeOffset? SentAt { get; init; }
        public DateTimeOffset? SignedAt { get; init; }
        public string? Notes { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset? UpdatedAt { get; init; }
        public IReadOnlyCollection<CampaignDocumentSignatureContract> Signatures { get; init; } = [];
        public IReadOnlyCollection<CampaignDocumentEventContract> Events { get; init; } = [];

        public static Expression<Func<CampaignDocument, CampaignDocumentContract>> Projection => item => new CampaignDocumentContract
        {
            Id = item.Id,
            CampaignId = item.CampaignId,
            CampaignCreatorId = item.CampaignCreatorId,
            TemplateId = item.TemplateId,
            TemplateName = item.Template != null ? item.Template.Name : null,
            DocumentType = item.DocumentType,
            Title = item.Title,
            DocumentUrl = item.DocumentUrl,
            Body = item.Body,
            Provider = item.Provider,
            ProviderDocumentId = item.ProviderDocumentId,
            SignedDocumentUrl = item.SignedDocumentUrl,
            Status = item.Status,
            RecipientEmail = item.RecipientEmail,
            EmailSubject = item.EmailSubject,
            EmailBody = item.EmailBody,
            SentAt = item.SentAt,
            SignedAt = item.SignedAt,
            Notes = item.Notes,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt,
            Signatures = item.Signatures.Select(s => new CampaignDocumentSignatureContract
            {
                Id = s.Id,
                Role = s.Role,
                SignerName = s.SignerName,
                SignerEmail = s.SignerEmail,
                SignerDocumentNumber = s.SignerDocumentNumber,
                ProviderSignerId = s.ProviderSignerId,
                SignedAt = s.SignedAt,
                IpAddress = s.IpAddress,
                IsSigned = s.SignedAt != null,
            }).ToList(),
            Events = item.Events.Select(e => new CampaignDocumentEventContract
            {
                Id = e.Id,
                EventType = e.EventType,
                OccurredAt = e.OccurredAt,
                Description = e.Description,
                Metadata = e.Metadata,
            }).ToList(),
        };
    }
}
