export interface CampaignCreator {
  id: number
  campaignId: number
  creatorId: number
  status: number
  agreedAmount: number
  agencyFeePercent: number
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
    defaultAgencyFeePercent: number
  }
  createdAt: string
  updatedAt?: string
}
