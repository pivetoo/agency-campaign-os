using Archon.Application.Integrations;
using Archon.Application.Services;
using Archon.Infrastructure.RestApi;
using Rest = Archon.Infrastructure.RestApi.RestApi;

namespace AgencyCampaign.Infrastructure.Clients
{
    public sealed class IdentityUsersClient
    {
        private const string IntegrationName = "identity-management";

        private readonly Rest restApi;
        private readonly IIntegrationService integrationService;

        public IdentityUsersClient(Rest restApi, IIntegrationService integrationService)
        {
            this.restApi = restApi;
            this.integrationService = integrationService;
        }

        public async Task<List<IdentityUserDto>> GetActiveUsersAsync(CancellationToken ct = default)
        {
            (string? baseUrl, string? secret) = await ResolveIntegrationAsync(ct);
            if (baseUrl is null)
            {
                throw new InvalidOperationException("Integration 'identity-management' is not configured.");
            }

            RestResponse<ApiResponse<List<IdentityUserDto>>> response = await restApi.Fetch<ApiResponse<List<IdentityUserDto>>>(
                RestRequest.Get($"{baseUrl}/api/Users/GetActive").WithSecret(secret!), ct);

            if (!response.Ok)
            {
                throw new HttpRequestException($"IdentityManagement /api/Users/GetActive returned {response.Status}");
            }

            return response.Data?.Data ?? [];
        }

        public async Task<IdentityUserDto?> GetUserByIdAsync(long userId, CancellationToken ct = default)
        {
            (string? baseUrl, string? secret) = await ResolveIntegrationAsync(ct);
            if (baseUrl is null)
            {
                throw new InvalidOperationException("Integration 'identity-management' is not configured.");
            }

            RestResponse<ApiResponse<IdentityUserDto>> response = await restApi.Fetch<ApiResponse<IdentityUserDto>>(
                RestRequest.Get($"{baseUrl}/api/Users/GetById/{userId}").WithSecret(secret!), ct);

            if (!response.Ok)
            {
                if (response.Status == 404)
                {
                    return null;
                }

                throw new HttpRequestException($"IdentityManagement /api/Users/GetById/{userId} returned {response.Status}");
            }

            return response.Data?.Data;
        }

        private async Task<(string? baseUrl, string? secret)> ResolveIntegrationAsync(CancellationToken ct)
        {
            Integration? integration = await integrationService.GetByNameAsync(IntegrationName, ct);
            if (integration is null)
            {
                Console.WriteLine("IdentityUsersClient: integration 'identity-management' was not found in table 'integrations'.");
                return (null, null);
            }

            if (string.IsNullOrWhiteSpace(integration.BaseUrl))
            {
                Console.WriteLine("IdentityUsersClient: integration 'identity-management' is configured without baseurl.");
                return (null, null);
            }

            string? secret = integration.GetParameter("IntegrationSecret");
            if (string.IsNullOrWhiteSpace(secret))
            {
                Console.WriteLine("IdentityUsersClient: integration 'identity-management' is configured without IntegrationSecret.");
            }

            return (integration.BaseUrl, secret);
        }
    }

    public sealed class IdentityUserDto
    {
        public long Id { get; set; }

        public string Username { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string? AvatarUrl { get; set; }

        public bool IsActive { get; set; }
    }
}
