using AgencyCampaign.Infrastructure.Clients;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.Json;

namespace AgencyCampaign.Api.Controllers
{
    public sealed class IntegrationPlatformProxyController : ApiControllerBase
    {
        private readonly IntegrationPlatformClient integrationPlatformClient;

        public IntegrationPlatformProxyController(IntegrationPlatformClient integrationPlatformClient)
        {
            this.integrationPlatformClient = integrationPlatformClient;
        }

        [RequireAccess("Permite listar integrações disponíveis no IntegrationPlatform por categoria.")]
        [GetEndpoint("integrations/{categoryId:long}")]
        public async Task<IActionResult> GetIntegrationsByCategory(long categoryId, CancellationToken cancellationToken)
        {
            List<IntegrationDto> integrations = await integrationPlatformClient.GetIntegrationsByCategoryAsync(categoryId, cancellationToken);
            return Http200(integrations);
        }

        [RequireAccess("Permite listar atributos configuráveis de uma integração.")]
        [GetEndpoint("integrationattributes/{integrationId:long}")]
        public async Task<IActionResult> GetIntegrationAttributes(long integrationId, CancellationToken cancellationToken)
        {
            List<IntegrationAttributeDto> attributes = await integrationPlatformClient.GetIntegrationAttributesAsync(integrationId, cancellationToken);
            return Http200(attributes);
        }

        [RequireAccess("Permite listar pipelines de uma integração.")]
        [GetEndpoint("pipelines/{integrationId:long}")]
        public async Task<IActionResult> GetPipelinesByIntegration(long integrationId, CancellationToken cancellationToken)
        {
            List<PipelineDto> pipelines = await integrationPlatformClient.GetPipelinesByIntegrationAsync(integrationId, cancellationToken);
            return Http200(pipelines);
        }

        [RequireAccess("Permite listar conectores de uma integração.")]
        [GetEndpoint("connectors/{integrationId:long}")]
        public async Task<IActionResult> GetConnectorsByIntegration(long integrationId, CancellationToken cancellationToken)
        {
            List<ConnectorDto> connectors = await integrationPlatformClient.GetConnectorsByIntegrationAsync(integrationId, cancellationToken);
            return Http200(connectors);
        }

        [RequireAccess("Permite listar todos os conectores ativos do tenant.")]
        [GetEndpoint("connectors/active")]
        public async Task<IActionResult> GetActiveConnectors(CancellationToken cancellationToken)
        {
            List<ConnectorDto> connectors = await integrationPlatformClient.GetActiveConnectorsAsync(cancellationToken);
            return Http200(connectors);
        }

        [RequireAccess("Permite obter os detalhes de um conector do IntegrationPlatform.")]
        [GetEndpoint("connectors/detail/{connectorId:long}")]
        public async Task<IActionResult> GetConnectorDetail(long connectorId, CancellationToken cancellationToken)
        {
            ConnectorDto connector = await integrationPlatformClient.GetConnectorByIdAsync(connectorId, cancellationToken);
            List<ConnectorAttributeValueDto> values = await integrationPlatformClient.GetConnectorAttributeValuesAsync(connectorId, cancellationToken);
            return Http200(new ConnectorDetailContract(connector, values));
        }

        [RequireAccess("Permite criar um conector no IntegrationPlatform.")]
        [PostEndpoint("connectors")]
        public async Task<IActionResult> CreateConnector([FromBody] CreateConnectorPayload payload, CancellationToken cancellationToken)
        {
            ConnectorDto connector = await integrationPlatformClient.CreateConnectorAsync(
                new CreateConnectorRequest
                {
                    IntegrationId = payload.IntegrationId,
                    Name = payload.Name,
                    SystemApplicationId = payload.SystemApplicationId
                },
                cancellationToken);

            if (payload.AttributeValues?.Count > 0)
            {
                foreach (ConnectorAttributeValuePayload attr in payload.AttributeValues)
                {
                    await integrationPlatformClient.CreateConnectorAttributeValueAsync(
                        new CreateConnectorAttributeValueRequest
                        {
                            ConnectorId = connector.Id,
                            IntegrationAttributeId = attr.IntegrationAttributeId,
                            Value = attr.Value
                        },
                        cancellationToken);
                }
            }

            return Http201(connector);
        }

