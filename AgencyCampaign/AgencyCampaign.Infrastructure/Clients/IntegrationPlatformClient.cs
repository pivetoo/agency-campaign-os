using Archon.Application.Integrations;
using Archon.Application.Services;
using Archon.Infrastructure.RestApi;
using Rest = Archon.Infrastructure.RestApi.RestApi;

namespace AgencyCampaign.Infrastructure.Clients
{
    public sealed class IntegrationPlatformClient
    {
        private const string IntegrationName = "integration-platform";

        private readonly Rest restApi;
        private readonly IIntegrationService integrationService;

        public IntegrationPlatformClient(Rest restApi, IIntegrationService integrationService)
        {
            this.restApi = restApi;
            this.integrationService = integrationService;
        }

        public async Task<List<IntegrationCategoryDto>> GetActiveIntegrationCategoriesAsync(CancellationToken ct = default)
        {
            (string? baseUrl, string? secret) = await ResolveIntegrationAsync(ct);
            if (baseUrl is null)
            {
                return [];
            }

            RestResponse<ApiResponse<List<IntegrationCategoryDto>>> response = await restApi.Fetch<ApiResponse<List<IntegrationCategoryDto>>>(
                RestRequest.Get($"{baseUrl}/api/integrationcategories/active").WithSecret(secret!), ct);

            return response.Ok ? response.Data?.Data ?? [] : [];
        }

        public async Task<List<IntegrationDto>> GetIntegrationsByCategoryAsync(long categoryId, CancellationToken ct = default)
        {
            (string? baseUrl, string? secret) = await ResolveIntegrationAsync(ct);
            if (baseUrl is null)
            {
                return [];
            }

            RestResponse<ApiResponse<List<IntegrationDto>>> response = await restApi.Fetch<ApiResponse<List<IntegrationDto>>>(
                RestRequest.Get($"{baseUrl}/api/integrations/category/{categoryId}").WithSecret(secret!), ct);

            return response.Ok ? response.Data?.Data ?? [] : [];
        }

        public async Task<List<IntegrationAttributeDto>> GetIntegrationAttributesAsync(long integrationId, CancellationToken ct = default)
        {
            (string? baseUrl, string? secret) = await ResolveIntegrationAsync(ct);
            if (baseUrl is null)
            {
                return [];
            }

            RestResponse<ApiResponse<List<IntegrationAttributeDto>>> response = await restApi.Fetch<ApiResponse<List<IntegrationAttributeDto>>>(
                RestRequest.Get($"{baseUrl}/api/integrationattributes/integration/{integrationId}").WithSecret(secret!), ct);

            return response.Ok ? response.Data?.Data ?? [] : [];
        }

        public async Task<List<PipelineDto>> GetPipelinesByIntegrationAsync(long integrationId, CancellationToken ct = default)
        {
            (string? baseUrl, string? secret) = await ResolveIntegrationAsync(ct);
            if (baseUrl is null)
            {
                return [];
            }

            RestResponse<ApiResponse<List<PipelineDto>>> response = await restApi.Fetch<ApiResponse<List<PipelineDto>>>(
                RestRequest.Get($"{baseUrl}/api/pipelines/integration/{integrationId}").WithSecret(secret!), ct);

            return response.Ok ? response.Data?.Data ?? [] : [];
        }

        public async Task<List<ConnectorDto>> GetConnectorsByIntegrationAsync(long integrationId, CancellationToken ct = default)
        {
            (string? baseUrl, string? secret) = await ResolveIntegrationAsync(ct);
            if (baseUrl is null)
            {
                return [];
            }

            RestResponse<ApiResponse<List<ConnectorDto>>> response = await restApi.Fetch<ApiResponse<List<ConnectorDto>>>(
                RestRequest.Get($"{baseUrl}/api/connectors/integration/{integrationId}").WithSecret(secret!), ct);

            return response.Ok ? response.Data?.Data ?? [] : [];
        }

        public async Task<ConnectorDto> GetConnectorByIdAsync(long connectorId, CancellationToken ct = default)
        {
            (string? baseUrl, string? secret) = await ResolveIntegrationAsync(ct);
            if (baseUrl is null)
            {
                throw new InvalidOperationException("Integration 'integration-platform' is not configured.");
            }

            RestResponse<ApiResponse<ConnectorDto>> response = await restApi.Fetch<ApiResponse<ConnectorDto>>(
                RestRequest.Get($"{baseUrl}/api/connectors/GetById/{connectorId}").WithSecret(secret!), ct);

            return response.Ok
                ? response.Data?.Data ?? throw new InvalidOperationException("Failed to get connector.")
                : throw new InvalidOperationException("Failed to get connector.");
        }

        public async Task<List<ConnectorAttributeValueDto>> GetConnectorAttributeValuesAsync(long connectorId, CancellationToken ct = default)
        {
            (string? baseUrl, string? secret) = await ResolveIntegrationAsync(ct);
            if (baseUrl is null)
            {
                return [];
            }

            RestResponse<ApiResponse<List<ConnectorAttributeValueDto>>> response = await restApi.Fetch<ApiResponse<List<ConnectorAttributeValueDto>>>(
                RestRequest.Get($"{baseUrl}/api/connectorattributevalues/connector/{connectorId}").WithSecret(secret!), ct);

            return response.Ok ? response.Data?.Data ?? [] : [];
        }

