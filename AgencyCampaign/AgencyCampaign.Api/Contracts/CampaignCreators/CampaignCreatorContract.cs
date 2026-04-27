using AgencyCampaign.Domain.Entities;
using System.Linq.Expressions;

namespace AgencyCampaign.Api.Contracts.CampaignCreators
{
    public sealed class CampaignCreatorContract
    {
        public long Id { get; init; }

        public long CampaignId { get; init; }

        public long CreatorId { get; init; }

        public long CampaignCreatorStatusId { get; init; }

        public decimal AgreedAmount { get; init; }

        public decimal AgencyFeePercent { get; init; }

        public decimal AgencyFeeAmount { get; init; }

        public string? Notes { get; init; }

        public DateTimeOffset? ConfirmedAt { get; init; }

        public DateTimeOffset? CancelledAt { get; init; }

        public CampaignCreatorCampaignReferenceContract? Campaign { get; init; }

        public CampaignCreatorCreatorReferenceContract? Creator { get; init; }

        public CampaignCreatorStatusReferenceContract? CampaignCreatorStatus { get; init; }

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset? UpdatedAt { get; init; }

        public static Expression<Func<CampaignCreator, CampaignCreatorContract>> Projection => item => new CampaignCreatorContract
        {
            Id = item.Id,
            CampaignId = item.CampaignId,
            CreatorId = item.CreatorId,
            CampaignCreatorStatusId = item.CampaignCreatorStatusId,
            AgreedAmount = item.AgreedAmount,
            AgencyFeePercent = item.AgencyFeePercent,
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
                StageName = item.Creator.StageName,
                DefaultAgencyFeePercent = item.Creator.DefaultAgencyFeePercent
            },
            CampaignCreatorStatus = item.CampaignCreatorStatus == null ? null : new CampaignCreatorStatusReferenceContract
            {
                Id = item.CampaignCreatorStatus.Id,
                Name = item.CampaignCreatorStatus.Name,
                Color = item.CampaignCreatorStatus.Color
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

        public decimal DefaultAgencyFeePercent { get; init; }
    }

    public sealed class CampaignCreatorStatusReferenceContract
    {
        public long Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public string Color { get; init; } = "#6366f1";
    }
}
