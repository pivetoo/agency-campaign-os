using AgencyCampaign.Application.Models.Financial;

namespace AgencyCampaign.Application.Services
{
    public interface IFinancialPeriodService
    {
        Task<IReadOnlyList<FinancialPeriodModel>> GetRecentPeriods(int months, CancellationToken cancellationToken = default);

        Task<FinancialPeriodModel> Close(int year, int month, CancellationToken cancellationToken = default);

        Task<FinancialPeriodModel> Reopen(int year, int month, CancellationToken cancellationToken = default);
    }
}
