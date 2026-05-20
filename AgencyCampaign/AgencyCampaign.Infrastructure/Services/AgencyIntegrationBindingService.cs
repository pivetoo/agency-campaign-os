using AgencyCampaign.Application.Catalogs;
using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Application.Requests.IntegrationBindings;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Archon.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class AgencyIntegrationBindingService : IAgencyIntegrationBindingService
    {
        private readonly DbContext dbContext;
        private readonly ICurrentUser currentUser;

        public AgencyIntegrationBindingService(DbContext dbContext, ICurrentUser currentUser)
        {
            this.dbContext = dbContext;
            this.currentUser = currentUser;
        }

        public async Task<IReadOnlyCollection<AgencyIntegrationBindingModel>> GetAll(CancellationToken cancellationToken = default)
        {
            List<AgencyIntegrationBinding> bindings = await dbContext.Set<AgencyIntegrationBinding>()
                .AsNoTracking()
                .OrderBy(item => item.IntentKey)
                .ToListAsync(cancellationToken);

            return bindings.Select(Map).ToArray();
        }

        public async Task<AgencyIntegrationBindingModel?> GetByIntentKey(string intentKey, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(intentKey))
            {
                return null;
            }

            string normalized = intentKey.Trim();
            AgencyIntegrationBinding? binding = await dbContext.Set<AgencyIntegrationBinding>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.IntentKey == normalized, cancellationToken);

            return binding is null ? null : Map(binding);
        }

        public async Task<AgencyIntegrationBindingModel> Save(SaveAgencyIntegrationBindingRequest request, CancellationToken cancellationToken = default)
        {
            string normalized = request.IntentKey.Trim();

            AgencyIntegrationBinding? existing = await dbContext.Set<AgencyIntegrationBinding>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.IntentKey == normalized, cancellationToken);

            if (existing is null)
            {
                AgencyIntegrationBinding created = new(normalized, request.ConnectorId, request.PipelineId, currentUser.UserId, currentUser.UserName);
                if (!request.IsActive)
                {
                    created.Deactivate();
                }

                dbContext.Set<AgencyIntegrationBinding>().Add(created);
                await dbContext.SaveChangesAsync(cancellationToken);
                return Map(created);
            }

            existing.UpdateTargets(request.ConnectorId, request.PipelineId);
            if (request.IsActive)
            {
                existing.Activate();
            }
            else
            {
                existing.Deactivate();
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            return Map(existing);
        }

        public async Task<bool> DeleteByIntentKey(string intentKey, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(intentKey))
            {
                return false;
            }

            string normalized = intentKey.Trim();
            AgencyIntegrationBinding? existing = await dbContext.Set<AgencyIntegrationBinding>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.IntentKey == normalized, cancellationToken);

            if (existing is null)
            {
                return false;
            }

            dbContext.Set<AgencyIntegrationBinding>().Remove(existing);
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }

        public IReadOnlyList<IntegrationIntentDescriptor> GetCatalog()
        {
            return IntegrationIntents.All;
        }

        private static AgencyIntegrationBindingModel Map(AgencyIntegrationBinding binding) => new()
        {
            Id = binding.Id,
            IntentKey = binding.IntentKey,
            ConnectorId = binding.ConnectorId,
            PipelineId = binding.PipelineId,
            IsActive = binding.IsActive,
            CreatedByUserName = binding.CreatedByUserName,
            CreatedAt = binding.CreatedAt,
            UpdatedAt = binding.UpdatedAt
        };
    }
}
