using AgencyCampaign.Application.Requests.IntegrationCallbacks;
using AgencyCampaign.Application.Services;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class IntegrationCallbackRouter : IIntegrationCallbackRouter
    {
        private readonly IReadOnlyDictionary<string, IIntegrationCallbackHandler> handlers;

        public IntegrationCallbackRouter(IEnumerable<IIntegrationCallbackHandler> handlers)
        {
            this.handlers = handlers
                .GroupBy(item => item.ServiceIdentifier, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        }

        public async Task<IntegrationCallbackRouterResult> RouteAsync(IntegrationCallbackEnvelope envelope, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(envelope.ServiceIdentifier))
            {
                return new IntegrationCallbackRouterResult(false, "missingServiceIdentifier");
            }

            if (!handlers.TryGetValue(envelope.ServiceIdentifier, out IIntegrationCallbackHandler? handler))
            {
                return new IntegrationCallbackRouterResult(false, "noHandlerRegistered");
            }

            await handler.HandleAsync(envelope, cancellationToken);
            return new IntegrationCallbackRouterResult(true, handler.GetType().Name);
        }
    }
}
