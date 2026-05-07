using AgencyCampaign.Application.Models.Dashboard;

namespace AgencyCampaign.Application.Services
{
    public interface IDashboardService
    {
        Task<DashboardOverviewModel> GetOverview(CancellationToken cancellationToken = default);
    }
}
