using AgencyCampaign.Application.Catalogs;
using AgencyCampaign.Application.Requests.IntegrationCapabilities;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class IntegrationCapabilityService : IIntegrationCapabilityService
    {
        private readonly DbContext dbContext;

        public IntegrationCapabilityService(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<IReadOnlyList<IntegrationCapability>> GetAll(CancellationToken cancellationToken = default)
        {
            return await dbContext.Set<IntegrationCapability>()
                .AsNoTracking()
                .OrderBy(item => item.IntentKey)
                .ToListAsync(cancellationToken);
        }

        public async Task<IntegrationCapability?> GetByIntent(string intentKey, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(intentKey))
            {
                return null;
            }

            string normalized = intentKey.Trim();
            return await dbContext.Set<IntegrationCapability>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.IntentKey == normalized, cancellationToken);
        }

        public IReadOnlyList<IntegrationIntentDescriptor> GetIntentCatalog()
        {
            return IntegrationIntents.All;
        }

        public async Task<IntegrationCapability> SetCapability(SetIntegrationCapabilityRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(request.IntentKey);

            IntegrationIntentDescriptor? descriptor = IntegrationIntents.Find(request.IntentKey);
            if (descriptor is null)
            {
                throw new InvalidOperationException("integrationCapability.intent.unknown");
            }

            string normalized = descriptor.Key;

            IntegrationCapability? existing = await dbContext.Set<IntegrationCapability>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.IntentKey == normalized, cancellationToken);

            if (existing is not null)
            {
                existing.UpdateConnector(request.ConnectorId);
                if (request.IsActive)
                {
                    existing.Activate();
                }
                else
                {
                    existing.Deactivate();
                }

                await dbContext.SaveChangesAsync(cancellationToken);
                return existing;
            }

            IntegrationCapability created = new(normalized, request.ConnectorId, request.IsActive);
            await dbContext.Set<IntegrationCapability>().AddAsync(created, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            return created;
        }

        public async Task<bool> RemoveCapability(string intentKey, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(intentKey))
            {
                return false;
            }

            string normalized = intentKey.Trim();

            IntegrationCapability? capability = await dbContext.Set<IntegrationCapability>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.IntentKey == normalized, cancellationToken);

            if (capability is null)
            {
                return false;
            }

            dbContext.Set<IntegrationCapability>().Remove(capability);
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<ResolvedCapability> ResolveForExecution(string intentKey, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(intentKey);

            IntegrationIntentDescriptor? descriptor = IntegrationIntents.Find(intentKey);
            if (descriptor is null)
            {
                throw new InvalidOperationException("integrationCapability.intent.unknown");
            }

            IntegrationCapability? capability = await GetByIntent(descriptor.Key, cancellationToken);
            if (capability is null || !capability.IsActive)
            {
                throw new InvalidOperationException("integrationCapability.notConfigured");
            }

            return new ResolvedCapability(descriptor.Key, descriptor.ServiceContractIdentifier, capability.ConnectorId);
        }
    }
}
