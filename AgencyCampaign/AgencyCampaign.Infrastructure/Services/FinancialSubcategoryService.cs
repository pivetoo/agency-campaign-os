using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Models.Financial;
using AgencyCampaign.Application.Requests.FinancialSubcategories;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
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

        public async Task<IReadOnlyCollection<FinancialSubcategoryModel>> GetAll(bool includeInactive, CancellationToken cancellationToken = default)
        {
            IQueryable<FinancialSubcategory> query = dbContext.Set<FinancialSubcategory>().AsNoTracking();
            if (!includeInactive)
            {
                query = query.Where(item => item.IsActive);
            }

            return await query
                .OrderBy(item => item.MacroCategory)
                .ThenBy(item => item.Name)
                .Select(item => Map(item))
                .ToArrayAsync(cancellationToken);
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
