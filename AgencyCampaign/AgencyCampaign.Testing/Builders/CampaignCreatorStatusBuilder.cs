using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Testing.TestSupport;
using CampaignCreatorStatus = AgencyCampaign.Domain.Entities.CampaignCreatorStatus;

namespace AgencyCampaign.Testing.Builders
{
    public sealed class CampaignCreatorStatusBuilder
    {
        private long id = 1;
        private string name = "Confirmado";
        private int displayOrder = 1;
        private string color = "#22c55e";
        private string? description;
        private bool isInitial;
        private bool isFinal;
        private CampaignCreatorStatusCategory category = CampaignCreatorStatusCategory.InProgress;
        private bool marksAsConfirmed;

        public CampaignCreatorStatusBuilder WithId(long value) { id = value; return this; }
        public CampaignCreatorStatusBuilder WithName(string value) { name = value; return this; }
        public CampaignCreatorStatusBuilder WithCategory(CampaignCreatorStatusCategory value) { category = value; return this; }
        public CampaignCreatorStatusBuilder MarksAsConfirmed() { marksAsConfirmed = true; return this; }
        public CampaignCreatorStatusBuilder AsFinal() { isFinal = true; return this; }
        public CampaignCreatorStatusBuilder AsInitial() { isInitial = true; return this; }

        public CampaignCreatorStatus Build()
        {
            CampaignCreatorStatus status = new(name, displayOrder, color, description, isInitial, isFinal, category, marksAsConfirmed);
            return status.WithId(id);
        }
    }
}
