using AgencyCampaign.Domain.Entities;
using System.Linq.Expressions;

namespace AgencyCampaign.Api.Contracts.Platforms
{
    public sealed class PlatformContract
    {
        public long Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? Identifier { get; init; }
        public bool IsSystem { get; init; }
        public bool IsActive { get; init; }
        public int DisplayOrder { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset? UpdatedAt { get; init; }

        public static Expression<Func<Platform, PlatformContract>> Projection => item => new PlatformContract
        {
            Id = item.Id,
            Name = item.Name,
            Identifier = item.Identifier,
            IsSystem = item.IsSystem,
            IsActive = item.IsActive,
            DisplayOrder = item.DisplayOrder,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
    }
}
