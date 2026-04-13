using AgencyCampaign.Domain.Entities;
using System.Linq.Expressions;

namespace AgencyCampaign.Api.Contracts.Campaigns
{
    public sealed class CampaignContract
    {
        public long Id { get; init; }

        public long BrandId { get; init; }

        public string Name { get; init; } = string.Empty;

        public string? Description { get; init; }

        public decimal Budget { get; init; }

        public DateTimeOffset StartsAt { get; init; }

        public DateTimeOffset? EndsAt { get; init; }

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
            Budget = item.Budget,
            StartsAt = item.StartsAt,
            EndsAt = item.EndsAt,
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
