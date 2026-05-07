namespace AgencyCampaign.Application.Services
{
    public interface IAutomationDispatcher
    {
        Task DispatchAsync(string trigger, IDictionary<string, object?> payload, CancellationToken cancellationToken = default);
    }
}
