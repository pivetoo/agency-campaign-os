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
    public sealed class ProposalTemplateService : IProposalTemplateService
    {
        private readonly DbContext dbContext;
        private readonly ICurrentUser currentUser;
        private readonly IStringLocalizer<AgencyCampaignResource> localizer;

        public ProposalTemplateService(DbContext dbContext, ICurrentUser currentUser, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.dbContext = dbContext;
            this.currentUser = currentUser;
            this.localizer = localizer;
        }

        public async Task<PagedResult<ProposalTemplateModel>> GetAll(PagedRequest request, string? search, bool includeInactive, CancellationToken cancellationToken = default)
        {
            IQueryable<ProposalTemplate> query = dbContext.Set<ProposalTemplate>()
                .AsNoTracking()
                .Include(item => item.Items);

            if (!includeInactive)
            {
                query = query.Where(item => item.IsActive);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string lower = search.ToLower();
                query = query.Where(item => item.Name.ToLower().Contains(lower)
                    || (item.Description != null && item.Description.ToLower().Contains(lower)));
            }

            PagedResult<ProposalTemplate> paged = await query
                .OrderBy(item => item.Name)
                .ToPagedResultAsync(request, cancellationToken);

            return new PagedResult<ProposalTemplateModel>
            {
                Items = paged.Items.Select(Map).ToArray(),
                Pagination = paged.Pagination
            };
        }

        public async Task<ProposalTemplateModel?> GetById(long id, CancellationToken cancellationToken = default)
        {
            ProposalTemplate? template = await dbContext.Set<ProposalTemplate>()
                .AsNoTracking()
                .Include(item => item.Items)
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            return template is null ? null : Map(template);
        }

        public async Task<ProposalTemplateModel> Create(CreateProposalTemplateRequest request, CancellationToken cancellationToken = default)
        {
            ProposalTemplate template = new(request.Name, request.Description, currentUser.UserId, currentUser.UserName);
            dbContext.Set<ProposalTemplate>().Add(template);
            await dbContext.SaveChangesAsync(cancellationToken);

            await ReplaceTemplateItems(template.Id, request.Items, cancellationToken);

            ProposalTemplate? reloaded = await dbContext.Set<ProposalTemplate>()
                .AsNoTracking()
                .Include(item => item.Items)
                .FirstOrDefaultAsync(item => item.Id == template.Id, cancellationToken);

            return Map(reloaded ?? template);
        }

        public async Task<ProposalTemplateModel> Update(long id, UpdateProposalTemplateRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException(localizer["request.route.idMismatch"]);
            }

            ProposalTemplate? template = await dbContext.Set<ProposalTemplate>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (template is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            template.Update(request.Name, request.Description, request.IsActive);
            await dbContext.SaveChangesAsync(cancellationToken);

            await ReplaceTemplateItems(id, request.Items, cancellationToken);

            ProposalTemplate? reloaded = await dbContext.Set<ProposalTemplate>()
                .AsNoTracking()
                .Include(item => item.Items)
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            return Map(reloaded ?? template);
        }

        public async Task Delete(long id, CancellationToken cancellationToken = default)
        {
            ProposalTemplate? template = await dbContext.Set<ProposalTemplate>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (template is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            dbContext.Set<ProposalTemplate>().Remove(template);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<int> ApplyToProposal(long proposalId, long templateId, CancellationToken cancellationToken = default)
        {
            Proposal? proposal = await dbContext.Set<Proposal>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == proposalId, cancellationToken);

            if (proposal is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            ProposalTemplate? template = await dbContext.Set<ProposalTemplate>()
                .AsNoTracking()
                .Include(item => item.Items)
                .FirstOrDefaultAsync(item => item.Id == templateId && item.IsActive, cancellationToken);

            if (template is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            DateTimeOffset reference = DateTimeOffset.UtcNow;
            int created = 0;

            foreach (ProposalTemplateItem templateItem in template.Items.OrderBy(item => item.DisplayOrder))
            {
                DateTimeOffset? deadline = templateItem.DefaultDeliveryDays.HasValue
                    ? reference.AddDays(templateItem.DefaultDeliveryDays.Value)
                    : null;

                ProposalItem newItem = new(
                    proposalId,
                    templateItem.Description,
                    templateItem.DefaultQuantity,
                    templateItem.DefaultUnitPrice,
                    deadline,
                    null,
                    templateItem.Observations);

                dbContext.Set<ProposalItem>().Add(newItem);
                created++;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            return created;
        }

        private async Task ReplaceTemplateItems(long templateId, IReadOnlyCollection<ProposalTemplateItemRequest> items, CancellationToken cancellationToken)
        {
            List<ProposalTemplateItem> existing = await dbContext.Set<ProposalTemplateItem>()
                .AsTracking()
                .Where(item => item.ProposalTemplateId == templateId)
                .ToListAsync(cancellationToken);

            if (existing.Count > 0)
            {
                dbContext.Set<ProposalTemplateItem>().RemoveRange(existing);
            }

            int order = 0;
            foreach (ProposalTemplateItemRequest itemRequest in items)
            {
                ProposalTemplateItem item = new(
                    templateId,
                    itemRequest.Description,
                    itemRequest.DefaultQuantity,
                    itemRequest.DefaultUnitPrice,
                    itemRequest.DefaultDeliveryDays,
                    itemRequest.Observations,
                    itemRequest.DisplayOrder == 0 ? order : itemRequest.DisplayOrder);

                dbContext.Set<ProposalTemplateItem>().Add(item);
                order++;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        private static ProposalTemplateModel Map(ProposalTemplate template)
        {
            return new ProposalTemplateModel
            {
                Id = template.Id,
                Name = template.Name,
                Description = template.Description,
                IsActive = template.IsActive,
                CreatedByUserName = template.CreatedByUserName,
                CreatedAt = template.CreatedAt,
                UpdatedAt = template.UpdatedAt,
                Items = template.Items
                    .OrderBy(item => item.DisplayOrder)
                    .Select(item => new ProposalTemplateItemModel
                    {
                        Id = item.Id,
                        ProposalTemplateId = item.ProposalTemplateId,
                        Description = item.Description,
                        DefaultQuantity = item.DefaultQuantity,
                        DefaultUnitPrice = item.DefaultUnitPrice,
                        DefaultDeliveryDays = item.DefaultDeliveryDays,
                        Observations = item.Observations,
                        DisplayOrder = item.DisplayOrder
                    }).ToArray()
            };
        }
    }
}
