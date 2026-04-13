using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using System.Linq.Expressions;

namespace AgencyCampaign.Api.Contracts.CampaignCreators
{
    public sealed class CampaignCreatorContract
    {
        public long Id { get; init; }

        public long CampaignId { get; init; }

        public long CreatorId { get; init; }

        public CampaignCreatorStatus Status { get; init; }

        public decimal AgreedAmount { get; init; }

        public decimal AgencyFeeAmount { get; init; }

        public string? Notes { get; init; }

        public DateTimeOffset? ConfirmedAt { get; init; }

        public DateTimeOffset? CancelledAt { get; init; }

        public CampaignCreatorCampaignReferenceContract? Campaign { get; init; }

        public CampaignCreatorCreatorReferenceContract? Creator { get; init; }

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset? UpdatedAt { get; init; }

        public static Expression<Func<CampaignCreator, CampaignCreatorContract>> Projection => item => new CampaignCreatorContract
        {
            Id = item.Id,
            CampaignId = item.CampaignId,
            CreatorId = item.CreatorId,
            Status = item.Status,
            AgreedAmount = item.AgreedAmount,
            AgencyFeeAmount = item.AgencyFeeAmount,
            Notes = item.Notes,
            ConfirmedAt = item.ConfirmedAt,
            CancelledAt = item.CancelledAt,
            Campaign = item.Campaign == null ? null : new CampaignCreatorCampaignReferenceContract
            {
                Id = item.Campaign.Id,
                Name = item.Campaign.Name
            },
            Creator = item.Creator == null ? null : new CampaignCreatorCreatorReferenceContract
            {
                Id = item.Creator.Id,
                Name = item.Creator.Name,
                StageName = item.Creator.StageName
            },
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
    }

    public sealed class CampaignCreatorCampaignReferenceContract
    {
        public long Id { get; init; }

        public string Name { get; init; } = string.Empty;
    }

    public sealed class CampaignCreatorCreatorReferenceContract
    {
        public long Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public string? StageName { get; init; }
    }
}
