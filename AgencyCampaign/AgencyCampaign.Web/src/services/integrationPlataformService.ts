import { httpClient } from 'archon-ui'
import type {
  IntegrationCategory,
  IntegrationPlataformIntegration,
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
} from '../types/integrationPlataform'

export const integrationPlataformService = {
  async getActiveIntegrationCategories(): Promise<IntegrationCategory[]> {
    const response = await httpClient.get<IntegrationCategory[]>('/IntegrationCategories/active')
    return response.data ?? []
  },

  async getIntegrationsByCategory(categoryId: number): Promise<IntegrationPlataformIntegration[]> {
    const response = await httpClient.get<IntegrationPlataformIntegration[]>(`/IntegrationPlataformProxy/integrations/${categoryId}`)
    return response.data ?? []
  },

  async getIntegrationAttributes(integrationId: number): Promise<IntegrationAttribute[]> {
    const response = await httpClient.get<IntegrationAttribute[]>(`/IntegrationPlataformProxy/integrationattributes/${integrationId}`)
    return response.data ?? []
  },

  async getPipelinesByIntegration(integrationId: number): Promise<Pipeline[]> {
    const response = await httpClient.get<Pipeline[]>(`/IntegrationPlataformProxy/pipelines/${integrationId}`)
    return response.data ?? []
  },

  async getConnectorsByIntegration(integrationId: number): Promise<Connector[]> {
    const response = await httpClient.get<Connector[]>(`/IntegrationPlataformProxy/connectors/${integrationId}`)
    return response.data ?? []
  },

  async getConnectorDetail(connectorId: number): Promise<ConnectorDetail> {
    const response = await httpClient.get<ConnectorDetail>(`/IntegrationPlataformProxy/connectors/detail/${connectorId}`)
    return response.data!
  },

  async createConnector(payload: CreateConnectorPayload): Promise<Connector> {
    const response = await httpClient.post<Connector>('/IntegrationPlataformProxy/connectors', payload)
    return response.data!
  },

  async updateConnector(connectorId: number, payload: UpdateConnectorPayload): Promise<Connector> {
    const response = await httpClient.put<Connector>(`/IntegrationPlataformProxy/connectors/${connectorId}`, payload)
    return response.data!
  },

  async executePipeline(payload: ExecutePipelinePayload): Promise<Execution> {
    const response = await httpClient.post<Execution>('/IntegrationPlataformProxy/executions', payload)
    return response.data!
  },

  async enqueuePipeline(payload: EnqueuePipelinePayload): Promise<ProcessingQueue> {
    const response = await httpClient.post<ProcessingQueue>('/IntegrationPlataformProxy/processingqueues/enqueue', payload)
    return response.data!
  },
}
