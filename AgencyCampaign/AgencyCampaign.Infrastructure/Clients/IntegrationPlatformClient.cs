using AgencyCampaign.Application.Localization;
using Archon.Application.Integrations;
using Archon.Application.Services;
using Archon.Infrastructure.RestApi;
using Microsoft.Extensions.Localization;
using Rest = Archon.Infrastructure.RestApi.RestApi;

namespace AgencyCampaign.Infrastructure.Clients
{
    public sealed class IntegrationPlatformClient
    {
        private const string IntegrationName = "integration-platform";

        private readonly Rest restApi;
        private readonly IIntegrationService integrationService;
        private readonly IStringLocalizer<AgencyCampaignResource> localizer;

        public IntegrationPlatformClient(Rest restApi, IIntegrationService integrationService, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.restApi = restApi;
            this.integrationService = integrationService;
            this.localizer = localizer;
        }

        public async Task<List<IntegrationCategoryDto>> GetActiveIntegrationCategoriesAsync(CancellationToken ct = default)
        {
            (string? baseUrl, string? tenantId, string? secret) = await ResolveIntegrationAsync(ct);
            if (baseUrl is null)
            {
                return [];
            }

            RestResponse<ApiResponse<List<IntegrationCategoryDto>>> response = await restApi.Fetch<ApiResponse<List<IntegrationCategoryDto>>>(
                RestRequest.Get($"{baseUrl}/api/integrationcategories/active").WithTenantApiKey(tenantId, secret!), ct);

            return response.Ok ? response.Data?.Data ?? [] : [];
        }

        public async Task<List<IntegrationDto>> GetIntegrationsByCategoryAsync(long categoryId, CancellationToken ct = default)
        {
            (string? baseUrl, string? tenantId, string? secret) = await ResolveIntegrationAsync(ct);
            if (baseUrl is null)
            {
                return [];
            }

            RestResponse<ApiResponse<List<IntegrationDto>>> response = await restApi.Fetch<ApiResponse<List<IntegrationDto>>>(
                RestRequest.Get($"{baseUrl}/api/integrations/category/{categoryId}").WithTenantApiKey(tenantId, secret!), ct);

            return response.Ok ? response.Data?.Data ?? [] : [];
        }

        public async Task<List<IntegrationAttributeDto>> GetIntegrationAttributesAsync(long integrationId, CancellationToken ct = default)
        {
            (string? baseUrl, string? tenantId, string? secret) = await ResolveIntegrationAsync(ct);
            if (baseUrl is null)
            {
                return [];
            }

            RestResponse<ApiResponse<List<IntegrationAttributeDto>>> response = await restApi.Fetch<ApiResponse<List<IntegrationAttributeDto>>>(
                RestRequest.Get($"{baseUrl}/api/integrationattributes/integration/{integrationId}").WithTenantApiKey(tenantId, secret!), ct);

            return response.Ok ? response.Data?.Data ?? [] : [];
        }

        public async Task<List<PipelineDto>> GetPipelinesByIntegrationAsync(long integrationId, CancellationToken ct = default)
        {
            (string? baseUrl, string? tenantId, string? secret) = await ResolveIntegrationAsync(ct);
            if (baseUrl is null)
            {
                return [];
            }

            RestResponse<ApiResponse<List<PipelineDto>>> response = await restApi.Fetch<ApiResponse<List<PipelineDto>>>(
                RestRequest.Get($"{baseUrl}/api/pipelines/integration/{integrationId}").WithTenantApiKey(tenantId, secret!), ct);

            return response.Ok ? response.Data?.Data ?? [] : [];
        }

        public async Task<List<ConnectorDto>> GetConnectorsByIntegrationAsync(long integrationId, CancellationToken ct = default)
        {
            (string? baseUrl, string? tenantId, string? secret) = await ResolveIntegrationAsync(ct);
            if (baseUrl is null)
            {
                return [];
            }

            RestResponse<ApiResponse<List<ConnectorDto>>> response = await restApi.Fetch<ApiResponse<List<ConnectorDto>>>(
                RestRequest.Get($"{baseUrl}/api/connectors/integration/{integrationId}").WithTenantApiKey(tenantId, secret!), ct);

            return response.Ok ? response.Data?.Data ?? [] : [];
        }

        public async Task<List<ConnectorDto>> GetActiveConnectorsAsync(CancellationToken ct = default)
        {
            (string? baseUrl, string? tenantId, string? secret) = await ResolveIntegrationAsync(ct);
            if (baseUrl is null)
            {
                return [];
            }

            RestResponse<ApiResponse<List<ConnectorDto>>> response = await restApi.Fetch<ApiResponse<List<ConnectorDto>>>(
                RestRequest.Get($"{baseUrl}/api/connectors/active").WithTenantApiKey(tenantId, secret!), ct);

            return response.Ok ? response.Data?.Data ?? [] : [];
        }

