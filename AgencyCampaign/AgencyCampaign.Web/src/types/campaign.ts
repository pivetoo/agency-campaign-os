export interface Campaign {
  id: number
  brandId: number
  name: string
  description?: string
  objective?: string
  briefing?: string
  budget: number
  startsAt: string
  endsAt?: string
  status: number
  internalOwnerName?: string
  notes?: string
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
  status: number
  budget: number
  campaignCreatorsCount: number
  confirmedCampaignCreatorsCount: number
  deliverablesCount: number
  pendingDeliverablesCount: number
  publishedDeliverablesCount: number
  pendingApprovalsCount: number
  grossAmountTotal: number
  creatorAmountTotal: number
  agencyFeeAmountTotal: number
  remainingBudget: number
}