        [RequireAccess("Permite atualizar um conector no IntegrationPlatform.")]
        [PutEndpoint("connectors/{connectorId:long}")]
        public async Task<IActionResult> UpdateConnector(long connectorId, [FromBody] UpdateConnectorPayload payload, CancellationToken cancellationToken)
        {
            ConnectorDto connector = await integrationPlatformClient.UpdateConnectorAsync(
                connectorId,
                new UpdateConnectorRequest
                {
                    Id = connectorId,
                    IntegrationId = payload.IntegrationId,
                    Name = payload.Name,
                    SystemApplicationId = payload.SystemApplicationId,
                    IsActive = payload.IsActive
                },
                cancellationToken);

            if (payload.AttributeValues?.Count > 0)
            {
                List<ConnectorAttributeValueDto> existingValues = await integrationPlatformClient.GetConnectorAttributeValuesAsync(connectorId, cancellationToken);

                foreach (ConnectorAttributeValuePayload attr in payload.AttributeValues)
                {
                    ConnectorAttributeValueDto? existing = existingValues.FirstOrDefault(v => v.IntegrationAttributeId == attr.IntegrationAttributeId);

                    if (existing != null)
                    {
                        await integrationPlatformClient.UpdateConnectorAttributeValueAsync(
                            existing.Id,
                            new UpdateConnectorAttributeValueRequest
                            {
                                Id = existing.Id,
                                IntegrationAttributeId = attr.IntegrationAttributeId,
                                Value = attr.Value
                            },
                            cancellationToken);
                    }
                    else
                    {
                        await integrationPlatformClient.CreateConnectorAttributeValueAsync(
                            new CreateConnectorAttributeValueRequest
                            {
                                ConnectorId = connectorId,
                                IntegrationAttributeId = attr.IntegrationAttributeId,
                                Value = attr.Value
                            },
                            cancellationToken);
                    }
                }
            }

            return Http200(connector);
        }

        [RequireAccess("Permite executar um pipeline no IntegrationPlatform.")]
        [PostEndpoint("executions")]
        public async Task<IActionResult> ExecutePipeline([FromBody] ExecutePipelinePayload payload, CancellationToken cancellationToken)
        {
            ExecutionDto execution = await integrationPlatformClient.ExecutePipelineAsync(
                new ExecutePipelineRequest
                {
                    ConnectorId = payload.ConnectorId,
                    PipelineId = payload.PipelineId,
                    InputData = payload.InputData is not null ? JsonSerializer.Serialize(payload.InputData) : null
                },
                cancellationToken);

            return Http200(execution);
        }

        [RequireAccess("Permite excluir um conector do IntegrationPlatform.")]
        [DeleteEndpoint("connectors/{connectorId:long}")]
        public async Task<IActionResult> DeleteConnector(long connectorId, CancellationToken cancellationToken)
        {
            await integrationPlatformClient.DeleteConnectorAsync(connectorId, cancellationToken);
            return Http200(new { id = connectorId });
        }

        [RequireAccess("Permite ativar ou desativar um conector sem reenviar todas as configuracoes.")]
        [PostEndpoint("connectors/{connectorId:long}/setactive")]
        public async Task<IActionResult> SetConnectorActive(long connectorId, [FromBody] SetConnectorActivePayload payload, CancellationToken cancellationToken)
        {
            ConnectorDto connector = await integrationPlatformClient.GetConnectorByIdAsync(connectorId, cancellationToken);

            ConnectorDto updated = await integrationPlatformClient.UpdateConnectorAsync(
                connectorId,
                new UpdateConnectorRequest
                {
                    Id = connectorId,
                    IntegrationId = connector.IntegrationId,
                    Name = connector.Name,
                    SystemApplicationId = connector.SystemApplicationId,
                    IsActive = payload.IsActive
                },
                cancellationToken);

            return Http200(updated);
        }

