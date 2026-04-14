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
        public CampaignDocumentType DocumentType { get; init; }
        public string Title { get; init; } = string.Empty;
        public string? DocumentUrl { get; init; }
        public CampaignDocumentStatus Status { get; init; }
        public string? RecipientEmail { get; init; }
        public string? EmailSubject { get; init; }
        public string? EmailBody { get; init; }
        public DateTimeOffset? SentAt { get; init; }
        public DateTimeOffset? SignedAt { get; init; }
        public string? Notes { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset? UpdatedAt { get; init; }

        public static Expression<Func<CampaignDocument, CampaignDocumentContract>> Projection => item => new CampaignDocumentContract
        {
            Id = item.Id,
            CampaignId = item.CampaignId,
            CampaignCreatorId = item.CampaignCreatorId,
            DocumentType = item.DocumentType,
            Title = item.Title,
            DocumentUrl = item.DocumentUrl,
            Status = item.Status,
            RecipientEmail = item.RecipientEmail,
            EmailSubject = item.EmailSubject,
            EmailBody = item.EmailBody,
            SentAt = item.SentAt,
            SignedAt = item.SignedAt,
            Notes = item.Notes,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt,
        };
    }
}
