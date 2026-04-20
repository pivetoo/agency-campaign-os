using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using System.Linq.Expressions;

namespace AgencyCampaign.Api.Contracts.Proposals
{
    public sealed class ProposalContract
    {
        public long Id { get; init; }

        public long BrandId { get; init; }

        public string Name { get; init; } = string.Empty;

        public string? Description { get; init; }

        public ProposalStatus Status { get; init; }

        public DateTimeOffset? ValidityUntil { get; init; }

        public long? OpportunityId { get; init; }

        public decimal TotalValue { get; init; }

        public long InternalOwnerId { get; init; }

        public string? InternalOwnerName { get; init; }

        public string? Notes { get; init; }

        public long? CampaignId { get; init; }

        public BrandReferenceContract? Brand { get; init; }

        public CampaignReferenceContract? Campaign { get; init; }

        public List<ProposalItemContract> Items { get; init; } = [];

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset? UpdatedAt { get; init; }

        public static Expression<Func<Proposal, ProposalContract>> Projection => item => new ProposalContract
        {
            Id = item.Id,
            BrandId = item.BrandId,
            Name = item.Name,
            Description = item.Description,
            Status = item.Status,
            ValidityUntil = item.ValidityUntil,
            OpportunityId = item.OpportunityId,
            TotalValue = item.TotalValue,
            InternalOwnerId = item.InternalOwnerId,
            InternalOwnerName = item.InternalOwnerName,
            Notes = item.Notes,
            CampaignId = item.CampaignId,
            Brand = item.Brand == null
                ? null
                : new BrandReferenceContract
                {
                    Id = item.Brand.Id,
                    Name = item.Brand.Name
                },
            Campaign = item.Campaign == null
                ? null
                : new CampaignReferenceContract
                {
                    Id = item.Campaign.Id,
                    Name = item.Campaign.Name
                },
            Items = item.Items.ToList().Select(x => new ProposalItemContract
            {
                Id = x.Id,
                ProposalId = x.ProposalId,
                Description = x.Description,
                Quantity = x.Quantity,
                UnitPrice = x.UnitPrice,
                DeliveryDeadline = x.DeliveryDeadline,
                Status = x.Status,
                Observations = x.Observations,
                CreatorId = x.CreatorId,
                Creator = x.Creator == null
                    ? null
                    : new CreatorReferenceContract
                    {
                        Id = x.Creator.Id,
                        Name = x.Creator.Name,
                        StageName = x.Creator.StageName
                    },
                Total = x.Total
            }).ToList(),
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
    }

    public sealed class ProposalItemContract
    {
        public long Id { get; init; }

        public long ProposalId { get; init; }

        public string Description { get; init; } = string.Empty;

        public int Quantity { get; init; }

        public decimal UnitPrice { get; init; }

        public DateTimeOffset? DeliveryDeadline { get; init; }

        public ProposalItemStatus Status { get; init; }

        public string? Observations { get; init; }

        public long? CreatorId { get; init; }

        public CreatorReferenceContract? Creator { get; init; }

        public decimal Total { get; init; }

        public static Expression<Func<ProposalItem, ProposalItemContract>> Projection => item => new ProposalItemContract
        {
            Id = item.Id,
            ProposalId = item.ProposalId,
            Description = item.Description,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice,
            DeliveryDeadline = item.DeliveryDeadline,
            Status = item.Status,
            Observations = item.Observations,
            CreatorId = item.CreatorId,
            Creator = item.Creator == null
                ? null
                : new CreatorReferenceContract
                {
                    Id = item.Creator.Id,
                    Name = item.Creator.Name,
                    StageName = item.Creator.StageName
                },
            Total = item.Total
        };
    }

    public sealed class BrandReferenceContract
    {
        public long Id { get; init; }

        public string Name { get; init; } = string.Empty;
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

        public string? StageName { get; init; }
    }
}
