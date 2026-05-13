using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace AgencyCampaign.Api.Hubs
{
    [Authorize]
    public sealed class WhatsAppHub : Hub<IWhatsAppHubClient>
    {
    }
}
