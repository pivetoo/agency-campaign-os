using System.Net.Http.Json;
using Archon.Application.Integrations;
using Archon.Application.Services;

namespace AgencyCampaign.Infrastructure.Clients
{
    public sealed class IntegrationPlatformClient
    {
        private const string IntegrationPlatformName = "integration-plataform";

        private readonly HttpClient httpClient;
        private readonly IIntegrationService integrationService;

        public IntegrationPlatformClient(HttpClient httpClient, IIntegrationService integrationService)
        {
            this.httpClient = httpClient;
            this.integrationService = integrationService;
        }

        public async Task<List<IntegrationCategoryDto>> GetActiveIntegrationCategoriesAsync(CancellationToken cancellationToken = default)
        {
            if (!await EnsureConfiguredAsync(cancellationToken)) return [];

            HttpResponseMessage response = await httpClient.GetAsync("api/integrationcategories/active", cancellationToken);
            response.EnsureSuccessStatusCode();

            ApiResponse<List<IntegrationCategoryDto>>? result = await response.Content.ReadFromJsonAsync<ApiResponse<List<IntegrationCategoryDto>>>(cancellationToken);
            return result?.Data ?? [];
        }

        public async Task<List<IntegrationDto>> GetIntegrationsByCategoryAsync(long categoryId, CancellationToken cancellationToken = default)
        {
            if (!await EnsureConfiguredAsync(cancellationToken)) return [];

            HttpResponseMessage response = await httpClient.GetAsync($"api/integrations/category/{categoryId}", cancellationToken);
            response.EnsureSuccessStatusCode();

            ApiResponse<List<IntegrationDto>>? result = await response.Content.ReadFromJsonAsync<ApiResponse<List<IntegrationDto>>>(cancellationToken);
            return result?.Data ?? [];
        }

        public async Task<List<IntegrationAttributeDto>> GetIntegrationAttributesAsync(long integrationId, CancellationToken cancellationToken = default)
        {
            if (!await EnsureConfiguredAsync(cancellationToken)) return [];

            HttpResponseMessage response = await httpClient.GetAsync($"api/integrationattributes/integration/{integrationId}", cancellationToken);
            response.EnsureSuccessStatusCode();

            ApiResponse<List<IntegrationAttributeDto>>? result = await response.Content.ReadFromJsonAsync<ApiResponse<List<IntegrationAttributeDto>>>(cancellationToken);
            return result?.Data ?? [];
        }

        public async Task<List<PipelineDto>> GetPipelinesByIntegrationAsync(long integrationId, CancellationToken cancellationToken = default)
        {
            if (!await EnsureConfiguredAsync(cancellationToken)) return [];

            HttpResponseMessage response = await httpClient.GetAsync($"api/pipelines/integration/{integrationId}", cancellationToken);
            response.EnsureSuccessStatusCode();

            ApiResponse<List<PipelineDto>>? result = await response.Content.ReadFromJsonAsync<ApiResponse<List<PipelineDto>>>(cancellationToken);
            return result?.Data ?? [];
        }

        public async Task<List<ConnectorDto>> GetConnectorsByIntegrationAsync(long integrationId, CancellationToken cancellationToken = default)
        {
            if (!await EnsureConfiguredAsync(cancellationToken)) return [];

            HttpResponseMessage response = await httpClient.GetAsync($"api/connectors/integration/{integrationId}", cancellationToken);
            response.EnsureSuccessStatusCode();

            ApiResponse<List<ConnectorDto>>? result = await response.Content.ReadFromJsonAsync<ApiResponse<List<ConnectorDto>>>(cancellationToken);
            return result?.Data ?? [];
        }

        public async Task<ConnectorDto> GetConnectorByIdAsync(long connectorId, CancellationToken cancellationToken = default)
        {
            if (!await EnsureConfiguredAsync(cancellationToken))
                throw new InvalidOperationException("Integration 'integration-plataform' is not configured.");

            HttpResponseMessage response = await httpClient.GetAsync($"api/connectors/GetById/{connectorId}", cancellationToken);
            response.EnsureSuccessStatusCode();

            ApiResponse<ConnectorDto>? result = await response.Content.ReadFromJsonAsync<ApiResponse<ConnectorDto>>(cancellationToken);
            return result?.Data ?? throw new InvalidOperationException("Failed to get connector.");
        }

        public async Task<List<ConnectorAttributeValueDto>> GetConnectorAttributeValuesAsync(long connectorId, CancellationToken cancellationToken = default)
        {
            if (!await EnsureConfiguredAsync(cancellationToken)) return [];

            HttpResponseMessage response = await httpClient.GetAsync($"api/connectorattributevalues/connector/{connectorId}", cancellationToken);
            response.EnsureSuccessStatusCode();

            ApiResponse<List<ConnectorAttributeValueDto>>? result = await response.Content.ReadFromJsonAsync<ApiResponse<List<ConnectorAttributeValueDto>>>(cancellationToken);
            return result?.Data ?? [];
        }

