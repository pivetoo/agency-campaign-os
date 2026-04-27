using AgencyCampaign.Domain.Entities;
using System.Linq.Expressions;

namespace AgencyCampaign.Api.Contracts.Integrations
{
    public sealed class IntegrationContract
    {
        public long Id { get; init; }

        public string Identifier { get; init; } = string.Empty;

        public string Name { get; init; } = string.Empty;

        public string? Description { get; init; }

        public long CategoryId { get; init; }

        public bool IsActive { get; init; }

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset? UpdatedAt { get; init; }

        public static Expression<Func<Integration, IntegrationContract>> Projection => item => new IntegrationContract
        {
            Id = item.Id,
            Identifier = item.Identifier,
            Name = item.Name,
            Description = item.Description,
            CategoryId = item.CategoryId,
            IsActive = item.IsActive,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
    }
}
