using AgencyCampaign.Domain.ValueObjects;

namespace AgencyCampaign.Application.Requests.Opportunities
{
    public sealed class ChangeOpportunityStageRequest
    {
        public OpportunityStage Stage { get; set; }
    }
}
