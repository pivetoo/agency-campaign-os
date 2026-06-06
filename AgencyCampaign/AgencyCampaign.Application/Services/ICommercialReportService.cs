using AgencyCampaign.Application.Models.Commercial;

namespace AgencyCampaign.Application.Services
{
    public interface ICommercialReportService
    {
        Task<ProposalsFunnelModel> GetProposalsFunnel(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);

        Task<BrandRankingModel> GetBrandRanking(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);
    }
}
