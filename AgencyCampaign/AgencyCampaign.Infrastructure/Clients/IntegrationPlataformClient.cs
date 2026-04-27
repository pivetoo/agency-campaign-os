using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;

namespace AgencyCampaign.Infrastructure.Clients
{
    public sealed class IntegrationPlataformClient
    {
        private readonly HttpClient httpClient;
        private readonly string integrationSecret;

        public IntegrationPlataformClient(HttpClient httpClient, IConfiguration configuration)
        {
            this.httpClient = httpClient;
            this.integrationSecret = configuration["IntegrationPlataform:IntegrationSecret"] ?? string.Empty;
        }

        public async Task<List<IntegrationCategoryDto>> GetActiveIntegrationCategoriesAsync(CancellationToken cancellationToken = default)
        {
            HttpRequestMessage request = new(HttpMethod.Get, "api/integrationcategories/active");
            request.Headers.Add("X-Integration-Secret", integrationSecret);

            HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            ApiResponse<List<IntegrationCategoryDto>>? result = await response.Content.ReadFromJsonAsync<ApiResponse<List<IntegrationCategoryDto>>>(cancellationToken);

            return result?.Data ?? [];
        }
    }

    public sealed class IntegrationCategoryDto
    {
        public long Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public bool IsActive { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }
    }

    public sealed class ApiResponse<T>
    {
        public string? Message { get; set; }

        public T? Data { get; set; }
    }
}
