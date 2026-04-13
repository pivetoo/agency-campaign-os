using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using System.Linq.Expressions;

namespace AgencyCampaign.Api.Contracts.CampaignDeliverables
{
    public sealed class CampaignDeliverableContract
    {
        public long Id { get; init; }

        public long CampaignId { get; init; }

        public long CreatorId { get; init; }

        public string Title { get; init; } = string.Empty;

        public string? Description { get; init; }

        public DateTimeOffset DueAt { get; init; }

        public DateTimeOffset? PublishedAt { get; init; }

        public DeliverableStatus Status { get; init; }

        public decimal GrossAmount { get; init; }

        public decimal CreatorAmount { get; init; }

        public decimal AgencyFeeAmount { get; init; }

        public CampaignReferenceContract? Campaign { get; init; }

        public CreatorReferenceContract? Creator { get; init; }

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset? UpdatedAt { get; init; }

        public static Expression<Func<CampaignDeliverable, CampaignDeliverableContract>> Projection => item => new CampaignDeliverableContract
        {
            Id = item.Id,
            CampaignId = item.CampaignId,
            CreatorId = item.CreatorId,
            Title = item.Title,
            Description = item.Description,
            DueAt = item.DueAt,
            PublishedAt = item.PublishedAt,
            Status = item.Status,
            GrossAmount = item.GrossAmount,
            CreatorAmount = item.CreatorAmount,
            AgencyFeeAmount = item.AgencyFeeAmount,
            Campaign = item.Campaign == null
                ? null
                : new CampaignReferenceContract
                {
                    Id = item.Campaign.Id,
                    Name = item.Campaign.Name
                },
            Creator = item.Creator == null
                ? null
                : new CreatorReferenceContract
                {
                    Id = item.Creator.Id,
                    Name = item.Creator.Name
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

    public sealed class CreatorReferenceContract
    {
        public long Id { get; init; }

        public string Name { get; init; } = string.Empty;
    }
}
