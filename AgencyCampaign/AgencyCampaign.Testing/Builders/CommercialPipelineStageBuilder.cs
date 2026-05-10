using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Testing.TestSupport;

namespace AgencyCampaign.Testing.Builders
{
    public sealed class CommercialPipelineStageBuilder
    {
        private long id = 1;
        private string name = "Qualificação";
        private int displayOrder = 1;
        private string color = "#6366f1";
        private string? description;
        private bool isInitial;
        private bool isFinal;
        private CommercialPipelineStageFinalBehavior finalBehavior = CommercialPipelineStageFinalBehavior.None;
        private decimal? defaultProbability;
        private int? slaInDays;
        private bool isActive = true;

        public CommercialPipelineStageBuilder WithId(long value) { id = value; return this; }
        public CommercialPipelineStageBuilder WithName(string value) { name = value; return this; }
        public CommercialPipelineStageBuilder WithDisplayOrder(int value) { displayOrder = value; return this; }
        public CommercialPipelineStageBuilder WithColor(string value) { color = value; return this; }
        public CommercialPipelineStageBuilder WithDescription(string? value) { description = value; return this; }
        public CommercialPipelineStageBuilder AsInitial() { isInitial = true; return this; }
        public CommercialPipelineStageBuilder AsFinal(CommercialPipelineStageFinalBehavior behavior)
        {
            isFinal = true;
            finalBehavior = behavior;
            return this;
        }
        public CommercialPipelineStageBuilder WithDefaultProbability(decimal? value) { defaultProbability = value; return this; }
        public CommercialPipelineStageBuilder WithSlaInDays(int? value) { slaInDays = value; return this; }
        public CommercialPipelineStageBuilder Inactive() { isActive = false; return this; }

        public CommercialPipelineStage Build()
        {
            CommercialPipelineStage stage = new(name, displayOrder, color, description, isInitial, isFinal, finalBehavior, defaultProbability, slaInDays);
            if (!isActive)
            {
                stage.Update(name, displayOrder, color, description, isInitial, isFinal, finalBehavior, false, defaultProbability, slaInDays);
            }
            return stage.WithId(id);
        }

        public static CommercialPipelineStage Default(long id = 1) => new CommercialPipelineStageBuilder().WithId(id).Build();
    }
}
