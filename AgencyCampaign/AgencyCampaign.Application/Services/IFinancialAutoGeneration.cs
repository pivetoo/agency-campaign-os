using AgencyCampaign.Domain.Entities;

namespace AgencyCampaign.Application.Services
{
    public interface IFinancialAutoGeneration
    {
        Task GenerateForConvertedProposal(Proposal proposal, long campaignId, CancellationToken cancellationToken = default);

        Task GenerateForPublishedDeliverable(CampaignDeliverable deliverable, CancellationToken cancellationToken = default);
    }
}
