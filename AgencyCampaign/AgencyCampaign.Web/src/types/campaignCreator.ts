export interface CampaignCreator {
  id: number
  campaignId: number
  creatorId: number
  status: number
  agreedAmount: number
  agencyFeeAmount: number
  notes?: string
  confirmedAt?: string
  cancelledAt?: string
  campaign?: {
    id: number
    name: string
  }
  creator?: {
    id: number
    name: string
    stageName?: string
  }
  createdAt: string
  updatedAt?: string
}
