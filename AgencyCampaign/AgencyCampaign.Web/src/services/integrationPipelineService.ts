import { httpClient } from 'archon-ui'
import type { IntegrationPipeline } from '../types/integration'

const BASE_URL = '/IntegrationPipelines'

export interface CreateIntegrationPipelineRequest {
  integrationId: number
  identifier: string
  name: string
  description?: string
}

export interface UpdateIntegrationPipelineRequest extends CreateIntegrationPipelineRequest {
  id: number
  isActive: boolean
}

function extractItems<T>(data: T[] | { items?: T[] } | undefined): T[] {
  return Array.isArray(data) ? data : data?.items ?? []
}

export const integrationPipelineService = {
  async getAll(): Promise<IntegrationPipeline[]> {
    const response = await httpClient.get<IntegrationPipeline[] | { items?: IntegrationPipeline[] }>(`${BASE_URL}/Get`)
    return extractItems(response.data)
  },

  async getActive(): Promise<IntegrationPipeline[]> {
    const response = await httpClient.get<IntegrationPipeline[]>(`${BASE_URL}/active`)
    return response.data ?? []
  },

  async getByIntegration(integrationId: number): Promise<IntegrationPipeline[]> {
    const response = await httpClient.get<IntegrationPipeline[]>(`${BASE_URL}/by-integration/${integrationId}`)
    return response.data ?? []
  },

  async getById(id: number): Promise<IntegrationPipeline | null> {
    const response = await httpClient.get<IntegrationPipeline>(`${BASE_URL}/${id}`)
    return response.data ?? null
  },

  create(data: CreateIntegrationPipelineRequest) {
    return httpClient.post<IntegrationPipeline>(`${BASE_URL}/Create`, data)
  },

  update(id: number, data: UpdateIntegrationPipelineRequest) {
    return httpClient.put<IntegrationPipeline>(`${BASE_URL}/Update/${id}`, data)
  },
}
