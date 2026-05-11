import { httpClient } from 'archon-ui'
import type {
  CampaignDocument,
  CampaignDocumentSignerRoleValue,
  CampaignDocumentTypeValue,
} from '../types/campaignDocument'

const BASE_URL = '/CampaignDocuments'

export interface CreateCampaignDocumentRequest {
  campaignId: number
  campaignCreatorId?: number
  documentType: CampaignDocumentTypeValue
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

export interface GenerateCampaignDocumentFromTemplateRequest {
  campaignId: number
  campaignCreatorId?: number
  templateId: number
  title: string
  overrides?: Record<string, string>
}

export interface CampaignDocumentSignerInput {
  role: CampaignDocumentSignerRoleValue
  name: string
  email: string
  documentNumber?: string
}

export interface SendCampaignDocumentForSignatureRequest {
  connectorId: number
  pipelineId: number
  signers: CampaignDocumentSignerInput[]
}

export const campaignDocumentService = {
  async getById(id: number): Promise<CampaignDocument | null> {
    const response = await httpClient.get<CampaignDocument>(`${BASE_URL}/GetById/${id}`)
    return response.data ?? null
  },

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

  generateFromTemplate(data: GenerateCampaignDocumentFromTemplateRequest) {
    return httpClient.post<CampaignDocument>(`${BASE_URL}/GenerateFromTemplate`, data)
  },

  sendForSignature(id: number, data: SendCampaignDocumentForSignatureRequest) {
    return httpClient.post<CampaignDocument>(`${BASE_URL}/${id}/send-signature`, data)
  },
}
