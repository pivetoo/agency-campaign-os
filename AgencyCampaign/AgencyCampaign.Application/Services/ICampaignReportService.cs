using AgencyCampaign.Application.Models.Campaigns;

namespace AgencyCampaign.Application.Services
{
    public interface ICampaignReportService
    {
        Task<CampaignReportLinkModel> CreateOrGetLink(long campaignId, CancellationToken cancellationToken = default);

        Task<CampaignReportModel?> GetReportByToken(string token, CancellationToken cancellationToken = default);
    }
}
