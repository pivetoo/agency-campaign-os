export interface Campaign {
  id: number
  brandId: number
  name: string
  description?: string
  budget: number
  startsAt: string
  endsAt?: string
  isActive: boolean
  brand?: {
    id: number
    name: string
  }
  createdAt: string
  updatedAt?: string
}

export interface CampaignSummary {
  campaignId: number
  campaignName: string
  brandId: number
  brandName: string
  budget: number
  deliverablesCount: number
  pendingDeliverablesCount: number
  publishedDeliverablesCount: number
  grossAmountTotal: number
  creatorAmountTotal: number
  agencyFeeAmountTotal: number
  remainingBudget: number
}
