using AgencyCampaign.Domain.Entities;
using System.Linq.Expressions;

namespace AgencyCampaign.Api.Contracts.DeliverableKinds
{
    public sealed class DeliverableKindContract
    {
        public long Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public bool IsActive { get; init; }
        public int DisplayOrder { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset? UpdatedAt { get; init; }

        public static Expression<Func<DeliverableKind, DeliverableKindContract>> Projection => item => new DeliverableKindContract
        {
            Id = item.Id,
            Name = item.Name,
            IsActive = item.IsActive,
            DisplayOrder = item.DisplayOrder,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
    }
}
