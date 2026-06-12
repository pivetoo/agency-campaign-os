import { httpClient, buildPaginationQuery } from 'archon-ui'
import type { ApiResponse } from 'archon-ui'
import type { Platform } from '../types/platform'
import { resolveUploadUrl } from '../lib/uploadUrl'

const BASE_URL = '/Platforms'

export const resolvePlatformLogoUrl = resolveUploadUrl

export interface CreatePlatformRequest {
  name: string
  displayOrder: number
  logoUrl?: string
}

export interface UpdatePlatformRequest extends CreatePlatformRequest {
  id: number
  isActive: boolean
}

export const platformService = {
  getAll(params?: { page?: number; pageSize?: number; search?: string; includeInactive?: boolean }): Promise<ApiResponse<Platform[]>> {
    const query = buildPaginationQuery(params)
    const searchParam = params?.search ? `${query ? '&' : '?'}search=${encodeURIComponent(params.search)}` : ''
    const inactiveParam = params?.includeInactive ? `${query || searchParam ? '&' : '?'}includeInactive=true` : ''
    return httpClient.get<Platform[]>(`${BASE_URL}/Get${query}${searchParam}${inactiveParam}`)
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