        public async Task<ConnectorDto> GetConnectorByIdAsync(long connectorId, CancellationToken ct = default)
        {
            (string? baseUrl, string? tenantId, string? secret) = await ResolveIntegrationAsync(ct);
            if (baseUrl is null)
            {
                throw new InvalidOperationException(localizer["integrationPlatform.notConfigured"]);
            }

            RestResponse<ApiResponse<ConnectorDto>> response = await restApi.Fetch<ApiResponse<ConnectorDto>>(
                RestRequest.Get($"{baseUrl}/api/connectors/GetById/{connectorId}").WithTenantApiKey(tenantId, secret!), ct);

            return response.Ok
                ? response.Data?.Data ?? throw new InvalidOperationException(localizer["integrationPlatform.connector.getFailed"])
                : throw new InvalidOperationException(localizer["integrationPlatform.connector.getFailed"]);
        }

        public async Task<List<ConnectorAttributeValueDto>> GetConnectorAttributeValuesAsync(long connectorId, CancellationToken ct = default)
        {
            (string? baseUrl, string? tenantId, string? secret) = await ResolveIntegrationAsync(ct);
            if (baseUrl is null)
            {
                return [];
            }

            RestResponse<ApiResponse<List<ConnectorAttributeValueDto>>> response = await restApi.Fetch<ApiResponse<List<ConnectorAttributeValueDto>>>(
                RestRequest.Get($"{baseUrl}/api/connectorattributevalues/connector/{connectorId}").WithTenantApiKey(tenantId, secret!), ct);

            return response.Ok ? response.Data?.Data ?? [] : [];
        }

        public async Task<ConnectorDto> CreateConnectorAsync(CreateConnectorRequest request, CancellationToken ct = default)
        {
            (string? baseUrl, string? tenantId, string? secret) = await ResolveIntegrationAsync(ct);
            if (baseUrl is null)
            {
                throw new InvalidOperationException(localizer["integrationPlatform.notConfigured"]);
            }

            RestResponse<ApiResponse<ConnectorDto>> response = await restApi.Fetch<ApiResponse<ConnectorDto>>(
                RestRequest.Post($"{baseUrl}/api/connectors/create", request)
                           .WithTenantApiKey(tenantId, secret!), ct);

            return response.Ok
                ? response.Data?.Data ?? throw new InvalidOperationException(localizer["integrationPlatform.connector.createFailed"])
                : throw new InvalidOperationException(localizer["integrationPlatform.connector.createFailed"]);
        }

        public async Task<ConnectorDto> UpdateConnectorAsync(long connectorId, UpdateConnectorRequest request, CancellationToken ct = default)
        {
            (string? baseUrl, string? tenantId, string? secret) = await ResolveIntegrationAsync(ct);
            if (baseUrl is null)
            {
                throw new InvalidOperationException(localizer["integrationPlatform.notConfigured"]);
            }

            RestResponse<ApiResponse<ConnectorDto>> response = await restApi.Fetch<ApiResponse<ConnectorDto>>(
                RestRequest.Put($"{baseUrl}/api/connectors/Update/{connectorId}", request)
                           .WithTenantApiKey(tenantId, secret!), ct);

            return response.Ok
                ? response.Data?.Data ?? throw new InvalidOperationException(localizer["integrationPlatform.connector.updateFailed"])
                : throw new InvalidOperationException(localizer["integrationPlatform.connector.updateFailed"]);
        }

        public async Task DeleteConnectorAsync(long connectorId, CancellationToken ct = default)
        {
            (string? baseUrl, string? tenantId, string? secret) = await ResolveIntegrationAsync(ct);
            if (baseUrl is null)
            {
                throw new InvalidOperationException(localizer["integrationPlatform.notConfigured"]);
            }

            RestResponse<ApiResponse<object>> response = await restApi.Fetch<ApiResponse<object>>(
                RestRequest.Delete($"{baseUrl}/api/connectors/Delete/{connectorId}").WithTenantApiKey(tenantId, secret!), ct);

            if (!response.Ok)
            {
                throw new InvalidOperationException(localizer["integrationPlatform.connector.deleteFailed"]);
            }
        }

        public async Task<ConnectorAttributeValueDto> CreateConnectorAttributeValueAsync(CreateConnectorAttributeValueRequest request, CancellationToken ct = default)
        {
            (string? baseUrl, string? tenantId, string? secret) = await ResolveIntegrationAsync(ct);
            if (baseUrl is null)
            {
                throw new InvalidOperationException(localizer["integrationPlatform.notConfigured"]);
            }

            RestResponse<ApiResponse<ConnectorAttributeValueDto>> response = await restApi.Fetch<ApiResponse<ConnectorAttributeValueDto>>(
                RestRequest.Post($"{baseUrl}/api/connectorattributevalues/create", request)
                           .WithTenantApiKey(tenantId, secret!), ct);

            return response.Ok
                ? response.Data?.Data ?? throw new InvalidOperationException(localizer["integrationPlatform.connectorAttribute.createFailed"])
                : throw new InvalidOperationException(localizer["integrationPlatform.connectorAttribute.createFailed"]);
        }

