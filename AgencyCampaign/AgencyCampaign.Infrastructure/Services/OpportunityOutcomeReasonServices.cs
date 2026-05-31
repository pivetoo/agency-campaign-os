using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Application.Requests.Opportunities;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using Microsoft.EntityFrameworkCore;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class OpportunityWinReasonService : IOpportunityWinReasonService
    {
        private readonly DbContext dbContext;

        public OpportunityWinReasonService(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<PagedResult<OpportunityWinReasonModel>> GetAll(PagedRequest request, string? search, bool includeInactive, CancellationToken cancellationToken = default)
        {
            IQueryable<OpportunityWinReason> query = dbContext.Set<OpportunityWinReason>().AsNoTracking();
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
                .OrderBy(item => item.DisplayOrder)
                .ThenBy(item => item.Name)
                .Select(item => new OpportunityWinReasonModel
                {
                    Id = item.Id,
                    Name = item.Name,
                    Color = item.Color,
                    DisplayOrder = item.DisplayOrder,
                    IsActive = item.IsActive
                })
                .ToPagedResultAsync(request, cancellationToken);
        }

        public async Task<OpportunityWinReasonModel> Create(CreateOpportunityWinReasonRequest request, CancellationToken cancellationToken = default)
        {
            OpportunityWinReason reason = new(request.Name, request.Color, request.DisplayOrder);
            dbContext.Set<OpportunityWinReason>().Add(reason);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Map(reason);
        }

        public async Task<OpportunityWinReasonModel> Update(long id, UpdateOpportunityWinReasonRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException("request.route.idMismatch");
            }

            OpportunityWinReason? reason = await dbContext.Set<OpportunityWinReason>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (reason is null)
            {
                throw new InvalidOperationException("record.notFound");
            }

            reason.Update(request.Name, request.Color, request.DisplayOrder, request.IsActive);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Map(reason);
        }

        public async Task Delete(long id, CancellationToken cancellationToken = default)
        {
            OpportunityWinReason? reason = await dbContext.Set<OpportunityWinReason>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (reason is null)
            {
                throw new InvalidOperationException("record.notFound");
            }

            reason.Update(reason.Name, reason.Color, reason.DisplayOrder, false);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        private static OpportunityWinReasonModel Map(OpportunityWinReason reason) => new()
        {
            Id = reason.Id,
            Name = reason.Name,
            Color = reason.Color,
            DisplayOrder = reason.DisplayOrder,
            IsActive = reason.IsActive
        };
    }

    public sealed class OpportunityLossReasonService : IOpportunityLossReasonService
    {
        private readonly DbContext dbContext;

        public OpportunityLossReasonService(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<PagedResult<OpportunityLossReasonModel>> GetAll(PagedRequest request, string? search, bool includeInactive, CancellationToken cancellationToken = default)
        {
            IQueryable<OpportunityLossReason> query = dbContext.Set<OpportunityLossReason>().AsNoTracking();
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
                .OrderBy(item => item.DisplayOrder)
                .ThenBy(item => item.Name)
                .Select(item => new OpportunityLossReasonModel
                {
                    Id = item.Id,
                    Name = item.Name,
                    Color = item.Color,
                    DisplayOrder = item.DisplayOrder,
                    IsActive = item.IsActive
                })
                .ToPagedResultAsync(request, cancellationToken);
        }

        public async Task<OpportunityLossReasonModel> Create(CreateOpportunityLossReasonRequest request, CancellationToken cancellationToken = default)
        {
            OpportunityLossReason reason = new(request.Name, request.Color, request.DisplayOrder);
            dbContext.Set<OpportunityLossReason>().Add(reason);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Map(reason);
        }

        public async Task<OpportunityLossReasonModel> Update(long id, UpdateOpportunityLossReasonRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException("request.route.idMismatch");
            }

            OpportunityLossReason? reason = await dbContext.Set<OpportunityLossReason>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (reason is null)
            {
                throw new InvalidOperationException("record.notFound");
            }

            reason.Update(request.Name, request.Color, request.DisplayOrder, request.IsActive);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Map(reason);
        }

        public async Task Delete(long id, CancellationToken cancellationToken = default)
        {
            OpportunityLossReason? reason = await dbContext.Set<OpportunityLossReason>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (reason is null)
            {
                throw new InvalidOperationException("record.notFound");
            }

            reason.Update(reason.Name, reason.Color, reason.DisplayOrder, false);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        private static OpportunityLossReasonModel Map(OpportunityLossReason reason) => new()
        {
            Id = reason.Id,
            Name = reason.Name,
            Color = reason.Color,
            DisplayOrder = reason.DisplayOrder,
            IsActive = reason.IsActive
        };
    }
}
