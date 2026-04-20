using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.Proposals;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Archon.Infrastructure.Persistence.EF;
using Archon.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class ProposalItemService : CrudService<ProposalItem>, IProposalItemService
    {
        private readonly IStringLocalizer<AgencyCampaignResource> localizer;

        public ProposalItemService(DbContext dbContext, IStringLocalizer<AgencyCampaignResource> localizer) : base(dbContext)
        {
            this.localizer = localizer;
        }

        public async Task<ProposalItem?> GetProposalItemById(long id, CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<ProposalItem>()
                .AsNoTracking()
                .Include(item => item.Creator)
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        }

        public async Task<ProposalItem> CreateProposalItem(CreateProposalItemRequest request, CancellationToken cancellationToken = default)
        {
            await EnsureProposalExists(request.ProposalId, cancellationToken);

            if (request.CreatorId.HasValue)
            {
                await EnsureCreatorExists(request.CreatorId.Value, cancellationToken);
            }

            ProposalItem item = new(
                request.ProposalId,
                request.Description,
                request.Quantity,
                request.UnitPrice,
                request.DeliveryDeadline,
                request.CreatorId,
                request.Observations);

            bool success = await Insert(cancellationToken, item);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            await RecalculateProposalTotal(request.ProposalId, cancellationToken);

            return await GetProposalItemById(item.Id, cancellationToken) ?? item;
        }

        public async Task<ProposalItem> UpdateProposalItem(long id, UpdateProposalItemRequest request, CancellationToken cancellationToken = default)
        {
            ProposalItem? item = await DbContext.Set<ProposalItem>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (item is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            long proposalId = item.ProposalId;

            item.Update(
                request.Description,
                request.Quantity,
                request.UnitPrice,
                request.DeliveryDeadline,
                request.Observations);

            ProposalItem? result = await Update(item, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            await RecalculateProposalTotal(proposalId, cancellationToken);

            return await GetProposalItemById(id, cancellationToken) ?? item;
        }

        public async Task DeleteProposalItem(long id, CancellationToken cancellationToken = default)
        {
            ProposalItem? item = await DbContext.Set<ProposalItem>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (item is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            long proposalId = item.ProposalId;

            await Delete([item], cancellationToken);

            await RecalculateProposalTotal(proposalId, cancellationToken);
        }

        public async Task<IReadOnlyCollection<ProposalItem>> GetItemsByProposalId(long proposalId, CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<ProposalItem>()
                .AsNoTracking()
                .Include(item => item.Creator)
                .Where(item => item.ProposalId == proposalId)
                .ToListAsync(cancellationToken);
        }

        private async Task EnsureProposalExists(long proposalId, CancellationToken cancellationToken)
        {
            bool exists = await DbContext.Set<Proposal>()
                .AsNoTracking()
                .AnyAsync(item => item.Id == proposalId, cancellationToken);

            if (!exists)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }
        }

        private async Task EnsureCreatorExists(long creatorId, CancellationToken cancellationToken)
        {
            bool exists = await DbContext.Set<Creator>()
                .AsNoTracking()
                .AnyAsync(item => item.Id == creatorId, cancellationToken);

            if (!exists)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }
        }

        private async Task RecalculateProposalTotal(long proposalId, CancellationToken cancellationToken)
        {
            Proposal? proposal = await DbContext.Set<Proposal>()
                .AsTracking()
                .Include(item => item.Items)
                .FirstOrDefaultAsync(item => item.Id == proposalId, cancellationToken);

            if (proposal is null)
            {
                return;
            }

            decimal total = proposal.Items.Sum(x => x.Total);
            proposal.UpdateTotalValue(total);

            await DbContext.SaveChangesAsync(cancellationToken);
        }
    }
}