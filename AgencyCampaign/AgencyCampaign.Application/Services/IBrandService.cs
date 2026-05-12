using AgencyCampaign.Application.Requests.Brands;
using AgencyCampaign.Domain.Entities;
using Archon.Application.Services;
using Archon.Core.Pagination;

namespace AgencyCampaign.Application.Services
{
    public interface IBrandService : ICrudService<Brand>
    {
        Task<PagedResult<Brand>> GetBrands(PagedRequest request, string? search, CancellationToken cancellationToken = default);

        IAsyncEnumerable<string> ExportAsync(CancellationToken cancellationToken = default);

        Task<Brand?> GetBrandById(long id, CancellationToken cancellationToken = default);

        Task<Brand> CreateBrand(CreateBrandRequest request, CancellationToken cancellationToken = default);

        Task<Brand> UpdateBrand(long id, UpdateBrandRequest request, CancellationToken cancellationToken = default);

        Task<Brand> SetBrandLogo(long id, string logoUrl, CancellationToken cancellationToken = default);

        Task<Brand> RemoveBrandLogo(long id, CancellationToken cancellationToken = default);
    }
}
