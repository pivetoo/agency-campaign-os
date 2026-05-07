using AgencyCampaign.Application.Models.Financial;
using AgencyCampaign.Application.Requests.FinancialAccounts;

namespace AgencyCampaign.Application.Services
{
    public interface IFinancialAccountService
    {
        Task<IReadOnlyCollection<FinancialAccountModel>> GetAll(bool includeInactive, CancellationToken cancellationToken = default);

        Task<FinancialAccountModel?> GetById(long id, CancellationToken cancellationToken = default);

        Task<FinancialAccountModel> Create(CreateFinancialAccountRequest request, CancellationToken cancellationToken = default);

        Task<FinancialAccountModel> Update(long id, UpdateFinancialAccountRequest request, CancellationToken cancellationToken = default);

        Task Delete(long id, CancellationToken cancellationToken = default);
    }
}
