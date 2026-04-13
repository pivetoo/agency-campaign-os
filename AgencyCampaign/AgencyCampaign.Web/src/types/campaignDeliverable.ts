export interface CampaignDeliverable {
  id: number
  campaignId: number
  creatorId: number
  title: string
  description?: string
  dueAt: string
  publishedAt?: string
  status: number
  grossAmount: number
  creatorAmount: number
  agencyFeeAmount: number
  campaign?: {
    id: number
    name: string
  }
  creator?: {
    id: number
    name: string
  }
  createdAt: string
  updatedAt?: string
}
