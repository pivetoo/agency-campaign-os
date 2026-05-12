using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.Brands;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using Archon.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System.Runtime.CompilerServices;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class BrandService : CrudService<Brand>, IBrandService
    {
        private readonly IStringLocalizer<AgencyCampaignResource> localizer;

        public BrandService(DbContext dbContext, IStringLocalizer<AgencyCampaignResource> localizer) : base(dbContext)
        {
            this.localizer = localizer;
        }

        public async Task<PagedResult<Brand>> GetBrands(PagedRequest request, string? search, CancellationToken cancellationToken = default)
        {
            var query = DbContext.Set<Brand>().AsNoTracking();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var lower = search.ToLower();
                query = query.Where(item => item.Name.ToLower().Contains(lower) || (item.TradeName != null && item.TradeName.ToLower().Contains(lower)));
            }
            return await query
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

        public async Task<Brand> SetBrandLogo(long id, string logoUrl, CancellationToken cancellationToken = default)
        {
            Brand brand = await LoadTrackedBrand(id, cancellationToken);
            brand.SetLogo(logoUrl);

            Brand? result = await Update(brand, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return result;
        }

        public async Task<Brand> RemoveBrandLogo(long id, CancellationToken cancellationToken = default)
        {
            Brand brand = await LoadTrackedBrand(id, cancellationToken);
            brand.SetLogo(null);

            Brand? result = await Update(brand, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return result;
        }

        private async Task<Brand> LoadTrackedBrand(long id, CancellationToken cancellationToken)
        {
            Brand? brand = await DbContext.Set<Brand>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (brand is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            return brand;
        }

        public async IAsyncEnumerable<string> ExportAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (Brand brand in DbContext.Set<Brand>()
                .AsNoTracking()
                .OrderBy(b => b.Name)
                .AsAsyncEnumerable()
                .WithCancellation(cancellationToken))
            {
                yield return CsvLine(brand.Name, brand.TradeName, brand.Document, brand.ContactName, brand.ContactEmail, brand.Notes, brand.IsActive ? "Sim" : "Não");
            }
        }

        private static string CsvLine(params string?[] fields) =>
            string.Join(",", fields.Select(EscapeField));

        private static string EscapeField(string? value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
                return $"\"{value.Replace("\"", "\"\"")}\"";
            return value;
        }
    }
}
