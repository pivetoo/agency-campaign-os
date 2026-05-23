using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Domain.Entities;

namespace AgencyCampaign.Application.Services
{
    public interface IPolicyEvaluator
    {
        Task<PolicyEvaluationModel> EvaluateNegotiationAsync(OpportunityNegotiation negotiation, CancellationToken cancellationToken = default);

        Task<PolicyEvaluationModel> EvaluateNegotiationByIdAsync(long negotiationId, CancellationToken cancellationToken = default);
    }
}