        public async Task<ConnectorDto> CreateConnectorAsync(CreateConnectorRequest request, CancellationToken ct = default)
        {
            (string? baseUrl, string? secret) = await ResolveIntegrationAsync(ct);
            if (baseUrl is null)
            {
                throw new InvalidOperationException("Integration 'integration-platform' is not configured.");
            }

            RestResponse<ApiResponse<ConnectorDto>> response = await restApi.Fetch<ApiResponse<ConnectorDto>>(
                RestRequest.Post($"{baseUrl}/api/connectors/create", request)
                           .WithSecret(secret!), ct);

            return response.Ok
                ? response.Data?.Data ?? throw new InvalidOperationException("Failed to create connector.")
                : throw new InvalidOperationException("Failed to create connector.");
        }

        public async Task<ConnectorDto> UpdateConnectorAsync(long connectorId, UpdateConnectorRequest request, CancellationToken ct = default)
        {
            (string? baseUrl, string? secret) = await ResolveIntegrationAsync(ct);
            if (baseUrl is null)
            {
                throw new InvalidOperationException("Integration 'integration-platform' is not configured.");
            }

            RestResponse<ApiResponse<ConnectorDto>> response = await restApi.Fetch<ApiResponse<ConnectorDto>>(
                RestRequest.Put($"{baseUrl}/api/connectors/Update/{connectorId}", request)
                           .WithSecret(secret!), ct);

            return response.Ok
                ? response.Data?.Data ?? throw new InvalidOperationException("Failed to update connector.")
                : throw new InvalidOperationException("Failed to update connector.");
        }

        public async Task<ConnectorAttributeValueDto> CreateConnectorAttributeValueAsync(CreateConnectorAttributeValueRequest request, CancellationToken ct = default)
        {
            (string? baseUrl, string? secret) = await ResolveIntegrationAsync(ct);
            if (baseUrl is null)
            {
                throw new InvalidOperationException("Integration 'integration-platform' is not configured.");
            }

            RestResponse<ApiResponse<ConnectorAttributeValueDto>> response = await restApi.Fetch<ApiResponse<ConnectorAttributeValueDto>>(
                RestRequest.Post($"{baseUrl}/api/connectorattributevalues/create", request)
                           .WithSecret(secret!), ct);

            return response.Ok
                ? response.Data?.Data ?? throw new InvalidOperationException("Failed to create connector attribute value.")
                : throw new InvalidOperationException("Failed to create connector attribute value.");
        }

        public async Task<ConnectorAttributeValueDto> UpdateConnectorAttributeValueAsync(long id, UpdateConnectorAttributeValueRequest request, CancellationToken ct = default)
        {
            (string? baseUrl, string? secret) = await ResolveIntegrationAsync(ct);
            if (baseUrl is null)
            {
                throw new InvalidOperationException("Integration 'integration-platform' is not configured.");
            }

            RestResponse<ApiResponse<ConnectorAttributeValueDto>> response = await restApi.Fetch<ApiResponse<ConnectorAttributeValueDto>>(
                RestRequest.Put($"{baseUrl}/api/connectorattributevalues/Update/{id}", request)
                           .WithSecret(secret!), ct);

            return response.Ok
                ? response.Data?.Data ?? throw new InvalidOperationException("Failed to update connector attribute value.")
                : throw new InvalidOperationException("Failed to update connector attribute value.");
        }

        public async Task<ExecutionDto> ExecutePipelineAsync(ExecutePipelineRequest request, CancellationToken ct = default)
        {
            (string? baseUrl, string? secret) = await ResolveIntegrationAsync(ct);
            if (baseUrl is null)
            {
                throw new InvalidOperationException("Integration 'integration-platform' is not configured.");
            }

            RestResponse<ApiResponse<ExecutionDto>> response = await restApi.Fetch<ApiResponse<ExecutionDto>>(
                RestRequest.Post($"{baseUrl}/api/executions/execute", request)
                           .WithSecret(secret!), ct);

            return response.Ok
                ? response.Data?.Data ?? throw new InvalidOperationException("Failed to execute pipeline.")
                : throw new InvalidOperationException("Failed to execute pipeline.");
        }

        public async Task<ProcessingQueueDto> EnqueuePipelineAsync(EnqueuePipelineRequest request, CancellationToken ct = default)
        {
            (string? baseUrl, string? secret) = await ResolveIntegrationAsync(ct);
            if (baseUrl is null)
            {
                throw new InvalidOperationException("Integration 'integration-platform' is not configured.");
            }

            RestResponse<ApiResponse<ProcessingQueueDto>> response = await restApi.Fetch<ApiResponse<ProcessingQueueDto>>(
                RestRequest.Post($"{baseUrl}/api/processingqueues/enqueue", request)
                           .WithSecret(secret!), ct);

            return response.Ok
                ? response.Data?.Data ?? throw new InvalidOperationException("Failed to enqueue pipeline.")
                : throw new InvalidOperationException("Failed to enqueue pipeline.");
        }

        private async Task<(string? baseUrl, string? secret)> ResolveIntegrationAsync(CancellationToken ct)
        {
            Integration? integration = await integrationService.GetByNameAsync(IntegrationName, ct);
            if (integration is null)
            {
                Console.WriteLine("IntegrationPlatformClient: integration 'integration-platform' was not found in table 'integrations'.");
                return (null, null);
            }

            if (string.IsNullOrWhiteSpace(integration.BaseUrl))
            {
                Console.WriteLine("IntegrationPlatformClient: integration 'integration-platform' is configured without baseurl.");
                return (null, null);
            }

            string? secret = integration.GetParameter("IntegrationSecret");
            if (string.IsNullOrWhiteSpace(secret))
            {
                Console.WriteLine("IntegrationPlatformClient: integration 'integration-platform' is configured without IntegrationSecret.");
            }

            return (integration.BaseUrl, secret);
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
