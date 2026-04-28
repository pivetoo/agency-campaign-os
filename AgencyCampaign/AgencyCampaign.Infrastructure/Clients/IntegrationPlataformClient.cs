using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;

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
            HttpResponseMessage response = await SendAsync(HttpMethod.Get, "api/integrationcategories/active", cancellationToken);
            response.EnsureSuccessStatusCode();

            ApiResponse<List<IntegrationCategoryDto>>? result = await response.Content.ReadFromJsonAsync<ApiResponse<List<IntegrationCategoryDto>>>(cancellationToken);
            return result?.Data ?? [];
        }

        public async Task<List<IntegrationDto>> GetIntegrationsByCategoryAsync(long categoryId, CancellationToken cancellationToken = default)
        {
            HttpResponseMessage response = await SendAsync(HttpMethod.Get, $"api/integrations/category/{categoryId}", cancellationToken);
            response.EnsureSuccessStatusCode();

            ApiResponse<List<IntegrationDto>>? result = await response.Content.ReadFromJsonAsync<ApiResponse<List<IntegrationDto>>>(cancellationToken);
            return result?.Data ?? [];
        }

        public async Task<List<IntegrationAttributeDto>> GetIntegrationAttributesAsync(long integrationId, CancellationToken cancellationToken = default)
        {
            HttpResponseMessage response = await SendAsync(HttpMethod.Get, $"api/integrationattributes/integration/{integrationId}", cancellationToken);
            response.EnsureSuccessStatusCode();

            ApiResponse<List<IntegrationAttributeDto>>? result = await response.Content.ReadFromJsonAsync<ApiResponse<List<IntegrationAttributeDto>>>(cancellationToken);
            return result?.Data ?? [];
        }

        public async Task<List<PipelineDto>> GetPipelinesByIntegrationAsync(long integrationId, CancellationToken cancellationToken = default)
        {
            HttpResponseMessage response = await SendAsync(HttpMethod.Get, $"api/pipelines/integration/{integrationId}", cancellationToken);
            response.EnsureSuccessStatusCode();

            ApiResponse<List<PipelineDto>>? result = await response.Content.ReadFromJsonAsync<ApiResponse<List<PipelineDto>>>(cancellationToken);
            return result?.Data ?? [];
        }

        public async Task<List<ConnectorDto>> GetConnectorsByIntegrationAsync(long integrationId, CancellationToken cancellationToken = default)
        {
            HttpResponseMessage response = await SendAsync(HttpMethod.Get, $"api/connectors/integration/{integrationId}", cancellationToken);
            response.EnsureSuccessStatusCode();

            ApiResponse<List<ConnectorDto>>? result = await response.Content.ReadFromJsonAsync<ApiResponse<List<ConnectorDto>>>(cancellationToken);
            return result?.Data ?? [];
        }

        public async Task<ConnectorDto> GetConnectorByIdAsync(long connectorId, CancellationToken cancellationToken = default)
        {
            HttpResponseMessage response = await SendAsync(HttpMethod.Get, $"api/connectors/GetById/{connectorId}", cancellationToken);
            response.EnsureSuccessStatusCode();

            ApiResponse<ConnectorDto>? result = await response.Content.ReadFromJsonAsync<ApiResponse<ConnectorDto>>(cancellationToken);
            return result?.Data ?? throw new InvalidOperationException("Failed to get connector.");
        }

        public async Task<List<ConnectorAttributeValueDto>> GetConnectorAttributeValuesAsync(long connectorId, CancellationToken cancellationToken = default)
        {
            HttpResponseMessage response = await SendAsync(HttpMethod.Get, $"api/connectorattributevalues/connector/{connectorId}", cancellationToken);
            response.EnsureSuccessStatusCode();

            ApiResponse<List<ConnectorAttributeValueDto>>? result = await response.Content.ReadFromJsonAsync<ApiResponse<List<ConnectorAttributeValueDto>>>(cancellationToken);
            return result?.Data ?? [];
        }

        public async Task<ConnectorDto> CreateConnectorAsync(CreateConnectorRequest request, CancellationToken cancellationToken = default)
        {
            HttpResponseMessage response = await SendAsync(HttpMethod.Post, "api/connectors/create", request, cancellationToken);
            response.EnsureSuccessStatusCode();

            ApiResponse<ConnectorDto>? result = await response.Content.ReadFromJsonAsync<ApiResponse<ConnectorDto>>(cancellationToken);
            return result?.Data ?? throw new InvalidOperationException("Failed to create connector.");
        }

        public async Task<ConnectorDto> UpdateConnectorAsync(long connectorId, UpdateConnectorRequest request, CancellationToken cancellationToken = default)
        {
            HttpResponseMessage response = await SendAsync(HttpMethod.Put, $"api/connectors/Update/{connectorId}", request, cancellationToken);
            response.EnsureSuccessStatusCode();

            ApiResponse<ConnectorDto>? result = await response.Content.ReadFromJsonAsync<ApiResponse<ConnectorDto>>(cancellationToken);
            return result?.Data ?? throw new InvalidOperationException("Failed to update connector.");
        }

        public async Task<ConnectorAttributeValueDto> CreateConnectorAttributeValueAsync(CreateConnectorAttributeValueRequest request, CancellationToken cancellationToken = default)
        {
            HttpResponseMessage response = await SendAsync(HttpMethod.Post, "api/connectorattributevalues/create", request, cancellationToken);
            response.EnsureSuccessStatusCode();

            ApiResponse<ConnectorAttributeValueDto>? result = await response.Content.ReadFromJsonAsync<ApiResponse<ConnectorAttributeValueDto>>(cancellationToken);
            return result?.Data ?? throw new InvalidOperationException("Failed to create connector attribute value.");
        }

        public async Task<ConnectorAttributeValueDto> UpdateConnectorAttributeValueAsync(long id, UpdateConnectorAttributeValueRequest request, CancellationToken cancellationToken = default)
        {
            HttpResponseMessage response = await SendAsync(HttpMethod.Put, $"api/connectorattributevalues/Update/{id}", request, cancellationToken);
            response.EnsureSuccessStatusCode();

            ApiResponse<ConnectorAttributeValueDto>? result = await response.Content.ReadFromJsonAsync<ApiResponse<ConnectorAttributeValueDto>>(cancellationToken);
            return result?.Data ?? throw new InvalidOperationException("Failed to update connector attribute value.");
        }

        public async Task<ExecutionDto> ExecutePipelineAsync(ExecutePipelineRequest request, CancellationToken cancellationToken = default)
        {
            HttpResponseMessage response = await SendAsync(HttpMethod.Post, "api/executions/execute", request, cancellationToken);
            response.EnsureSuccessStatusCode();

            ApiResponse<ExecutionDto>? result = await response.Content.ReadFromJsonAsync<ApiResponse<ExecutionDto>>(cancellationToken);
            return result?.Data ?? throw new InvalidOperationException("Failed to execute pipeline.");
        }

        public async Task<ProcessingQueueDto> EnqueuePipelineAsync(EnqueuePipelineRequest request, CancellationToken cancellationToken = default)
        {
            HttpResponseMessage response = await SendAsync(HttpMethod.Post, "api/processingqueues/enqueue", request, cancellationToken);
            response.EnsureSuccessStatusCode();

            ApiResponse<ProcessingQueueDto>? result = await response.Content.ReadFromJsonAsync<ApiResponse<ProcessingQueueDto>>(cancellationToken);
            return result?.Data ?? throw new InvalidOperationException("Failed to enqueue pipeline.");
        }

        private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, CancellationToken cancellationToken)
        {
            HttpRequestMessage request = new(method, path);
            request.Headers.Add("X-Integration-Secret", integrationSecret);
            return await httpClient.SendAsync(request, cancellationToken);
        }

        private async Task<HttpResponseMessage> SendAsync<T>(HttpMethod method, string path, T body, CancellationToken cancellationToken)
        {
            HttpRequestMessage request = new(method, path)
            {
                Content = JsonContent.Create(body)
            };
            request.Headers.Add("X-Integration-Secret", integrationSecret);
            return await httpClient.SendAsync(request, cancellationToken);
        }
    }

    public sealed class ApiResponse<T>
    {
        public string? Message { get; set; }
        public T? Data { get; set; }
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

    public sealed class IntegrationDto
    {
        public long Id { get; set; }
        public string Identifier { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }

    public sealed class IntegrationAttributeDto
    {
        public long Id { get; set; }
        public long IntegrationId { get; set; }
        public string Field { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Placeholder { get; set; }
        public int Type { get; set; }
        public string? DefaultValue { get; set; }
        public bool IsRequired { get; set; }
        public int Order { get; set; }
        public string? Group { get; set; }
        public bool IsSensitive { get; set; }
    }

    public sealed class PipelineDto
    {
        public long Id { get; set; }
        public long IntegrationId { get; set; }
        public string Identifier { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }

    public sealed class ConnectorDto
    {
        public long Id { get; set; }
        public long IntegrationId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? SystemApplicationId { get; set; }
        public bool IsActive { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    public sealed class ConnectorAttributeValueDto
    {
        public long Id { get; set; }
        public long ConnectorId { get; set; }
        public long IntegrationAttributeId { get; set; }
        public string Value { get; set; } = string.Empty;
    }

    public sealed class ExecutionDto
    {
        public long Id { get; set; }
        public long? ConnectorId { get; set; }
        public long? PipelineId { get; set; }
        public int Status { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    public sealed class ProcessingQueueDto
    {
        public long Id { get; set; }
        public long ConnectorId { get; set; }
        public long PipelineId { get; set; }
        public int Priority { get; set; }
        public int Status { get; set; }
    }

    public sealed class CreateConnectorRequest
    {
        public long IntegrationId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? SystemApplicationId { get; set; }
    }

    public sealed class UpdateConnectorRequest
    {
        public long Id { get; set; }
        public long IntegrationId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? SystemApplicationId { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public sealed class CreateConnectorAttributeValueRequest
    {
        public long ConnectorId { get; set; }
        public long IntegrationAttributeId { get; set; }
        public string Value { get; set; } = string.Empty;
    }

    public sealed class UpdateConnectorAttributeValueRequest
    {
        public long Id { get; set; }
        public long IntegrationAttributeId { get; set; }
        public string Value { get; set; } = string.Empty;
    }

    public sealed class ExecutePipelineRequest
    {
        public long ConnectorId { get; set; }
        public long PipelineId { get; set; }
        public Dictionary<string, object>? InputData { get; set; }
    }

    public sealed class EnqueuePipelineRequest
    {
        public long ConnectorId { get; set; }
        public long PipelineId { get; set; }
        public string? Payload { get; set; }
        public int Priority { get; set; }
    }
}
