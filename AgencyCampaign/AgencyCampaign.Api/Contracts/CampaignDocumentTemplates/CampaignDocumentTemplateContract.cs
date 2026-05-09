using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using System.Linq.Expressions;

namespace AgencyCampaign.Api.Contracts.CampaignDocumentTemplates
{
    public sealed class CampaignDocumentTemplateContract
    {
        public long Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public CampaignDocumentType DocumentType { get; init; }
        public string Body { get; init; } = string.Empty;
        public bool IsActive { get; init; }
        public long? CreatedByUserId { get; init; }
        public string? CreatedByUserName { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset? UpdatedAt { get; init; }

        public static Expression<Func<CampaignDocumentTemplate, CampaignDocumentTemplateContract>> Projection => item => new CampaignDocumentTemplateContract
        {
            Id = item.Id,
            Name = item.Name,
            Description = item.Description,
            DocumentType = item.DocumentType,
            Body = item.Body,
            IsActive = item.IsActive,
            CreatedByUserId = item.CreatedByUserId,
            CreatedByUserName = item.CreatedByUserName,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
    }
}
