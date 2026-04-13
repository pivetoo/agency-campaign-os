import { httpClient } from 'archon-ui'
import type { CampaignFinancialEntry } from '../types/campaignFinancialEntry'

const BASE_URL = '/CampaignFinancialEntries'

export interface CreateCampaignFinancialEntryRequest {
  campaignId: number
  campaignDeliverableId?: number
  type: number
  description: string
  amount: number
  dueAt: string
  paidAt?: string
  status: number
  counterpartyName?: string
  notes?: string
}

export interface UpdateCampaignFinancialEntryRequest extends Omit<CreateCampaignFinancialEntryRequest, 'campaignId'> {
  id: number
}

export const campaignFinancialEntryService = {
  async getAll(): Promise<CampaignFinancialEntry[]> {
    const response = await httpClient.get<CampaignFinancialEntry[]>(`${BASE_URL}/Get`)
    return response.data ?? []
  },

  async getByCampaign(campaignId: number): Promise<CampaignFinancialEntry[]> {
    const response = await httpClient.get<CampaignFinancialEntry[]>(`${BASE_URL}/campaign/${campaignId}`)
    return response.data ?? []
  },

  create(data: CreateCampaignFinancialEntryRequest) {
    return httpClient.post<CampaignFinancialEntry>(`${BASE_URL}/Create`, data)
  },

  update(id: number, data: UpdateCampaignFinancialEntryRequest) {
    return httpClient.put<CampaignFinancialEntry>(`${BASE_URL}/Update/${id}`, data)
  },
}
