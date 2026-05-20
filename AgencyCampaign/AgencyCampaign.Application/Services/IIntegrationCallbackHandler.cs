using AgencyCampaign.Application.Requests.IntegrationCallbacks;

namespace AgencyCampaign.Application.Services
{
    public interface IIntegrationCallbackHandler
    {
        string ServiceIdentifier { get; }

        Task HandleAsync(IntegrationCallbackEnvelope envelope, CancellationToken cancellationToken = default);
    }

    public interface IIntegrationCallbackRouter
    {
        Task<IntegrationCallbackRouterResult> RouteAsync(IntegrationCallbackEnvelope envelope, CancellationToken cancellationToken = default);
    }

    public sealed record IntegrationCallbackRouterResult(bool Handled, string? Detail);
}
