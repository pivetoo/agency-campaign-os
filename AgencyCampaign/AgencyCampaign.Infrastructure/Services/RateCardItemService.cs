using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Application.Requests.Proposals;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Archon.Core.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class RateCardItemService : IRateCardItemService
    {
        private readonly DbContext dbContext;

        public RateCardItemService(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<IReadOnlyCollection<RateCardItemModel>> GetByCreator(long creatorId, bool includeInactive, CancellationToken cancellationToken = default)
        {
            IQueryable<RateCardItem> query = dbContext.Set<RateCardItem>()
                .AsNoTracking()
                .Where(item => item.CreatorId == creatorId);

            if (!includeInactive)
            {
                query = query.Where(item => item.IsActive);
            }

            return await query
                .OrderBy(item => item.DisplayOrder)
                .ThenBy(item => item.Label)
                .Select(item => Map(item))
                .ToListAsync(cancellationToken);
        }

        public async Task<RateCardItemModel> Create(CreateRateCardItemRequest request, CancellationToken cancellationToken = default)
        {
            await EnsureCreatorExists(request.CreatorId, cancellationToken);

            RateCardItem item = new(request.CreatorId, request.Label, request.UnitPrice, request.DisplayOrder);
            dbContext.Set<RateCardItem>().Add(item);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Map(item);
        }

        public async Task<RateCardItemModel> Update(long id, UpdateRateCardItemRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException("request.route.idMismatch");
            }

            RateCardItem? item = await dbContext.Set<RateCardItem>()
                .AsTracking()
                .FirstOrDefaultAsync(entry => entry.Id == id, cancellationToken);

            if (item is null)
            {
                throw new NotFoundException("record.notFound");
            }

            item.Update(request.Label, request.UnitPrice, request.DisplayOrder, request.IsActive);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Map(item);
        }

        public async Task Delete(long id, CancellationToken cancellationToken = default)
        {
            RateCardItem? item = await dbContext.Set<RateCardItem>()
                .AsTracking()
                .FirstOrDefaultAsync(entry => entry.Id == id, cancellationToken);

            if (item is null)
            {
                throw new NotFoundException("record.notFound");
            }

            item.Update(item.Label, item.UnitPrice, item.DisplayOrder, false);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task EnsureCreatorExists(long creatorId, CancellationToken cancellationToken)
        {
            bool exists = await dbContext.Set<Creator>()
                .AsNoTracking()
                .AnyAsync(item => item.Id == creatorId, cancellationToken);

            if (!exists)
            {
                throw new NotFoundException("record.notFound");
            }
        }

        private static RateCardItemModel Map(RateCardItem item) => new()
        {
            Id = item.Id,
            CreatorId = item.CreatorId,
            Label = item.Label,
            UnitPrice = item.UnitPrice,
            DisplayOrder = item.DisplayOrder,
            IsActive = item.IsActive
        };
    }
}
