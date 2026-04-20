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
    public sealed class OpportunityFollowUpService : CrudService<OpportunityFollowUp>, IOpportunityFollowUpService
    {
        private readonly IStringLocalizer<AgencyCampaignResource> localizer;

        public OpportunityFollowUpService(DbContext dbContext, IStringLocalizer<AgencyCampaignResource> localizer) : base(dbContext)
        {
            this.localizer = localizer;
        }

        public async Task<OpportunityFollowUp?> GetOpportunityFollowUpById(long id, CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<OpportunityFollowUp>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        }

        public async Task<OpportunityFollowUp> CreateOpportunityFollowUp(CreateOpportunityFollowUpRequest request, CancellationToken cancellationToken = default)
        {
            await EnsureOpportunityExists(request.OpportunityId, cancellationToken);

            OpportunityFollowUp followUp = new(
                request.OpportunityId,
                request.Subject,
                request.DueAt,
                request.Notes);

            bool success = await Insert(cancellationToken, followUp);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return await GetOpportunityFollowUpById(followUp.Id, cancellationToken) ?? followUp;
        }

        public async Task<OpportunityFollowUp> UpdateOpportunityFollowUp(long id, UpdateOpportunityFollowUpRequest request, CancellationToken cancellationToken = default)
        {
            OpportunityFollowUp? followUp = await DbContext.Set<OpportunityFollowUp>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (followUp is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            followUp.Update(request.Subject, request.DueAt, request.Notes);

            OpportunityFollowUp? result = await Update(followUp, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return await GetOpportunityFollowUpById(id, cancellationToken) ?? followUp;
        }

        public async Task<OpportunityFollowUp> CompleteOpportunityFollowUp(long id, CancellationToken cancellationToken = default)
        {
            OpportunityFollowUp? followUp = await DbContext.Set<OpportunityFollowUp>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (followUp is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            followUp.Complete();

            OpportunityFollowUp? result = await Update(followUp, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return await GetOpportunityFollowUpById(id, cancellationToken) ?? followUp;
        }

        public async Task DeleteOpportunityFollowUp(long id, CancellationToken cancellationToken = default)
        {
            OpportunityFollowUp? followUp = await DbContext.Set<OpportunityFollowUp>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (followUp is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            await Delete([followUp], cancellationToken);
        }

        public async Task<IReadOnlyCollection<OpportunityFollowUp>> GetFollowUpsByOpportunityId(long opportunityId, CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<OpportunityFollowUp>()
                .AsNoTracking()
                .Where(item => item.OpportunityId == opportunityId)
                .OrderBy(item => item.IsCompleted)
                .ThenBy(item => item.DueAt)
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
