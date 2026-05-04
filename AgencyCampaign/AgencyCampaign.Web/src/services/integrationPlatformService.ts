import { httpClient } from 'archon-ui'
import type {
  IntegrationCategory,
  IntegrationPlatformIntegration,
  IntegrationAttribute,
  Connector,
  ConnectorAttributeValue,
  Pipeline,
  Execution,
  ProcessingQueue,
  CreateConnectorPayload,
  UpdateConnectorPayload,
  ExecutePipelinePayload,
  EnqueuePipelinePayload,
} from '../types/integrationPlatform'

export const integrationPlatformService = {
  async getActiveIntegrationCategories(): Promise<IntegrationCategory[]> {
    const response = await httpClient.get<IntegrationCategory[]>('/IntegrationCategories/active')
    return response.data ?? []
  },

  async getIntegrationsByCategory(categoryId: number): Promise<IntegrationPlatformIntegration[]> {
    const response = await httpClient.get<IntegrationPlatformIntegration[]>(`/IntegrationPlatformProxy/integrations/${categoryId}`)
    return response.data ?? []
  },

  async getIntegrationAttributes(integrationId: number): Promise<IntegrationAttribute[]> {
    const response = await httpClient.get<IntegrationAttribute[]>(`/IntegrationPlatformProxy/integrationattributes/${integrationId}`)
    return response.data ?? []
  },

  async getPipelinesByIntegration(integrationId: number): Promise<Pipeline[]> {
    const response = await httpClient.get<Pipeline[]>(`/IntegrationPlatformProxy/pipelines/${integrationId}`)
    return response.data ?? []
  },

  async getConnectorsByIntegration(integrationId: number): Promise<Connector[]> {
    const response = await httpClient.get<Connector[]>(`/IntegrationPlatformProxy/connectors/${integrationId}`)
    return response.data ?? []
  },

  async getConnectorDetail(connectorId: number): Promise<ConnectorDetail> {
    const response = await httpClient.get<ConnectorDetail>(`/IntegrationPlatformProxy/connectors/detail/${connectorId}`)
    return response.data!
  },

  async createConnector(payload: CreateConnectorPayload): Promise<Connector> {
    const response = await httpClient.post<Connector>('/IntegrationPlatformProxy/connectors', payload)
    return response.data!
  },

  async updateConnector(connectorId: number, payload: UpdateConnectorPayload): Promise<Connector> {
    const response = await httpClient.put<Connector>(`/IntegrationPlatformProxy/connectors/${connectorId}`, payload)
    return response.data!
  },

  async executePipeline(payload: ExecutePipelinePayload): Promise<Execution> {
    const response = await httpClient.post<Execution>('/IntegrationPlatformProxy/executions', payload)
    return response.data!
  },

  async enqueuePipeline(payload: EnqueuePipelinePayload): Promise<ProcessingQueue> {
    const response = await httpClient.post<ProcessingQueue>('/IntegrationPlatformProxy/processingqueues/enqueue', payload)
    return response.data!
  },
}
