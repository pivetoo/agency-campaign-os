export interface CampaignDeliverable {
  id: number
  campaignId: number
  campaignCreatorId: number
  title: string
  description?: string
  type: number
  platform: number
  dueAt: string
  publishedAt?: string
  publishedUrl?: string
  evidenceUrl?: string
  status: number
  grossAmount: number
  creatorAmount: number
  agencyFeeAmount: number
  notes?: string
  campaign?: {
    id: number
    name: string
  }
  campaignCreator?: {
    id: number
    creatorId: number
    creatorName: string
    stageName?: string
  }
  createdAt: string
  updatedAt?: string
}
