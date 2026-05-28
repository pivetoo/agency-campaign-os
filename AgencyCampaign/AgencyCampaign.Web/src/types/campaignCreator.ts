export interface CampaignCreator {
  id: number
  campaignId: number
  creatorId: number
  campaignCreatorStatusId: number
  campaignCreatorStatus?: {
    id: number
    name: string
    color: string
  }
  agreedAmount: number
  agencyFeePercent: number
  agencyFeeAmount: number
  notes?: string
  confirmedAt?: string
  cancelledAt?: string
  couponCode?: string | null
  trackingUrl?: string | null
  attributedOrders?: number | null
  attributedRevenue?: number | null
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
