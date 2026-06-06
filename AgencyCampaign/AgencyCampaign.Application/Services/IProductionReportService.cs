using AgencyCampaign.Application.Models.Production;

namespace AgencyCampaign.Application.Services
{
    public interface IProductionReportService
    {
        Task<CampaignPerformanceModel> GetCampaignPerformance(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);

        Task<CreatorPerformanceModel> GetCreatorPerformance(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);

        Task<PlatformProductionModel> GetPlatformProduction(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);
    }
}
