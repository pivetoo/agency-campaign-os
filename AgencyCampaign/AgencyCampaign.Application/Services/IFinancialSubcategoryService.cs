using AgencyCampaign.Application.Models.Financial;
using AgencyCampaign.Application.Requests.FinancialSubcategories;
using Archon.Core.Pagination;

namespace AgencyCampaign.Application.Services
{
    public interface IFinancialSubcategoryService
    {
        Task<PagedResult<FinancialSubcategoryModel>> GetAll(PagedRequest request, string? search, bool includeInactive, CancellationToken cancellationToken = default);

        Task<FinancialSubcategoryModel> Create(CreateFinancialSubcategoryRequest request, CancellationToken cancellationToken = default);

        Task<FinancialSubcategoryModel> Update(long id, UpdateFinancialSubcategoryRequest request, CancellationToken cancellationToken = default);

        Task Delete(long id, CancellationToken cancellationToken = default);
    }
}
