import { httpClient } from 'archon-ui'
import type { IntegrationLog } from '../types/integration'

const BASE_URL = '/IntegrationLogs'

function extractItems<T>(data: T[] | { items?: T[] } | undefined): T[] {
  return Array.isArray(data) ? data : data?.items ?? []
}

export const integrationLogService = {
  async getAll(): Promise<IntegrationLog[]> {
    const response = await httpClient.get<IntegrationLog[] | { items?: IntegrationLog[] }>(`${BASE_URL}/Get`)
    return extractItems(response.data)
  },

  async getByPipeline(integrationPipelineId: number): Promise<IntegrationLog[]> {
    const response = await httpClient.get<IntegrationLog[]>(`${BASE_URL}/by-pipeline/${integrationPipelineId}`)
    return response.data ?? []
  },

  async getById(id: number): Promise<IntegrationLog | null> {
    const response = await httpClient.get<IntegrationLog>(`${BASE_URL}/${id}`)
    return response.data ?? null
  },
}
