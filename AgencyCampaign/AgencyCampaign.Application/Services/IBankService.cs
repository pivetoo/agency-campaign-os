using AgencyCampaign.Application.Models.Financial;
using AgencyCampaign.Application.Requests.Banks;
using Archon.Core.Pagination;

namespace AgencyCampaign.Application.Services
{
    public interface IBankService
    {
        Task<PagedResult<BankModel>> GetAll(PagedRequest request, string? search, bool includeInactive, CancellationToken cancellationToken = default);

        Task<List<BankModel>> GetActive(CancellationToken cancellationToken = default);

        Task<BankModel?> GetById(long id, CancellationToken cancellationToken = default);

        Task<BankModel> Create(CreateBankRequest request, CancellationToken cancellationToken = default);

        Task<BankModel> Update(long id, UpdateBankRequest request, CancellationToken cancellationToken = default);

        Task Delete(long id, CancellationToken cancellationToken = default);

        Task<BankModel> SetLogo(long id, string logoUrl, CancellationToken cancellationToken = default);

        Task<BankModel> RemoveLogo(long id, CancellationToken cancellationToken = default);
    }
}
