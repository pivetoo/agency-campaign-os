using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.Proposals;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using Archon.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class ProposalService : CrudService<Proposal>, IProposalService
    {
        private readonly IStringLocalizer<AgencyCampaignResource> localizer;

        public ProposalService(DbContext dbContext, IStringLocalizer<AgencyCampaignResource> localizer) : base(dbContext)
        {
            this.localizer = localizer;
        }

        public async Task<PagedResult<Proposal>> GetProposals(PagedRequest request, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .OrderByDescending(item => item.Id)
                .ToPagedResultAsync(request, cancellationToken);
        }

        public async Task<Proposal?> GetProposalById(long id, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        }

        public async Task<Proposal> CreateProposal(CreateProposalRequest request, CancellationToken cancellationToken = default)
        {
            await EnsureBrandExists(request.BrandId, cancellationToken);

            long internalOwnerId = request.InternalOwnerId ?? 0;
            string internalOwnerName = request.InternalOwnerName ?? string.Empty;

            Proposal proposal = new(
                request.BrandId,
                request.Name,
                internalOwnerId,
                request.Description,
                request.ValidityUntil,
                request.Notes);

            if (!string.IsNullOrWhiteSpace(internalOwnerName))
            {
                proposal.SetInternalOwner(internalOwnerId, internalOwnerName);
            }

            bool success = await Insert(cancellationToken, proposal);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return await GetProposalById(proposal.Id, cancellationToken) ?? proposal;
        }

        public async Task<Proposal> UpdateProposal(long id, UpdateProposalRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException(localizer["request.route.idMismatch"]);
            }

            Proposal? proposal = await DbContext.Set<Proposal>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (proposal is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            await EnsureBrandExists(request.BrandId, cancellationToken);

            proposal.Update(
                request.Name,
                request.BrandId,
                request.ValidityUntil,
                request.Description,
                request.Notes);

            Proposal? result = await Update(proposal, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return await GetProposalById(result.Id, cancellationToken) ?? result;
        }

        public async Task<Proposal> MarkAsSent(long id, CancellationToken cancellationToken = default)
        {
            Proposal? proposal = await GetAndValidateProposal(id, cancellationToken);
            proposal.MarkAsSent();

            return await SaveAndReturn(proposal, cancellationToken);
        }

        public async Task<Proposal> MarkAsViewed(long id, CancellationToken cancellationToken = default)
        {
            Proposal? proposal = await GetAndValidateProposal(id, cancellationToken);
            proposal.MarkAsViewed();

            return await SaveAndReturn(proposal, cancellationToken);
        }

        public async Task<Proposal> ApproveProposal(long id, CancellationToken cancellationToken = default)
        {
            Proposal? proposal = await GetAndValidateProposal(id, cancellationToken);
            proposal.Approve();

            return await SaveAndReturn(proposal, cancellationToken);
        }

        public async Task<Proposal> RejectProposal(long id, CancellationToken cancellationToken = default)
        {
            Proposal? proposal = await GetAndValidateProposal(id, cancellationToken);
            proposal.Reject();

            return await SaveAndReturn(proposal, cancellationToken);
        }

        public async Task<Proposal> ConvertToCampaign(long id, long campaignId, CancellationToken cancellationToken = default)
        {
            Proposal? proposal = await GetAndValidateProposal(id, cancellationToken);

            bool campaignExists = await DbContext.Set<Campaign>()
                .AsNoTracking()
                .AnyAsync(item => item.Id == campaignId, cancellationToken);

            if (!campaignExists)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            proposal.ConvertToCampaign(campaignId);

            return await SaveAndReturn(proposal, cancellationToken);
        }

        public async Task<Proposal> CancelProposal(long id, CancellationToken cancellationToken = default)
        {
            Proposal? proposal = await GetAndValidateProposal(id, cancellationToken);
            proposal.Cancel();

            return await SaveAndReturn(proposal, cancellationToken);
        }

        private async Task<Proposal> GetAndValidateProposal(long id, CancellationToken cancellationToken)
        {
            Proposal? proposal = await DbContext.Set<Proposal>()
                .AsTracking()
                .Include(item => item.Items)
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (proposal is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            return proposal;
        }

        private async Task EnsureBrandExists(long brandId, CancellationToken cancellationToken)
        {
            bool exists = await DbContext.Set<Brand>()
                .AsNoTracking()
                .AnyAsync(item => item.Id == brandId, cancellationToken);

            if (!exists)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }
        }

        private async Task<Proposal> SaveAndReturn(Proposal proposal, CancellationToken cancellationToken)
        {
            bool success = await DbContext.SaveChangesAsync(cancellationToken) > 0;
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return await GetProposalById(proposal.Id, cancellationToken) ?? proposal;
        }

        private IQueryable<Proposal> QueryWithDetails()
        {
            return DbContext.Set<Proposal>()
                .AsNoTracking()
                .Include(item => item.Brand)
                .Include(item => item.Campaign)
                .Include(item => item.Items)
                    .ThenInclude(item => item.Creator);
        }
    }
}