import type { CampaignDocumentTypeValue } from './campaignDocument'

export interface CampaignDocumentTemplateVariable {
  key: string
  label: string
  description: string
  group: string
}

export type CampaignDocumentTemplateVariableMap = Record<string, CampaignDocumentTemplateVariable[]>

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
