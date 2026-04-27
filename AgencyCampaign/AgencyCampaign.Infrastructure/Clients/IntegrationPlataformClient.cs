using System.Net.Http.Json;

namespace AgencyCampaign.Infrastructure.Clients
{
    public sealed class IntegrationPlataformClient
    {
        private readonly HttpClient httpClient;

        public IntegrationPlataformClient(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        public async Task<List<IntegrationCategoryDto>> GetActiveIntegrationCategoriesAsync(CancellationToken cancellationToken = default)
        {
            HttpResponseMessage response = await httpClient.GetAsync("api/integrationcategories/active", cancellationToken);
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
