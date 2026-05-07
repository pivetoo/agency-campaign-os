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

        public decimal? DefaultProbability { get; private set; }

        public int? SlaInDays { get; private set; }

        public bool IsActive { get; private set; } = true;

        private CommercialPipelineStage()
        {
        }

        public CommercialPipelineStage(string name, int displayOrder, string color, string? description = null, bool isInitial = false, bool isFinal = false, CommercialPipelineStageFinalBehavior finalBehavior = CommercialPipelineStageFinalBehavior.None, decimal? defaultProbability = null, int? slaInDays = null)
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
            DefaultProbability = NormalizeProbability(defaultProbability);
            SlaInDays = NormalizeSla(slaInDays);
            CreatedAt = DateTimeOffset.UtcNow;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void Update(string name, int displayOrder, string color, string? description, bool isInitial, bool isFinal, CommercialPipelineStageFinalBehavior finalBehavior, bool isActive, decimal? defaultProbability = null, int? slaInDays = null)
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
            DefaultProbability = NormalizeProbability(defaultProbability);
            SlaInDays = NormalizeSla(slaInDays);
            IsActive = isActive;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        private static int? NormalizeSla(int? slaInDays)
        {
            if (!slaInDays.HasValue)
            {
                return null;
            }

            if (slaInDays.Value <= 0)
            {
                return null;
            }

            return slaInDays.Value;
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static decimal? NormalizeProbability(decimal? probability)
        {
            if (!probability.HasValue)
            {
                return null;
            }

            if (probability.Value < 0m || probability.Value > 100m)
            {
                throw new ArgumentOutOfRangeException(nameof(probability), "Default probability must be between 0 and 100.");
            }

            return probability.Value;
        }
    }
}
