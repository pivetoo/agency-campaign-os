import { httpClient } from 'archon-ui'
import type { Integration } from '../types/integration'

const BASE_URL = '/Integrations'

export interface CreateIntegrationRequest {
  identifier: string
  name: string
  description?: string
  categoryId: number
}

export interface UpdateIntegrationRequest extends CreateIntegrationRequest {
  id: number
  isActive: boolean
}

function extractItems<T>(data: T[] | { items?: T[] } | undefined): T[] {
  return Array.isArray(data) ? data : data?.items ?? []
}

export const integrationService = {
  async getAll(): Promise<Integration[]> {
    const response = await httpClient.get<Integration[] | { items?: Integration[] }>(`${BASE_URL}/Get`)
    return extractItems(response.data)
  },

  async getActive(): Promise<Integration[]> {
    const response = await httpClient.get<Integration[]>(`${BASE_URL}/active`)
    return response.data ?? []
  },

  async getById(id: number): Promise<Integration | null> {
    const response = await httpClient.get<Integration>(`${BASE_URL}/${id}`)
    return response.data ?? null
  },

  create(data: CreateIntegrationRequest) {
    return httpClient.post<Integration>(`${BASE_URL}/Create`, data)
  },

  update(id: number, data: UpdateIntegrationRequest) {
    return httpClient.put<Integration>(`${BASE_URL}/Update/${id}`, data)
  },
}
