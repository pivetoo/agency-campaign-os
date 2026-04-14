import { httpClient } from 'archon-ui'
import type { Platform } from '../types/platform'

const BASE_URL = '/Platforms'

export interface CreatePlatformRequest {
  name: string
  displayOrder: number
}

export interface UpdatePlatformRequest extends CreatePlatformRequest {
  id: number
  isActive: boolean
}

function extractItems<T>(data: T[] | { items?: T[] } | undefined): T[] {
  return Array.isArray(data) ? data : data?.items ?? []
}

export const platformService = {
  async getAll(): Promise<Platform[]> {
    const response = await httpClient.get<Platform[] | { items?: Platform[] }>(`${BASE_URL}/Get`)
    return extractItems(response.data)
  },

  async getActive(): Promise<Platform[]> {
    const response = await httpClient.get<Platform[]>(`${BASE_URL}/active`)
    return response.data ?? []
  },

  create(data: CreatePlatformRequest) {
    return httpClient.post<Platform>(`${BASE_URL}/Create`, data)
  },

  update(id: number, data: UpdatePlatformRequest) {
    return httpClient.put<Platform>(`${BASE_URL}/Update/${id}`, data)
  },
}
