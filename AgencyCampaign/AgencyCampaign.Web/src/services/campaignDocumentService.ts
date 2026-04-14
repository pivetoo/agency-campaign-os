import { httpClient } from 'archon-ui'
import type { CampaignDocument } from '../types/campaignDocument'

const BASE_URL = '/CampaignDocuments'

export interface CreateCampaignDocumentRequest {
  campaignId: number
  campaignCreatorId?: number
  documentType: number
  title: string
  documentUrl?: string
  notes?: string
}

export interface UpdateCampaignDocumentRequest extends Omit<CreateCampaignDocumentRequest, 'campaignId'> {
  id: number
}

export interface SendCampaignDocumentEmailRequest {
  recipientEmail: string
  subject: string
  body?: string
}

export interface MarkCampaignDocumentSignedRequest {
  signedAt: string
}

export const campaignDocumentService = {
  async getByCampaign(campaignId: number): Promise<CampaignDocument[]> {
    const response = await httpClient.get<CampaignDocument[]>(`${BASE_URL}/campaign/${campaignId}`)
    return response.data ?? []
  },

  create(data: CreateCampaignDocumentRequest) {
    return httpClient.post<CampaignDocument>(`${BASE_URL}/Create`, data)
  },

  update(id: number, data: UpdateCampaignDocumentRequest) {
    return httpClient.put<CampaignDocument>(`${BASE_URL}/Update/${id}`, data)
  },

  sendEmail(id: number, data: SendCampaignDocumentEmailRequest) {
    return httpClient.post<CampaignDocument>(`${BASE_URL}/${id}/send-email`, data)
  },

  markSigned(id: number, data: MarkCampaignDocumentSignedRequest) {
    return httpClient.post<CampaignDocument>(`${BASE_URL}/${id}/mark-signed`, data)
  },
}
