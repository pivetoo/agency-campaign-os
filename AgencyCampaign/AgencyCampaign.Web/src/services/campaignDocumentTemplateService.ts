import { httpClient } from 'archon-ui'
import type { CampaignDocumentTemplate } from '../types/campaignDocumentTemplate'
import type { CampaignDocumentTypeValue } from '../types/campaignDocument'

const BASE_URL = '/CampaignDocumentTemplates'

export interface CreateCampaignDocumentTemplateRequest {
  name: string
  description?: string
  documentType: CampaignDocumentTypeValue
  body: string
}

export interface UpdateCampaignDocumentTemplateRequest extends CreateCampaignDocumentTemplateRequest {
  id: number
  isActive: boolean
}

export const campaignDocumentTemplateService = {
  async getAll(): Promise<CampaignDocumentTemplate[]> {
    const response = await httpClient.get<{ items: CampaignDocumentTemplate[] }>(`${BASE_URL}/Get?pageSize=200`)
    const data = response.data as unknown as { items: CampaignDocumentTemplate[] } | CampaignDocumentTemplate[] | null
    if (!data) return []
    if (Array.isArray(data)) return data
    return data.items ?? []
  },

  async getById(id: number): Promise<CampaignDocumentTemplate | null> {
    const response = await httpClient.get<CampaignDocumentTemplate>(`${BASE_URL}/${id}`)
    return response.data ?? null
  },

  async getActiveByDocumentType(documentType: CampaignDocumentTypeValue): Promise<CampaignDocumentTemplate[]> {
    const response = await httpClient.get<CampaignDocumentTemplate[]>(`${BASE_URL}/active/${documentType}`)
    return response.data ?? []
  },

  create(data: CreateCampaignDocumentTemplateRequest) {
    return httpClient.post<CampaignDocumentTemplate>(`${BASE_URL}/Create`, data)
  },

  update(id: number, data: UpdateCampaignDocumentTemplateRequest) {
    return httpClient.put<CampaignDocumentTemplate>(`${BASE_URL}/Update/${id}`, data)
  },

  delete(id: number) {
    return httpClient.delete(`${BASE_URL}/Delete/${id}`)
  },
}
