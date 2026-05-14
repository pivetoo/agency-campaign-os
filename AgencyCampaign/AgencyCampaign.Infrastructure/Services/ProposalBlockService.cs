using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Application.Requests.Proposals;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Archon.Application.Abstractions;
using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class ProposalBlockService : IProposalBlockService
    {
        private readonly DbContext dbContext;
        private readonly ICurrentUser currentUser;
        private readonly IStringLocalizer<AgencyCampaignResource> localizer;

        public ProposalBlockService(DbContext dbContext, ICurrentUser currentUser, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.dbContext = dbContext;
            this.currentUser = currentUser;
            this.localizer = localizer;
        }

        public async Task<PagedResult<ProposalBlockModel>> GetAll(PagedRequest request, string? search, string? category, bool includeInactive, CancellationToken cancellationToken = default)
        {
            IQueryable<ProposalBlock> query = dbContext.Set<ProposalBlock>().AsNoTracking();

            if (!includeInactive)
            {
                query = query.Where(item => item.IsActive);
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                string normalized = category.Trim();
                query = query.Where(item => item.Category == normalized);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string lower = search.ToLower();
                query = query.Where(item => item.Name.ToLower().Contains(lower) || item.Body.ToLower().Contains(lower));
            }

            return await query
                .OrderBy(item => item.Category)
                .ThenBy(item => item.Name)
                .Select(item => new ProposalBlockModel
                {
                    Id = item.Id,
                    Name = item.Name,
                    Body = item.Body,
                    Category = item.Category,
                    IsActive = item.IsActive,
                    CreatedByUserName = item.CreatedByUserName,
                    CreatedAt = item.CreatedAt,
                    UpdatedAt = item.UpdatedAt
                })
                .ToPagedResultAsync(request, cancellationToken);
        }

        public async Task<ProposalBlockModel?> GetById(long id, CancellationToken cancellationToken = default)
        {
            ProposalBlock? block = await dbContext.Set<ProposalBlock>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            return block is null ? null : Map(block);
        }

        public async Task<ProposalBlockModel> Create(CreateProposalBlockRequest request, CancellationToken cancellationToken = default)
        {
            ProposalBlock block = new(request.Name, request.Body, request.Category, currentUser.UserId, currentUser.UserName);
            dbContext.Set<ProposalBlock>().Add(block);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Map(block);
        }

        public async Task<ProposalBlockModel> Update(long id, UpdateProposalBlockRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException(localizer["request.route.idMismatch"]);
            }

            ProposalBlock? block = await dbContext.Set<ProposalBlock>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (block is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            block.Update(request.Name, request.Body, request.Category, request.IsActive);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Map(block);
        }

        public async Task Delete(long id, CancellationToken cancellationToken = default)
        {
            ProposalBlock? block = await dbContext.Set<ProposalBlock>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (block is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            dbContext.Set<ProposalBlock>().Remove(block);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        private static ProposalBlockModel Map(ProposalBlock block)
        {
            return new ProposalBlockModel
            {
                Id = block.Id,
                Name = block.Name,
                Body = block.Body,
                Category = block.Category,
                IsActive = block.IsActive,
                CreatedByUserName = block.CreatedByUserName,
                CreatedAt = block.CreatedAt,
                UpdatedAt = block.UpdatedAt
            };
        }
    }
}
