using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Domain.Entities;

namespace AgencyCampaign.Application.Services
{
    public interface IPolicyEvaluator
    {
        Task<PolicyEvaluationModel> EvaluateProposalAsync(Proposal proposal, CancellationToken cancellationToken = default);

        Task<PolicyEvaluationModel> EvaluateProposalByIdAsync(long proposalId, CancellationToken cancellationToken = default);
    }
}
