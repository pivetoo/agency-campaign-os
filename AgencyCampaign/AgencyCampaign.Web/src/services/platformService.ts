import { httpClient, buildPaginationQuery } from 'archon-ui'
import type { ApiResponse } from 'archon-ui'
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

export const platformService = {
  getAll(params?: { page?: number; pageSize?: number; search?: string }): Promise<ApiResponse<Platform[]>> {
    const query = buildPaginationQuery(params)
    const searchParam = params?.search ? `${query ? '&' : '?'}search=${encodeURIComponent(params.search)}` : ''
    return httpClient.get<Platform[]>(`${BASE_URL}/Get${query}${searchParam}`)
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
