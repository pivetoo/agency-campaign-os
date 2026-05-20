using AgencyCampaign.Application.Catalogs;
using AgencyCampaign.Application.Models;
using AgencyCampaign.Application.Requests.IntegrationCapabilities;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Infrastructure.Clients;
using Microsoft.EntityFrameworkCore;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class IntegrationCapabilityService : IIntegrationCapabilityService
    {
        private readonly DbContext dbContext;
        private readonly IntegrationPlatformClient? integrationPlatformClient;

        public IntegrationCapabilityService(DbContext dbContext, IntegrationPlatformClient? integrationPlatformClient = null)
        {
            this.dbContext = dbContext;
            this.integrationPlatformClient = integrationPlatformClient;
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

        public async Task<List<IntegrationCapabilitySummary>> GetSummary(CancellationToken cancellationToken = default)
        {
            IReadOnlyList<IntegrationIntentDescriptor> catalog = IntegrationIntents.All;

            List<IntegrationCapability> capabilities = await dbContext.Set<IntegrationCapability>()
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            Dictionary<string, List<CapabilityConnectorOption>> connectorsByCategory = [];
            string[] uniqueCategories = catalog.Select(item => item.CategoryIdentifier).Distinct().ToArray();

            foreach (string categoryIdentifier in uniqueCategories)
            {
                if (integrationPlatformClient is null)
                {
                    connectorsByCategory[categoryIdentifier] = [];
                    continue;
                }

                try
                {
                    List<ConnectorDto> connectors = await integrationPlatformClient.GetConnectorsByCategoryIdentifierAsync(categoryIdentifier, cancellationToken);
                    connectorsByCategory[categoryIdentifier] = connectors
                        .Select(connector => new CapabilityConnectorOption
                        {
                            Id = connector.Id,
                            Name = connector.Name,
                            IsActive = connector.IsActive,
                        })
                        .ToList();
                }
                catch
                {
                    connectorsByCategory[categoryIdentifier] = [];
                }
            }

            return catalog.Select(descriptor =>
            {
                IntegrationCapability? capability = capabilities.FirstOrDefault(item => item.IntentKey == descriptor.Key);
                List<CapabilityConnectorOption> available = connectorsByCategory.TryGetValue(descriptor.CategoryIdentifier, out List<CapabilityConnectorOption>? list)
                    ? list
                    : [];

                return new IntegrationCapabilitySummary
                {
                    IntentKey = descriptor.Key,
                    Label = descriptor.Label,
                    CategoryIdentifier = descriptor.CategoryIdentifier,
                    ServiceContractIdentifier = descriptor.ServiceContractIdentifier,
                    ConfiguredConnectorId = capability?.ConnectorId,
                    IsActive = capability?.IsActive ?? false,
                    AvailableConnectors = available,
                };
            }).ToList();
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
