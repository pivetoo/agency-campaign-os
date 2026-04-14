export interface CampaignDeliverable {
  id: number
  campaignId: number
  campaignCreatorId: number
  title: string
  description?: string
  deliverableKindId: number
  platformId: number
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
  deliverableKind?: {
    id: number
    name: string
  }
  platform?: {
    id: number
    name: string
  }
  createdAt: string
  updatedAt?: string
}
