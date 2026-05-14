import { httpClient, buildPaginationQuery } from 'archon-ui'
import type { ApiResponse } from 'archon-ui'
import type { CampaignCreatorStatus } from '../types/campaignCreatorStatus'

const BASE_URL = '/CampaignCreatorStatuses'

export interface CreateCampaignCreatorStatusRequest {
  name: string
  description?: string
  displayOrder: number
  color: string
  isInitial: boolean
  isFinal: boolean
  category: number
  marksAsConfirmed: boolean
}

export interface UpdateCampaignCreatorStatusRequest extends CreateCampaignCreatorStatusRequest {
  id: number
  isActive: boolean
}

export const campaignCreatorStatusService = {
  getAll(params?: { page?: number; pageSize?: number; search?: string; includeInactive?: boolean }): Promise<ApiResponse<CampaignCreatorStatus[]>> {
    const query = buildPaginationQuery(params)
    const searchParam = params?.search ? `${query ? '&' : '?'}search=${encodeURIComponent(params.search)}` : ''
    const inactiveParam = params?.includeInactive ? `${query || searchParam ? '&' : '?'}includeInactive=true` : ''
    return httpClient.get<CampaignCreatorStatus[]>(`${BASE_URL}/Get${query}${searchParam}${inactiveParam}`)
  },

  async getActive(): Promise<CampaignCreatorStatus[]> {
    const response = await httpClient.get<CampaignCreatorStatus[]>(`${BASE_URL}/active`)
    return response.data ?? []
  },

  create(data: CreateCampaignCreatorStatusRequest) {
    return httpClient.post<CampaignCreatorStatus>(`${BASE_URL}/Create`, data)
  },

  update(id: number, data: UpdateCampaignCreatorStatusRequest) {
    return httpClient.put<CampaignCreatorStatus>(`${BASE_URL}/${id}`, data)
  },
}
