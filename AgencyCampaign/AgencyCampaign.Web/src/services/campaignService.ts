import { httpClient } from 'archon-ui'
import type { Campaign, CampaignSummary } from '../types/campaign'

const BASE_URL = '/Campaigns'

export const campaignService = {
  async getAll(): Promise<Campaign[]> {
    const response = await httpClient.get<Campaign[]>(`${BASE_URL}/Get`)
    return response.data ?? []
  },

  async getSummary(id: number): Promise<CampaignSummary | null> {
    const response = await httpClient.get<CampaignSummary>(`${BASE_URL}/summary/${id}`)
    return response.data ?? null
  },
}
