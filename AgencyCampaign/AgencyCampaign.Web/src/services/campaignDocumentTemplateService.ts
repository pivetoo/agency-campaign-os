import { httpClient } from 'archon-ui'
import type { CampaignDocumentTemplate, CampaignDocumentTemplateVariableMap } from '../types/campaignDocumentTemplate'
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
  getAll(params?: { page?: number; pageSize?: number }) {
    const searchParams = new URLSearchParams()
    if (params?.page) searchParams.set('page', params.page.toString())
    if (params?.pageSize) searchParams.set('pageSize', params.pageSize.toString())
    const query = searchParams.toString()
    return httpClient.get<CampaignDocumentTemplate[]>(`${BASE_URL}/Get${query ? `?${query}` : ''}`)
  },

  async getById(id: number): Promise<CampaignDocumentTemplate | null> {
    const response = await httpClient.get<CampaignDocumentTemplate>(`${BASE_URL}/GetById/${id}`)
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

  async getVariables(): Promise<CampaignDocumentTemplateVariableMap> {
    const response = await httpClient.get<CampaignDocumentTemplateVariableMap>(`${BASE_URL}/Variables`)
    return response.data ?? {}
  },
}
