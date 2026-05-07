using AgencyCampaign.Application.Models.Financial;
using AgencyCampaign.Application.Requests.FinancialSubcategories;

namespace AgencyCampaign.Application.Services
{
    public interface IFinancialSubcategoryService
    {
        Task<IReadOnlyCollection<FinancialSubcategoryModel>> GetAll(bool includeInactive, CancellationToken cancellationToken = default);

        Task<FinancialSubcategoryModel> Create(CreateFinancialSubcategoryRequest request, CancellationToken cancellationToken = default);

        Task<FinancialSubcategoryModel> Update(long id, UpdateFinancialSubcategoryRequest request, CancellationToken cancellationToken = default);

        Task Delete(long id, CancellationToken cancellationToken = default);
    }
}
