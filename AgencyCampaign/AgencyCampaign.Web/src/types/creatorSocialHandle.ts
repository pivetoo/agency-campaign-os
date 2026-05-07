export interface CreatorSocialHandle {
  id: number
  creatorId: number
  platformId: number
  platformName: string
  handle: string
  profileUrl?: string
  followers?: number | null
  engagementRate?: number | null
  isPrimary: boolean
  isActive: boolean
}

export interface CreatorPerformanceByPlatform {
  platformId: number
  platformName: string
  deliverables: number
  published: number
  grossAmount: number
}

export interface CreatorSummary {
  creatorId: number
  creatorName: string
  totalCampaigns: number
  confirmedCampaigns: number
  cancelledCampaigns: number
  totalDeliverables: number
  publishedDeliverables: number
  overdueDeliverables: number
  totalGrossAmount: number
  totalCreatorAmount: number
  totalAgencyFeeAmount: number
  onTimeDeliveryRate: number
  performanceByPlatform: CreatorPerformanceByPlatform[]
}

export interface CreatorCampaignEntry {
  campaignCreatorId: number
  campaignId: number
  campaignName?: string | null
  brandId?: number | null
  brandName?: string | null
  statusId: number
  statusName?: string | null
  statusColor?: string | null
  agreedAmount: number
  agencyFeeAmount: number
  confirmedAt?: string | null
  cancelledAt?: string | null
}
