import type { CampaignDocumentTypeValue } from './campaignDocument'

export interface CampaignDocumentTemplate {
  id: number
  name: string
  description?: string
  documentType: CampaignDocumentTypeValue
  body: string
  isActive: boolean
  createdByUserId?: number
  createdByUserName?: string
  createdAt: string
  updatedAt?: string
}
