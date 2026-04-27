import { httpClient } from 'archon-ui'
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
}

export interface UpdateCampaignCreatorStatusRequest extends CreateCampaignCreatorStatusRequest {
  id: number
  isActive: boolean
}

function extractItems<T>(data: T[] | { items?: T[] } | undefined): T[] {
  return Array.isArray(data) ? data : data?.items ?? []
}

export const campaignCreatorStatusService = {
  async getAll(): Promise<CampaignCreatorStatus[]> {
    const response = await httpClient.get<CampaignCreatorStatus[] | { items?: CampaignCreatorStatus[] }>(`${BASE_URL}/Get`)
    return extractItems(response.data)
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
