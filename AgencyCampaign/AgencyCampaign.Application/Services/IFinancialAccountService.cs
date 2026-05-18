using AgencyCampaign.Application.Models.Financial;
using AgencyCampaign.Application.Requests.FinancialAccounts;
using Archon.Core.Pagination;

namespace AgencyCampaign.Application.Services
{
    public interface IFinancialAccountService
    {
        Task<PagedResult<FinancialAccountModel>> GetAll(PagedRequest request, string? search, bool includeInactive, CancellationToken cancellationToken = default);

        Task<FinancialAccountSummaryModel> GetSummary(CancellationToken cancellationToken = default);

        Task<FinancialAccountModel?> GetById(long id, CancellationToken cancellationToken = default);

        Task<FinancialAccountModel> Create(CreateFinancialAccountRequest request, CancellationToken cancellationToken = default);

        Task<FinancialAccountModel> Update(long id, UpdateFinancialAccountRequest request, CancellationToken cancellationToken = default);

        Task Delete(long id, CancellationToken cancellationToken = default);

        Task<FinancialAccountModel> AttachConnector(long id, long connectorId, CancellationToken cancellationToken = default);

        Task<FinancialAccountModel> DetachConnector(long id, CancellationToken cancellationToken = default);

        Task<long> TriggerSync(long id, CancellationToken cancellationToken = default);
    }
}
