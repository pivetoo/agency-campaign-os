using AgencyCampaign.Domain.Entities;
using System.Linq.Expressions;

namespace AgencyCampaign.Api.Contracts.Brands
{
    public sealed class BrandContract
    {
        public long Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public string? ContactName { get; init; }

        public string? ContactEmail { get; init; }

        public bool IsActive { get; init; }

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset? UpdatedAt { get; init; }

        public static Expression<Func<Brand, BrandContract>> Projection => item => new BrandContract
        {
            Id = item.Id,
            Name = item.Name,
            ContactName = item.ContactName,
            ContactEmail = item.ContactEmail,
            IsActive = item.IsActive,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
    }
}
