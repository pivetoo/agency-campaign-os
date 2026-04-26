using AgencyCampaign.Domain.ValueObjects;
using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class CommercialPipelineStage : Entity
    {
        public string Name { get; private set; } = string.Empty;

        public string? Description { get; private set; }

        public int DisplayOrder { get; private set; }

        public string Color { get; private set; } = "#6366f1";

        public bool IsInitial { get; private set; }

        public bool IsFinal { get; private set; }

        public CommercialPipelineStageFinalBehavior FinalBehavior { get; private set; }

        public bool IsActive { get; private set; } = true;

        private CommercialPipelineStage()
        {
        }

        public CommercialPipelineStage(string name, int displayOrder, string color, string? description = null, bool isInitial = false, bool isFinal = false, CommercialPipelineStageFinalBehavior finalBehavior = CommercialPipelineStageFinalBehavior.None)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(color);

            Name = name.Trim();
            DisplayOrder = displayOrder;
            Color = color.Trim();
            Description = Normalize(description);
            IsInitial = isInitial;
            IsFinal = isFinal;
            FinalBehavior = isFinal ? finalBehavior : CommercialPipelineStageFinalBehavior.None;
            CreatedAt = DateTimeOffset.UtcNow;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void Update(string name, int displayOrder, string color, string? description, bool isInitial, bool isFinal, CommercialPipelineStageFinalBehavior finalBehavior, bool isActive)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(color);

            Name = name.Trim();
            DisplayOrder = displayOrder;
            Color = color.Trim();
            Description = Normalize(description);
            IsInitial = isInitial;
            IsFinal = isFinal;
            FinalBehavior = isFinal ? finalBehavior : CommercialPipelineStageFinalBehavior.None;
            IsActive = isActive;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
