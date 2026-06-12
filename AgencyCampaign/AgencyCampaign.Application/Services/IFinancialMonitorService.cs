using AgencyCampaign.Application.Models.Financial;

namespace AgencyCampaign.Application.Services
{
    public interface IFinancialMonitorService
    {
        Task<FinancialMonitorModel> GetMonitor(CancellationToken cancellationToken = default);
    }
}
