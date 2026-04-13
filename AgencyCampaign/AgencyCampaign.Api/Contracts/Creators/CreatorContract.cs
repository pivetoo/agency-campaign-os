using AgencyCampaign.Domain.Entities;
using System.Linq.Expressions;

namespace AgencyCampaign.Api.Contracts.Creators
{
    public sealed class CreatorContract
    {
        public long Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public string? Email { get; init; }

        public string? Phone { get; init; }

        public string? Document { get; init; }

        public string? PixKey { get; init; }

        public bool IsActive { get; init; }

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset? UpdatedAt { get; init; }

        public static Expression<Func<Creator, CreatorContract>> Projection => item => new CreatorContract
        {
            Id = item.Id,
            Name = item.Name,
            Email = item.Email,
            Phone = item.Phone,
            Document = item.Document,
            PixKey = item.PixKey,
            IsActive = item.IsActive,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
    }
}
