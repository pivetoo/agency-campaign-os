using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Application.Requests.Opportunities;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class OpportunityApprovalImpactService : IOpportunityApprovalImpactService
    {
        private readonly DbContext dbContext;

        public OpportunityApprovalImpactService(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<IReadOnlyCollection<OpportunityApprovalImpactModel>> GetByApprovalId(long approvalId, CancellationToken cancellationToken = default)
        {
            return await dbContext.Set<OpportunityApprovalImpact>()
                .AsNoTracking()
                .Where(item => item.OpportunityApprovalRequestId == approvalId)
                .OrderBy(item => item.DisplayOrder)
                .ThenBy(item => item.Id)
                .Select(item => Map(item))
                .ToListAsync(cancellationToken);
        }

        public async Task<OpportunityApprovalImpactModel> Add(long approvalId, AddOpportunityApprovalImpactRequest request, CancellationToken cancellationToken = default)
        {
            OpportunityApprovalImpact impact = new(approvalId, request.Label, request.Value, request.IsGood, request.DisplayOrder);
            dbContext.Set<OpportunityApprovalImpact>().Add(impact);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Map(impact);
        }

        public async Task Remove(long id, CancellationToken cancellationToken = default)
        {
            OpportunityApprovalImpact? impact = await dbContext.Set<OpportunityApprovalImpact>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (impact is null)
            {
                throw new InvalidOperationException("record.notFound");
            }

            dbContext.Set<OpportunityApprovalImpact>().Remove(impact);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        private static OpportunityApprovalImpactModel Map(OpportunityApprovalImpact impact) => new()
        {
            Id = impact.Id,
            OpportunityApprovalRequestId = impact.OpportunityApprovalRequestId,
            Label = impact.Label,
            Value = impact.Value,
            IsGood = impact.IsGood,
            DisplayOrder = impact.DisplayOrder,
        };
    }
}
