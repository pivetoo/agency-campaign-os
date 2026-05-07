import { httpClient } from 'archon-ui'
import type { Campaign, CampaignSummary } from '../types/campaign'

const BASE_URL = '/Campaigns'

export interface CreateCampaignRequest {
  brandId: number
  name: string
  description?: string
  objective?: string
  briefing?: string
  budget: number
  startsAt: string
  endsAt?: string
  commercialResponsibleId?: number
  notes?: string
  status: number
}

export interface UpdateCampaignRequest extends CreateCampaignRequest {
  id: number
  isActive: boolean
}

export const campaignService = {
  async getAll(): Promise<Campaign[]> {
    const response = await httpClient.get<Campaign[]>(`${BASE_URL}/Get`)
    return response.data ?? []
  },

  async getById(id: number): Promise<Campaign | null> {
    const response = await httpClient.get<Campaign>(`${BASE_URL}/GetById/${id}`)
    return response.data ?? null
  },

  async getSummary(id: number): Promise<CampaignSummary | null> {
    const response = await httpClient.get<CampaignSummary>(`${BASE_URL}/summary/${id}`)
    return response.data ?? null
  },

  create(data: CreateCampaignRequest) {
    return httpClient.post<Campaign>(`${BASE_URL}/Create`, data)
  },

  update(id: number, data: UpdateCampaignRequest) {
    return httpClient.put<Campaign>(`${BASE_URL}/Update/${id}`, data)
  },

  async getStatusHistory(id: number): Promise<CampaignStatusHistoryEntry[]> {
    const response = await httpClient.get<CampaignStatusHistoryEntry[]>(`${BASE_URL}/statushistory/${id}`)
    return response.data ?? []
  },
}

export interface CampaignStatusHistoryEntry {
  id: number
  fromStatus?: number | null
  toStatus: number
  changedAt: string
  changedByUserId?: number | null
  changedByUserName?: string | null
  reason?: string | null
}
