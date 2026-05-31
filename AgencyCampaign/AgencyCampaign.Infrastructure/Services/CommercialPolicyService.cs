using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Application.Requests.Commercial;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class CommercialPolicyService : ICommercialPolicyService
    {
        private readonly DbContext dbContext;

        public CommercialPolicyService(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<CommercialPolicyModel?> GetCurrent(CancellationToken cancellationToken = default)
        {
            CommercialPolicy? policy = await dbContext.Set<CommercialPolicy>()
                .AsNoTracking()
                .OrderByDescending(item => item.Id)
                .FirstOrDefaultAsync(cancellationToken);

            return policy is null ? null : Map(policy);
        }

        public async Task<CommercialPolicyModel> Upsert(UpsertCommercialPolicyRequest request, CancellationToken cancellationToken = default)
        {
            CommercialPolicy? existing = await dbContext.Set<CommercialPolicy>()
                .AsTracking()
                .OrderByDescending(item => item.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (existing is null)
            {
                CommercialPolicy created = new(request.MaxDiscountPercent, request.DefaultPaymentTermDays, request.MaxPaymentTermDays, request.Notes);
                dbContext.Set<CommercialPolicy>().Add(created);
                await dbContext.SaveChangesAsync(cancellationToken);
                return Map(created);
            }

            existing.Update(request.MaxDiscountPercent, request.DefaultPaymentTermDays, request.MaxPaymentTermDays, request.Notes);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Map(existing);
        }

        private static CommercialPolicyModel Map(CommercialPolicy policy) => new()
        {
            Id = policy.Id,
            MaxDiscountPercent = policy.MaxDiscountPercent,
            DefaultPaymentTermDays = policy.DefaultPaymentTermDays,
            MaxPaymentTermDays = policy.MaxPaymentTermDays,
            Notes = policy.Notes,
            CreatedAt = policy.CreatedAt,
            UpdatedAt = policy.UpdatedAt,
        };
    }
}
