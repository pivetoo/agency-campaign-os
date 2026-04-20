using AgencyCampaign.Domain.ValueObjects;

namespace AgencyCampaign.Application.Requests.Opportunities
{
    public sealed class ChangeOpportunityNegotiationStatusRequest
    {
        public OpportunityNegotiationStatus Status { get; set; }
    }
}