        public async Task<ConnectorDto> CreateConnectorAsync(CreateConnectorRequest request, CancellationToken cancellationToken = default)
        {
            if (!await EnsureConfiguredAsync(cancellationToken))
                throw new InvalidOperationException("Integration 'integration-plataform' is not configured.");

            HttpResponseMessage response = await httpClient.PostAsJsonAsync("api/connectors/create", request, cancellationToken);
            response.EnsureSuccessStatusCode();

            ApiResponse<ConnectorDto>? result = await response.Content.ReadFromJsonAsync<ApiResponse<ConnectorDto>>(cancellationToken);
            return result?.Data ?? throw new InvalidOperationException("Failed to create connector.");
        }

        public async Task<ConnectorDto> UpdateConnectorAsync(long connectorId, UpdateConnectorRequest request, CancellationToken cancellationToken = default)
        {
            if (!await EnsureConfiguredAsync(cancellationToken))
                throw new InvalidOperationException("Integration 'integration-plataform' is not configured.");

            HttpResponseMessage response = await httpClient.PutAsJsonAsync($"api/connectors/Update/{connectorId}", request, cancellationToken);
            response.EnsureSuccessStatusCode();

            ApiResponse<ConnectorDto>? result = await response.Content.ReadFromJsonAsync<ApiResponse<ConnectorDto>>(cancellationToken);
            return result?.Data ?? throw new InvalidOperationException("Failed to update connector.");
        }

        public async Task<ConnectorAttributeValueDto> CreateConnectorAttributeValueAsync(CreateConnectorAttributeValueRequest request, CancellationToken cancellationToken = default)
        {
            if (!await EnsureConfiguredAsync(cancellationToken))
                throw new InvalidOperationException("Integration 'integration-plataform' is not configured.");

            HttpResponseMessage response = await httpClient.PostAsJsonAsync("api/connectorattributevalues/create", request, cancellationToken);
            response.EnsureSuccessStatusCode();

            ApiResponse<ConnectorAttributeValueDto>? result = await response.Content.ReadFromJsonAsync<ApiResponse<ConnectorAttributeValueDto>>(cancellationToken);
            return result?.Data ?? throw new InvalidOperationException("Failed to create connector attribute value.");
        }

        public async Task<ConnectorAttributeValueDto> UpdateConnectorAttributeValueAsync(long id, UpdateConnectorAttributeValueRequest request, CancellationToken cancellationToken = default)
        {
            if (!await EnsureConfiguredAsync(cancellationToken))
                throw new InvalidOperationException("Integration 'integration-plataform' is not configured.");

            HttpResponseMessage response = await httpClient.PutAsJsonAsync($"api/connectorattributevalues/Update/{id}", request, cancellationToken);
            response.EnsureSuccessStatusCode();

            ApiResponse<ConnectorAttributeValueDto>? result = await response.Content.ReadFromJsonAsync<ApiResponse<ConnectorAttributeValueDto>>(cancellationToken);
            return result?.Data ?? throw new InvalidOperationException("Failed to update connector attribute value.");
        }

        public async Task<ExecutionDto> ExecutePipelineAsync(ExecutePipelineRequest request, CancellationToken cancellationToken = default)
        {
            if (!await EnsureConfiguredAsync(cancellationToken))
                throw new InvalidOperationException("Integration 'integration-plataform' is not configured.");

            HttpResponseMessage response = await httpClient.PostAsJsonAsync("api/executions/execute", request, cancellationToken);
            response.EnsureSuccessStatusCode();

            ApiResponse<ExecutionDto>? result = await response.Content.ReadFromJsonAsync<ApiResponse<ExecutionDto>>(cancellationToken);
            return result?.Data ?? throw new InvalidOperationException("Failed to execute pipeline.");
        }

        public async Task<ProcessingQueueDto> EnqueuePipelineAsync(EnqueuePipelineRequest request, CancellationToken cancellationToken = default)
        {
            if (!await EnsureConfiguredAsync(cancellationToken))
                throw new InvalidOperationException("Integration 'integration-plataform' is not configured.");

            HttpResponseMessage response = await httpClient.PostAsJsonAsync("api/processingqueues/enqueue", request, cancellationToken);
            response.EnsureSuccessStatusCode();

            ApiResponse<ProcessingQueueDto>? result = await response.Content.ReadFromJsonAsync<ApiResponse<ProcessingQueueDto>>(cancellationToken);
            return result?.Data ?? throw new InvalidOperationException("Failed to enqueue pipeline.");
        }

        private async Task<bool> EnsureConfiguredAsync(CancellationToken cancellationToken)
        {
            Integration? integration = await integrationService.GetByNameAsync(IntegrationPlatformName, cancellationToken);
            if (integration is null)
            {
                Console.WriteLine("IntegrationPlatformClient: integration 'integration-plataform' was not found in table 'integrations'.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(integration.BaseUrl))
            {
                Console.WriteLine("IntegrationPlatformClient: integration 'integration-plataform' is configured without baseurl.");
                return false;
            }

            httpClient.BaseAddress = new Uri(integration.BaseUrl, UriKind.Absolute);

            httpClient.DefaultRequestHeaders.Remove("X-Integration-Secret");
            string? secret = integration.GetParameter("IntegrationSecret");
            if (!string.IsNullOrWhiteSpace(secret))
            {
                httpClient.DefaultRequestHeaders.Add("X-Integration-Secret", secret);
            }
            else
            {
                Console.WriteLine("IntegrationPlatformClient: integration 'integration-plataform' is configured without IntegrationSecret.");
            }

            return true;
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
