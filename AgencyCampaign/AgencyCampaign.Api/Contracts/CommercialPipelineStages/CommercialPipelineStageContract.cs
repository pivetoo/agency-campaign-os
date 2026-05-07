using AgencyCampaign.Domain.Entities;
using System.Linq.Expressions;

namespace AgencyCampaign.Api.Contracts.CommercialPipelineStages
{
    public sealed class CommercialPipelineStageContract
    {
        public long Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public int DisplayOrder { get; init; }
        public string Color { get; init; } = "#6366f1";
        public bool IsInitial { get; init; }
        public bool IsFinal { get; init; }
        public int FinalBehavior { get; init; }
        public decimal? DefaultProbability { get; init; }
        public int? SlaInDays { get; init; }
        public bool IsActive { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset? UpdatedAt { get; init; }

        public static Expression<Func<CommercialPipelineStage, CommercialPipelineStageContract>> Projection => item => new CommercialPipelineStageContract
        {
            Id = item.Id,
            Name = item.Name,
            Description = item.Description,
            DisplayOrder = item.DisplayOrder,
            Color = item.Color,
            IsInitial = item.IsInitial,
            IsFinal = item.IsFinal,
            FinalBehavior = (int)item.FinalBehavior,
            DefaultProbability = item.DefaultProbability,
            SlaInDays = item.SlaInDays,
            IsActive = item.IsActive,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
    }
}
