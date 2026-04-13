using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using System.Linq.Expressions;

namespace AgencyCampaign.Api.Contracts.Campaigns
{
    public sealed class CampaignContract
    {
        public long Id { get; init; }

        public long BrandId { get; init; }

        public string Name { get; init; } = string.Empty;

        public string? Description { get; init; }

        public string? Objective { get; init; }

        public string? Briefing { get; init; }

        public decimal Budget { get; init; }

        public DateTimeOffset StartsAt { get; init; }

        public DateTimeOffset? EndsAt { get; init; }

        public CampaignStatus Status { get; init; }

        public string? InternalOwnerName { get; init; }

        public string? Notes { get; init; }

        public bool IsActive { get; init; }

        public BrandReferenceContract? Brand { get; init; }

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset? UpdatedAt { get; init; }

        public static Expression<Func<Campaign, CampaignContract>> Projection => item => new CampaignContract
        {
            Id = item.Id,
            BrandId = item.BrandId,
            Name = item.Name,
            Description = item.Description,
            Objective = item.Objective,
            Briefing = item.Briefing,
            Budget = item.Budget,
            StartsAt = item.StartsAt,
            EndsAt = item.EndsAt,
            Status = item.Status,
            InternalOwnerName = item.InternalOwnerName,
            Notes = item.Notes,
            IsActive = item.IsActive,
            Brand = item.Brand == null
                ? null
                : new BrandReferenceContract
                {
                    Id = item.Brand.Id,
                    Name = item.Brand.Name
                },
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
    }

    public sealed class BrandReferenceContract
    {
        public long Id { get; init; }

        public string Name { get; init; } = string.Empty;
    }
}
