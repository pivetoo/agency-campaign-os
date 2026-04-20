using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.Opportunities;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Archon.Infrastructure.Persistence.EF;
using Archon.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class OpportunityNegotiationService : CrudService<OpportunityNegotiation>, IOpportunityNegotiationService
    {
        private readonly IStringLocalizer<AgencyCampaignResource> localizer;

        public OpportunityNegotiationService(DbContext dbContext, IStringLocalizer<AgencyCampaignResource> localizer) : base(dbContext)
        {
            this.localizer = localizer;
        }

        public async Task<OpportunityNegotiation?> GetOpportunityNegotiationById(long id, CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<OpportunityNegotiation>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        }

        public async Task<OpportunityNegotiation> CreateOpportunityNegotiation(CreateOpportunityNegotiationRequest request, CancellationToken cancellationToken = default)
        {
            await EnsureOpportunityExists(request.OpportunityId, cancellationToken);

            OpportunityNegotiation negotiation = new(
                request.OpportunityId,
                request.Title,
                request.Amount,
                request.NegotiatedAt,
                request.Notes);

            bool success = await Insert(cancellationToken, negotiation);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return await GetOpportunityNegotiationById(negotiation.Id, cancellationToken) ?? negotiation;
        }

        public async Task<OpportunityNegotiation> UpdateOpportunityNegotiation(long id, UpdateOpportunityNegotiationRequest request, CancellationToken cancellationToken = default)
        {
            OpportunityNegotiation? negotiation = await DbContext.Set<OpportunityNegotiation>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (negotiation is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            negotiation.Update(request.Title, request.Amount, request.NegotiatedAt, request.Notes);

            OpportunityNegotiation? result = await Update(negotiation, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return await GetOpportunityNegotiationById(id, cancellationToken) ?? negotiation;
        }

        public async Task DeleteOpportunityNegotiation(long id, CancellationToken cancellationToken = default)
        {
            OpportunityNegotiation? negotiation = await DbContext.Set<OpportunityNegotiation>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (negotiation is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            await Delete([negotiation], cancellationToken);
        }

        public async Task<IReadOnlyCollection<OpportunityNegotiation>> GetNegotiationsByOpportunityId(long opportunityId, CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<OpportunityNegotiation>()
                .AsNoTracking()
                .Where(item => item.OpportunityId == opportunityId)
                .OrderByDescending(item => item.NegotiatedAt)
                .ThenByDescending(item => item.Id)
                .ToListAsync(cancellationToken);
        }

        private async Task EnsureOpportunityExists(long opportunityId, CancellationToken cancellationToken)
        {
            bool exists = await DbContext.Set<Opportunity>()
                .AsNoTracking()
                .AnyAsync(item => item.Id == opportunityId, cancellationToken);

            if (!exists)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }
        }
    }
}