        public async Task<ConnectorAttributeValueDto> UpdateConnectorAttributeValueAsync(long id, UpdateConnectorAttributeValueRequest request, CancellationToken ct = default)
        {
            (string? baseUrl, string? tenantId, string? secret) = await ResolveIntegrationAsync(ct);
            if (baseUrl is null)
            {
                throw new InvalidOperationException(localizer["integrationPlatform.notConfigured"]);
            }

            RestResponse<ApiResponse<ConnectorAttributeValueDto>> response = await restApi.Fetch<ApiResponse<ConnectorAttributeValueDto>>(
                RestRequest.Put($"{baseUrl}/api/connectorattributevalues/Update/{id}", request)
                           .WithTenantApiKey(tenantId, secret!), ct);

            return response.Ok
                ? response.Data?.Data ?? throw new InvalidOperationException(localizer["integrationPlatform.connectorAttribute.updateFailed"])
                : throw new InvalidOperationException(localizer["integrationPlatform.connectorAttribute.updateFailed"]);
        }

        public async Task<ExecutionDto> ExecutePipelineAsync(ExecutePipelineRequest request, CancellationToken ct = default)
        {
            (string? baseUrl, string? tenantId, string? secret) = await ResolveIntegrationAsync(ct);
            if (baseUrl is null)
            {
                throw new InvalidOperationException(localizer["integrationPlatform.notConfigured"]);
            }

            RestResponse<ApiResponse<ExecutionDto>> response = await restApi.Fetch<ApiResponse<ExecutionDto>>(
                RestRequest.Post($"{baseUrl}/api/executions/execute", request)
                           .WithTenantApiKey(tenantId, secret!), ct);

            return response.Ok
                ? response.Data?.Data ?? throw new InvalidOperationException(localizer["integrationPlatform.pipeline.executeFailed"])
                : throw new InvalidOperationException(localizer["integrationPlatform.pipeline.executeFailed"]);
        }

        public async Task<ProcessingQueueDto> EnqueuePipelineAsync(EnqueuePipelineRequest request, CancellationToken ct = default)
        {
            (string? baseUrl, string? tenantId, string? secret) = await ResolveIntegrationAsync(ct);
            if (baseUrl is null)
            {
                throw new InvalidOperationException(localizer["integrationPlatform.notConfigured"]);
            }

            RestResponse<ApiResponse<ProcessingQueueDto>> response = await restApi.Fetch<ApiResponse<ProcessingQueueDto>>(
                RestRequest.Post($"{baseUrl}/api/processingqueues/enqueue", request)
                           .WithTenantApiKey(tenantId, secret!), ct);

            return response.Ok
                ? response.Data?.Data ?? throw new InvalidOperationException(localizer["integrationPlatform.pipeline.enqueueFailed"])
                : throw new InvalidOperationException(localizer["integrationPlatform.pipeline.enqueueFailed"]);
        }

        private async Task<(string? baseUrl, string? tenantId, string? apiKey)> ResolveIntegrationAsync(CancellationToken ct)
        {
            Integration? integration = await integrationService.GetByNameAsync(IntegrationName, ct);
            if (integration is null)
            {
                Console.WriteLine("IntegrationPlatformClient: integration 'integration-platform' was not found in table 'integrations'.");
                return (null, null, null);
            }

            if (string.IsNullOrWhiteSpace(integration.BaseUrl))
            {
                Console.WriteLine("IntegrationPlatformClient: integration 'integration-platform' is configured without baseurl.");
                return (null, null, null);
            }

            string? tenantId = integration.GetParameter("TenantId");
            string? apiKey = integration.GetParameter("ApiKey");
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                Console.WriteLine("IntegrationPlatformClient: integration 'integration-platform' is configured without ApiKey.");
            }

            return (integration.BaseUrl, tenantId, apiKey);
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
        public string? IconUrl { get; set; }
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
        public bool IsDefault { get; set; }
        public bool IsTestPipeline { get; set; }
    }

    public sealed class ConnectorDto
    {
        public long Id { get; set; }
        public long IntegrationId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? SystemApplicationId { get; set; }
        public bool IsActive { get; set; }
        public string? WebhookToken { get; set; }
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
        public string? InputData { get; set; }
    }

    public sealed class EnqueuePipelineRequest
    {
        public long ConnectorId { get; set; }
        public long PipelineId { get; set; }
        public string? Payload { get; set; }
        public int Priority { get; set; }
    }
}
