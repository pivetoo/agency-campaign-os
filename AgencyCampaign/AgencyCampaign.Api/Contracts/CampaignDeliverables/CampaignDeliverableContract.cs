using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using System.Linq.Expressions;

namespace AgencyCampaign.Api.Contracts.CampaignDeliverables
{
    public sealed class CampaignDeliverableContract
    {
        public long Id { get; init; }

        public long CampaignId { get; init; }

        public long CampaignCreatorId { get; init; }

        public string Title { get; init; } = string.Empty;

        public string? Description { get; init; }

        public DeliverableType Type { get; init; }

        public SocialPlatform Platform { get; init; }

        public DateTimeOffset DueAt { get; init; }

        public DateTimeOffset? PublishedAt { get; init; }

        public string? PublishedUrl { get; init; }

        public string? EvidenceUrl { get; init; }

        public DeliverableStatus Status { get; init; }

        public decimal GrossAmount { get; init; }

        public decimal CreatorAmount { get; init; }

        public decimal AgencyFeeAmount { get; init; }

        public string? Notes { get; init; }

        public CampaignReferenceContract? Campaign { get; init; }

        public CampaignCreatorReferenceContract? CampaignCreator { get; init; }

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset? UpdatedAt { get; init; }

        public static Expression<Func<CampaignDeliverable, CampaignDeliverableContract>> Projection => item => new CampaignDeliverableContract
        {
            Id = item.Id,
            CampaignId = item.CampaignId,
            CampaignCreatorId = item.CampaignCreatorId,
            Title = item.Title,
            Description = item.Description,
            Type = item.Type,
            Platform = item.Platform,
            DueAt = item.DueAt,
            PublishedAt = item.PublishedAt,
            PublishedUrl = item.PublishedUrl,
            EvidenceUrl = item.EvidenceUrl,
            Status = item.Status,
            GrossAmount = item.GrossAmount,
            CreatorAmount = item.CreatorAmount,
            AgencyFeeAmount = item.AgencyFeeAmount,
            Notes = item.Notes,
            Campaign = item.Campaign == null
                ? null
                : new CampaignReferenceContract
                {
                    Id = item.Campaign.Id,
                    Name = item.Campaign.Name
                },
            CampaignCreator = item.CampaignCreator == null
                ? null
                : new CampaignCreatorReferenceContract
                {
                    Id = item.CampaignCreator.Id,
                    CreatorId = item.CampaignCreator.CreatorId,
                    CreatorName = item.CampaignCreator.Creator == null ? string.Empty : item.CampaignCreator.Creator.Name,
                    StageName = item.CampaignCreator.Creator == null ? null : item.CampaignCreator.Creator.StageName
                },
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
    }

    public sealed class CampaignReferenceContract
    {
        public long Id { get; init; }

        public string Name { get; init; } = string.Empty;
    }

    public sealed class CampaignCreatorReferenceContract
    {
        public long Id { get; init; }

        public long CreatorId { get; init; }

        public string CreatorName { get; init; } = string.Empty;

        public string? StageName { get; init; }
    }
}
