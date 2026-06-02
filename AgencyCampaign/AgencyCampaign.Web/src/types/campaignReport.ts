export interface CampaignReportTotals {
  deliverablesCount: number
  publishedCount: number
  totalReach: number
  totalImpressions: number
  totalViews: number
  totalEngagement: number
  avgEngagementRate?: number
  investment: number
  cpm?: number
  costPerEngagement?: number
  emv?: number
  attributedRevenue?: number
  attributedOrders?: number
  roi?: number
}

export interface CampaignReportGroupItem {
  name: string
  deliverables: number
  reach: number
  impressions: number
  engagement: number
}

export interface CampaignReportDeliverableItem {
  title: string
  platformName: string
  creatorName: string
  publishedUrl?: string
  publishedAt?: string
  reach?: number
  impressions?: number
  views?: number
  engagement?: number
  engagementRate?: number
}

export interface CampaignReport {
  campaignName: string
  brandName?: string
  startsAt?: string
  endsAt?: string
  totals: CampaignReportTotals
  byPlatform: CampaignReportGroupItem[]
  byCreator: CampaignReportGroupItem[]
  deliverables: CampaignReportDeliverableItem[]
}

export interface CampaignReportLink {
  token: string
  isActive: boolean
  expiresAt?: string
  revokedAt?: string
  lastViewedAt?: string
  viewCount: number
  createdAt: string
}
