import { httpClient } from 'archon-ui'
import type { CampaignDeliverable } from '../types/campaignDeliverable'

const BASE_URL = '/CampaignDeliverables'

export interface CreateCampaignDeliverableRequest {
  campaignId: number
  creatorId: number
  title: string
  description?: string
  dueAt: string
  publishedAt?: string
  status: number
  grossAmount: number
  creatorAmount: number
  agencyFeeAmount: number
}

export interface UpdateCampaignDeliverableRequest {
  id: number
  title: string
  description?: string
  dueAt: string
  publishedAt?: string
  status: number
  grossAmount: number
  creatorAmount: number
  agencyFeeAmount: number
}

export const campaignDeliverableService = {
  async getByCampaign(campaignId: number): Promise<CampaignDeliverable[]> {
    const response = await httpClient.get<CampaignDeliverable[]>(`${BASE_URL}/campaign/${campaignId}`)
    return response.data ?? []
  },

  create(data: CreateCampaignDeliverableRequest) {
    return httpClient.post<CampaignDeliverable>(`${BASE_URL}/Create`, data)
  },

  update(id: number, data: UpdateCampaignDeliverableRequest) {
    return httpClient.put<CampaignDeliverable>(`${BASE_URL}/Update/${id}`, data)
  },
}