        [RequireAccess("Permite testar um conector executando o pipeline de teste correspondente.")]
        [PostEndpoint("connectors/{connectorId:long}/test")]
        public async Task<IActionResult> TestConnector(long connectorId, [FromBody] TestConnectorPayload? payload, CancellationToken cancellationToken)
        {
            ConnectorDto connector;
            try
            {
                connector = await integrationPlatformClient.GetConnectorByIdAsync(connectorId, cancellationToken);
            }
            catch (Exception)
            {
                return Http200(new TestConnectorResult(false, "Conta nao encontrada no IntegrationPlatform.", 0));
            }

            if (!connector.IsActive)
            {
                return Http200(new TestConnectorResult(false, "Conta esta inativa. Ative-a antes de testar.", 0));
            }

            List<PipelineDto> pipelines = await integrationPlatformClient.GetPipelinesByIntegrationAsync(connector.IntegrationId, cancellationToken);
            PipelineDto? testPipeline = pipelines.FirstOrDefault(item => item.IsTestPipeline && item.IsActive);

            if (testPipeline is null)
            {
                return Http200(new TestConnectorResult(false, "Pipeline de teste ainda nao foi cadastrado para esta integracao.", 0));
            }

            Stopwatch stopwatch = Stopwatch.StartNew();
            try
            {
                ExecutionDto execution = await integrationPlatformClient.ExecutePipelineAsync(
                    new ExecutePipelineRequest
                    {
                        ConnectorId = connectorId,
                        PipelineId = testPipeline.Id,
                        InputData = JsonSerializer.Serialize(payload?.InputData ?? new Dictionary<string, object> { ["test"] = true })
                    },
                    cancellationToken);
                stopwatch.Stop();

                bool success = execution.Status == 1 || execution.Status == 2;
                string message = success
                    ? $"Pipeline de teste executado em {stopwatch.ElapsedMilliseconds}ms."
                    : $"Pipeline de teste retornou status {execution.Status}.";

                return Http200(new TestConnectorResult(success, message, stopwatch.ElapsedMilliseconds));
            }
            catch (Exception exception)
            {
                stopwatch.Stop();
                return Http200(new TestConnectorResult(false, $"Falha ao executar teste: {exception.Message}", stopwatch.ElapsedMilliseconds));
            }
        }

        [RequireAccess("Permite enfileirar um pipeline no IntegrationPlatform.")]
        [PostEndpoint("processingqueues/enqueue")]
        public async Task<IActionResult> EnqueuePipeline([FromBody] EnqueuePipelinePayload payload, CancellationToken cancellationToken)
        {
            ProcessingQueueDto queue = await integrationPlatformClient.EnqueuePipelineAsync(
                new EnqueuePipelineRequest
                {
                    ConnectorId = payload.ConnectorId,
                    PipelineId = payload.PipelineId,
                    Payload = payload.Payload,
                    Priority = payload.Priority
                },
                cancellationToken);

            return Http201(queue);
        }
    }

    public sealed class CreateConnectorPayload
    {
        public long IntegrationId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? SystemApplicationId { get; set; }
        public List<ConnectorAttributeValuePayload>? AttributeValues { get; set; }
    }

    public sealed class ConnectorAttributeValuePayload
    {
        public long IntegrationAttributeId { get; set; }
        public string Value { get; set; } = string.Empty;
    }

    public sealed class ExecutePipelinePayload
    {
        public long ConnectorId { get; set; }
        public long PipelineId { get; set; }
        public Dictionary<string, object>? InputData { get; set; }
    }

    public sealed class UpdateConnectorPayload
    {
        public long IntegrationId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? SystemApplicationId { get; set; }
        public bool IsActive { get; set; } = true;
        public List<ConnectorAttributeValuePayload>? AttributeValues { get; set; }
    }

    public sealed class EnqueuePipelinePayload
    {
        public long ConnectorId { get; set; }
        public long PipelineId { get; set; }
        public string? Payload { get; set; }
        public int Priority { get; set; }
    }

    public sealed class ConnectorDetailContract
    {
        public ConnectorDto Connector { get; set; }
        public List<ConnectorAttributeValueDto> AttributeValues { get; set; }

        public ConnectorDetailContract(ConnectorDto connector, List<ConnectorAttributeValueDto> attributeValues)
        {
            Connector = connector;
            AttributeValues = attributeValues;
        }
    }

    public sealed class TestConnectorPayload
    {
        public Dictionary<string, object>? InputData { get; set; }
    }

    public sealed class SetConnectorActivePayload
    {
        public bool IsActive { get; set; }
    }

    public sealed class TestConnectorResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public long LatencyMs { get; set; }

        public TestConnectorResult(bool success, string message, long latencyMs)
        {
            Success = success;
            Message = message;
            LatencyMs = latencyMs;
        }
    }
}
