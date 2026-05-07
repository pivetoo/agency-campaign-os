using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Application.Requests.Opportunities;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class OpportunitySourceService : IOpportunitySourceService
    {
        private readonly DbContext dbContext;
        private readonly IStringLocalizer<AgencyCampaignResource> localizer;

        public OpportunitySourceService(DbContext dbContext, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.dbContext = dbContext;
            this.localizer = localizer;
        }

        public async Task<IReadOnlyCollection<OpportunitySourceModel>> GetAll(bool includeInactive, CancellationToken cancellationToken = default)
        {
            IQueryable<OpportunitySource> query = dbContext.Set<OpportunitySource>().AsNoTracking();
            if (!includeInactive)
            {
                query = query.Where(item => item.IsActive);
            }

            return await query
                .OrderBy(item => item.DisplayOrder)
                .ThenBy(item => item.Name)
                .Select(item => new OpportunitySourceModel
                {
                    Id = item.Id,
                    Name = item.Name,
                    Color = item.Color,
                    DisplayOrder = item.DisplayOrder,
                    IsActive = item.IsActive
                })
                .ToArrayAsync(cancellationToken);
        }

        public async Task<OpportunitySourceModel> Create(CreateOpportunitySourceRequest request, CancellationToken cancellationToken = default)
        {
            OpportunitySource source = new(request.Name, request.Color, request.DisplayOrder);
            dbContext.Set<OpportunitySource>().Add(source);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Map(source);
        }

        public async Task<OpportunitySourceModel> Update(long id, UpdateOpportunitySourceRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException(localizer["request.route.idMismatch"]);
            }

            OpportunitySource? source = await dbContext.Set<OpportunitySource>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (source is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            source.Update(request.Name, request.Color, request.DisplayOrder, request.IsActive);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Map(source);
        }

        public async Task Delete(long id, CancellationToken cancellationToken = default)
        {
            OpportunitySource? source = await dbContext.Set<OpportunitySource>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (source is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            dbContext.Set<OpportunitySource>().Remove(source);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        private static OpportunitySourceModel Map(OpportunitySource source) => new()
        {
            Id = source.Id,
            Name = source.Name,
            Color = source.Color,
            DisplayOrder = source.DisplayOrder,
            IsActive = source.IsActive
        };
    }

    public sealed class OpportunityTagService : IOpportunityTagService
    {
        private readonly DbContext dbContext;
        private readonly IStringLocalizer<AgencyCampaignResource> localizer;

        public OpportunityTagService(DbContext dbContext, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.dbContext = dbContext;
            this.localizer = localizer;
        }

        public async Task<IReadOnlyCollection<OpportunityTagModel>> GetAll(bool includeInactive, CancellationToken cancellationToken = default)
        {
            IQueryable<OpportunityTag> query = dbContext.Set<OpportunityTag>().AsNoTracking();
            if (!includeInactive)
            {
                query = query.Where(item => item.IsActive);
            }

            return await query
                .OrderBy(item => item.Name)
                .Select(item => new OpportunityTagModel
                {
                    Id = item.Id,
                    Name = item.Name,
                    Color = item.Color,
                    IsActive = item.IsActive
                })
                .ToArrayAsync(cancellationToken);
        }

        public async Task<OpportunityTagModel> Create(CreateOpportunityTagRequest request, CancellationToken cancellationToken = default)
        {
            OpportunityTag tag = new(request.Name, request.Color);
            dbContext.Set<OpportunityTag>().Add(tag);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Map(tag);
        }

        public async Task<OpportunityTagModel> Update(long id, UpdateOpportunityTagRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException(localizer["request.route.idMismatch"]);
            }

            OpportunityTag? tag = await dbContext.Set<OpportunityTag>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (tag is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            tag.Update(request.Name, request.Color, request.IsActive);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Map(tag);
        }

        public async Task Delete(long id, CancellationToken cancellationToken = default)
        {
            OpportunityTag? tag = await dbContext.Set<OpportunityTag>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (tag is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            dbContext.Set<OpportunityTag>().Remove(tag);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        private static OpportunityTagModel Map(OpportunityTag tag) => new()
        {
            Id = tag.Id,
            Name = tag.Name,
            Color = tag.Color,
            IsActive = tag.IsActive
        };
    }
}
