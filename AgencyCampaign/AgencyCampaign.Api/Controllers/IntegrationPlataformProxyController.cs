using AgencyCampaign.Infrastructure.Clients;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace AgencyCampaign.Api.Controllers
{
    public sealed class IntegrationPlataformProxyController : ApiControllerBase
    {
        private readonly IntegrationPlataformClient integrationPlataformClient;

        public IntegrationPlataformProxyController(IntegrationPlataformClient integrationPlataformClient)
        {
            this.integrationPlataformClient = integrationPlataformClient;
        }

        [RequireAccess("Permite listar integrações disponíveis no IntegrationPlataform por categoria.")]
        [GetEndpoint("integrations/{categoryId:long}")]
        public async Task<IActionResult> GetIntegrationsByCategory(long categoryId, CancellationToken cancellationToken)
        {
            List<IntegrationDto> integrations = await integrationPlataformClient.GetIntegrationsByCategoryAsync(categoryId, cancellationToken);
            return Http200(integrations);
        }

        [RequireAccess("Permite listar atributos configuráveis de uma integração.")]
        [GetEndpoint("integrationattributes/{integrationId:long}")]
        public async Task<IActionResult> GetIntegrationAttributes(long integrationId, CancellationToken cancellationToken)
        {
            List<IntegrationAttributeDto> attributes = await integrationPlataformClient.GetIntegrationAttributesAsync(integrationId, cancellationToken);
            return Http200(attributes);
        }

        [RequireAccess("Permite listar pipelines de uma integração.")]
        [GetEndpoint("pipelines/{integrationId:long}")]
        public async Task<IActionResult> GetPipelinesByIntegration(long integrationId, CancellationToken cancellationToken)
        {
            List<PipelineDto> pipelines = await integrationPlataformClient.GetPipelinesByIntegrationAsync(integrationId, cancellationToken);
            return Http200(pipelines);
        }

        [RequireAccess("Permite listar conectores de uma integração.")]
        [GetEndpoint("connectors/{integrationId:long}")]
        public async Task<IActionResult> GetConnectorsByIntegration(long integrationId, CancellationToken cancellationToken)
        {
            List<ConnectorDto> connectors = await integrationPlataformClient.GetConnectorsByIntegrationAsync(integrationId, cancellationToken);
            return Http200(connectors);
        }

        [RequireAccess("Permite obter os detalhes de um conector do IntegrationPlataform.")]
        [GetEndpoint("connectors/detail/{connectorId:long}")]
        public async Task<IActionResult> GetConnectorDetail(long connectorId, CancellationToken cancellationToken)
        {
            ConnectorDto connector = await integrationPlataformClient.GetConnectorByIdAsync(connectorId, cancellationToken);
            List<ConnectorAttributeValueDto> values = await integrationPlataformClient.GetConnectorAttributeValuesAsync(connectorId, cancellationToken);
            return Http200(new ConnectorDetailContract(connector, values));
        }

        [RequireAccess("Permite criar um conector no IntegrationPlataform.")]
        [PostEndpoint("connectors")]
        public async Task<IActionResult> CreateConnector([FromBody] CreateConnectorPayload payload, CancellationToken cancellationToken)
        {
            ConnectorDto connector = await integrationPlataformClient.CreateConnectorAsync(
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
                    await integrationPlataformClient.CreateConnectorAttributeValueAsync(
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

        [RequireAccess("Permite atualizar um conector no IntegrationPlataform.")]
        [PutEndpoint("connectors/{connectorId:long}")]
        public async Task<IActionResult> UpdateConnector(long connectorId, [FromBody] UpdateConnectorPayload payload, CancellationToken cancellationToken)
        {
            ConnectorDto connector = await integrationPlataformClient.UpdateConnectorAsync(
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
                List<ConnectorAttributeValueDto> existingValues = await integrationPlataformClient.GetConnectorAttributeValuesAsync(connectorId, cancellationToken);

                foreach (ConnectorAttributeValuePayload attr in payload.AttributeValues)
                {
                    ConnectorAttributeValueDto? existing = existingValues.FirstOrDefault(v => v.IntegrationAttributeId == attr.IntegrationAttributeId);

                    if (existing != null)
                    {
                        await integrationPlataformClient.UpdateConnectorAttributeValueAsync(
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
                        await integrationPlataformClient.CreateConnectorAttributeValueAsync(
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

        [RequireAccess("Permite executar um pipeline no IntegrationPlataform.")]
        [PostEndpoint("executions")]
        public async Task<IActionResult> ExecutePipeline([FromBody] ExecutePipelinePayload payload, CancellationToken cancellationToken)
        {
            ExecutionDto execution = await integrationPlataformClient.ExecutePipelineAsync(
                new ExecutePipelineRequest
                {
                    ConnectorId = payload.ConnectorId,
                    PipelineId = payload.PipelineId,
                    InputData = payload.InputData
                },
                cancellationToken);

            return Http200(execution);
        }

        [RequireAccess("Permite enfileirar um pipeline no IntegrationPlataform.")]
        [PostEndpoint("processingqueues/enqueue")]
        public async Task<IActionResult> EnqueuePipeline([FromBody] EnqueuePipelinePayload payload, CancellationToken cancellationToken)
        {
            ProcessingQueueDto queue = await integrationPlataformClient.EnqueuePipelineAsync(
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
}
