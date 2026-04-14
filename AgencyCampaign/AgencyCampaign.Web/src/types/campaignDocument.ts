export interface CampaignDocument {
  id: number
  campaignId: number
  campaignCreatorId?: number
  documentType: number
  title: string
  documentUrl?: string
  status: number
  recipientEmail?: string
  emailSubject?: string
  emailBody?: string
  sentAt?: string
  signedAt?: string
  notes?: string
  createdAt: string
  updatedAt?: string
}
