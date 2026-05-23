using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Application.Requests.Opportunities;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class OpportunityApprovalDiffService : IOpportunityApprovalDiffService
    {
        private readonly DbContext dbContext;

        public OpportunityApprovalDiffService(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<IReadOnlyCollection<OpportunityApprovalDiffModel>> GetByApprovalId(long approvalId, CancellationToken cancellationToken = default)
        {
            return await dbContext.Set<OpportunityApprovalDiff>()
                .AsNoTracking()
                .Where(item => item.OpportunityApprovalRequestId == approvalId)
                .OrderBy(item => item.DisplayOrder)
                .ThenBy(item => item.Id)
                .Select(item => Map(item))
                .ToListAsync(cancellationToken);
        }

        public async Task<OpportunityApprovalDiffModel> Add(long approvalId, AddOpportunityApprovalDiffRequest request, CancellationToken cancellationToken = default)
        {
            OpportunityApprovalDiff diff = new(approvalId, request.Field, request.PolicyValue, request.RequestedValue, (OpportunityApprovalDiffKind)request.Kind, request.Delta, request.DisplayOrder);
            dbContext.Set<OpportunityApprovalDiff>().Add(diff);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Map(diff);
        }

        public async Task Remove(long id, CancellationToken cancellationToken = default)
        {
            OpportunityApprovalDiff? diff = await dbContext.Set<OpportunityApprovalDiff>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (diff is null)
            {
                throw new InvalidOperationException("record.notFound");
            }

            dbContext.Set<OpportunityApprovalDiff>().Remove(diff);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        private static OpportunityApprovalDiffModel Map(OpportunityApprovalDiff diff) => new()
        {
            Id = diff.Id,
            OpportunityApprovalRequestId = diff.OpportunityApprovalRequestId,
            Field = diff.Field,
            PolicyValue = diff.PolicyValue,
            RequestedValue = diff.RequestedValue,
            Delta = diff.Delta,
            Kind = (int)diff.Kind,
            DisplayOrder = diff.DisplayOrder,
        };
    }
}
