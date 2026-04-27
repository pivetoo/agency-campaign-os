using AgencyCampaign.Domain.Entities;
using System.Linq.Expressions;

namespace AgencyCampaign.Api.Contracts.CampaignCreatorStatuses
{
    public sealed class CampaignCreatorStatusContract
    {
        public long Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public string? Description { get; init; }

        public int DisplayOrder { get; init; }

        public string Color { get; init; } = "#6366f1";

        public bool IsInitial { get; init; }

        public bool IsFinal { get; init; }

        public int Category { get; init; }

        public bool IsActive { get; init; }

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset? UpdatedAt { get; init; }

        public static Expression<Func<CampaignCreatorStatus, CampaignCreatorStatusContract>> Projection => item => new CampaignCreatorStatusContract
        {
            Id = item.Id,
            Name = item.Name,
            Description = item.Description,
            DisplayOrder = item.DisplayOrder,
            Color = item.Color,
            IsInitial = item.IsInitial,
            IsFinal = item.IsFinal,
            Category = (int)item.Category,
            IsActive = item.IsActive,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
    }
}
