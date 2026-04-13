using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.Brands;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using Archon.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class BrandService : CrudService<Brand>, IBrandService
    {
        private readonly IStringLocalizer<AgencyCampaignResource> localizer;

        public BrandService(DbContext dbContext, IStringLocalizer<AgencyCampaignResource> localizer) : base(dbContext)
        {
            this.localizer = localizer;
        }

        public async Task<PagedResult<Brand>> GetBrands(PagedRequest request, CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<Brand>()
                .AsNoTracking()
                .OrderByDescending(item => item.IsActive)
                .ThenByDescending(item => item.Id)
                .ToPagedResultAsync(request, cancellationToken);
        }

        public async Task<Brand?> GetBrandById(long id, CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<Brand>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        }

        public async Task<Brand> CreateBrand(CreateBrandRequest request, CancellationToken cancellationToken = default)
        {
            Brand brand = new(request.Name, request.TradeName, request.Document, request.ContactName, request.ContactEmail, request.Notes);
            bool success = await Insert(cancellationToken, brand);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return brand;
        }

        public async Task<Brand> UpdateBrand(long id, UpdateBrandRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException(localizer["request.route.idMismatch"]);
            }

            Brand? brand = await DbContext.Set<Brand>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (brand is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            brand.Update(request.Name, request.TradeName, request.Document, request.ContactName, request.ContactEmail, request.Notes, request.IsActive);

            Brand? result = await Update(brand, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return result;
        }
    }
}
