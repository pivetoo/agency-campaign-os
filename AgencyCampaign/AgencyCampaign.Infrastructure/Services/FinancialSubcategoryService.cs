using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Models.Financial;
using AgencyCampaign.Application.Requests.FinancialSubcategories;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class FinancialSubcategoryService : IFinancialSubcategoryService
    {
        private readonly DbContext dbContext;
        private readonly IStringLocalizer<AgencyCampaignResource> localizer;

        public FinancialSubcategoryService(DbContext dbContext, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.dbContext = dbContext;
            this.localizer = localizer;
        }

        public async Task<PagedResult<FinancialSubcategoryModel>> GetAll(PagedRequest request, string? search, bool includeInactive, CancellationToken cancellationToken = default)
        {
            IQueryable<FinancialSubcategory> query = dbContext.Set<FinancialSubcategory>().AsNoTracking();
            if (!includeInactive)
            {
                query = query.Where(item => item.IsActive);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string lower = search.ToLower();
                query = query.Where(item => item.Name.ToLower().Contains(lower));
            }

            return await query
                .OrderBy(item => item.MacroCategory)
                .ThenBy(item => item.Name)
                .Select(item => new FinancialSubcategoryModel
                {
                    Id = item.Id,
                    Name = item.Name,
                    MacroCategory = item.MacroCategory,
                    Color = item.Color,
                    IsActive = item.IsActive
                })
                .ToPagedResultAsync(request, cancellationToken);
        }

        public async Task<FinancialSubcategoryModel> Create(CreateFinancialSubcategoryRequest request, CancellationToken cancellationToken = default)
        {
            FinancialSubcategory subcategory = new(request.Name, request.MacroCategory, request.Color);
            dbContext.Set<FinancialSubcategory>().Add(subcategory);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Map(subcategory);
        }

        public async Task<FinancialSubcategoryModel> Update(long id, UpdateFinancialSubcategoryRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException(localizer["request.route.idMismatch"]);
            }

            FinancialSubcategory? subcategory = await dbContext.Set<FinancialSubcategory>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (subcategory is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            subcategory.Update(request.Name, request.MacroCategory, request.Color, request.IsActive);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Map(subcategory);
        }

        public async Task Delete(long id, CancellationToken cancellationToken = default)
        {
            FinancialSubcategory? subcategory = await dbContext.Set<FinancialSubcategory>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (subcategory is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            dbContext.Set<FinancialSubcategory>().Remove(subcategory);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        private static FinancialSubcategoryModel Map(FinancialSubcategory subcategory) => new()
        {
            Id = subcategory.Id,
            Name = subcategory.Name,
            MacroCategory = subcategory.MacroCategory,
            Color = subcategory.Color,
            IsActive = subcategory.IsActive
        };
    }
}
