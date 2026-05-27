import { httpClient } from 'archon-ui'
import type { CampaignDocument } from '../types/campaignDocument'
import type { CreatorPayment, PixKeyTypeValue } from '../types/creatorPayment'
import type { CampaignDeliverable } from '../types/campaignDeliverable'
import type { ContentReview, ContentAssetInput } from '../types/contentReview'

const BASE = '/CreatorPortal'

export interface PortalCreator {
  id: number
  name: string
  stageName?: string
  email?: string
  phone?: string
  document?: string
  pixKey?: string
  pixKeyType?: PixKeyTypeValue
  primaryNiche?: string
}

export interface PortalCampaign {
  id: number
  campaignId: number
  campaignName?: string
  brandName?: string
  statusName?: string
  statusColor?: string
  agreedAmount: number
  agencyFeePercent: number
  notes?: string
  startsAt?: string
  endsAt?: string
}

export interface PortalSession {
  creator: PortalCreator
  token: {
    expiresAt?: string
    usageCount: number
    lastUsedAt?: string
  }
}

export interface UpdateBankInfoPayload {
  pixKey: string
  pixKeyType: PixKeyTypeValue
  document?: string
}

export interface UploadInvoicePayload {
  creatorPaymentId: number
  invoiceNumber?: string
  invoiceUrl: string
  issuedAt?: string
}

export interface SubmitInsightsPayload {
  reach?: number | null
  impressions?: number | null
  saves?: number | null
}

export const creatorPortalService = {
  async me(token: string): Promise<PortalSession | null> {
    const response = await httpClient.get<PortalSession>(`${BASE}/${token}/me`)
    return response.data ?? null
  },
  async getCampaigns(token: string): Promise<PortalCampaign[]> {
    const response = await httpClient.get<PortalCampaign[]>(`${BASE}/${token}/campaigns`)
    return response.data ?? []
  },
  async getDocuments(token: string): Promise<CampaignDocument[]> {
    const response = await httpClient.get<CampaignDocument[]>(`${BASE}/${token}/documents`)
    return response.data ?? []
  },
  async getPayments(token: string): Promise<CreatorPayment[]> {
    const response = await httpClient.get<CreatorPayment[]>(`${BASE}/${token}/payments`)
    return response.data ?? []
  },
  updateBankInfo(token: string, payload: UpdateBankInfoPayload) {
    return httpClient.post(`${BASE}/${token}/bank-info`, payload)
  },
  uploadInvoice(token: string, payload: UploadInvoicePayload) {
    return httpClient.post<CreatorPayment>(`${BASE}/${token}/invoice`, payload)
  },
  async getDeliverables(token: string): Promise<CampaignDeliverable[]> {
    const response = await httpClient.get<CampaignDeliverable[]>(`${BASE}/${token}/deliverables`)
    return response.data ?? []
  },
  submitInsights(token: string, deliverableId: number, payload: SubmitInsightsPayload) {
    return httpClient.post<CampaignDeliverable>(`${BASE}/${token}/deliverables/${deliverableId}/insights`, payload)
  },
  async getDeliverableReview(token: string, deliverableId: number): Promise<ContentReview | null> {
    const response = await httpClient.get<ContentReview>(`${BASE}/${token}/deliverables/${deliverableId}/review`)
    return response.data ?? null
  },
  submitContentVersion(token: string, deliverableId: number, assets: ContentAssetInput[], note?: string) {
    return httpClient.post<ContentReview>(`${BASE}/${token}/deliverables/${deliverableId}/version`, { assets, note })
  },
  addReviewComment(token: string, deliverableId: number, body: string) {
    return httpClient.post<ContentReview>(`${BASE}/${token}/deliverables/${deliverableId}/comment`, { body })
  },
  uploadReviewFile(token: string, deliverableId: number, file: File) {
    const form = new FormData()
    form.append('file', file)
    return httpClient.post<{ url: string; fileName: string; contentType: string }>(`${BASE}/${token}/deliverables/${deliverableId}/upload`, form)
  },
}
