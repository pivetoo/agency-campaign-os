import { httpClient } from 'archon-ui'
import type { CampaignBriefing, UpsertCampaignBriefingInput } from '../types/campaignBriefing'

const BASE = '/CampaignBriefing'

export const campaignBriefingService = {
  async getByCampaign(campaignId: number): Promise<CampaignBriefing | null> {
    const response = await httpClient.get<CampaignBriefing | null>(`${BASE}/campaign/${campaignId}`)
    return response.data ?? null
  },

  upsert(campaignId: number, data: UpsertCampaignBriefingInput) {
    return httpClient.put<CampaignBriefing>(`${BASE}/campaign/${campaignId}`, data)
  },
}
