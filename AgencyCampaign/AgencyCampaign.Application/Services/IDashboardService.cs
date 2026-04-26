using AgencyCampaign.Application.Models.Dashboard;

namespace AgencyCampaign.Application.Services
{
    public interface IDashboardService
    {
        Task<DashboardChartsModel> GetChartsData(CancellationToken cancellationToken = default);
    }
}
