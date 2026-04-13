import { httpClient } from 'archon-ui'
import type { CampaignCreator } from '../types/campaignCreator'

const BASE_URL = '/CampaignCreators'

export interface CreateCampaignCreatorRequest {
  campaignId: number
  creatorId: number
  agreedAmount: number
  agencyFeeAmount: number
  notes?: string
  status: number
}

export interface UpdateCampaignCreatorRequest {
  id: number
  agreedAmount: number
  agencyFeeAmount: number
  notes?: string
  status: number
}

export const campaignCreatorService = {
  async getByCampaign(campaignId: number): Promise<CampaignCreator[]> {
    const response = await httpClient.get<CampaignCreator[]>(`${BASE_URL}/campaign/${campaignId}`)
    return response.data ?? []
  },

  create(data: CreateCampaignCreatorRequest) {
    return httpClient.post<CampaignCreator>(`${BASE_URL}/Create`, data)
  },

  update(id: number, data: UpdateCampaignCreatorRequest) {
    return httpClient.put<CampaignCreator>(`${BASE_URL}/${id}`, data)
  },
}
