export interface DeliverableShareLink {
  id: number
  campaignDeliverableId: number
  token: string
  reviewerName: string
  expiresAt?: string | null
  revokedAt?: string | null
  lastViewedAt?: string | null
  viewCount: number
  isActive: boolean
  createdAt: string
}

export interface DeliverableApproval {
  id: number
  campaignDeliverableId: number
  approvalType: number
  status: number
  reviewerName: string
  comment?: string | null
  approvedAt?: string | null
  rejectedAt?: string | null
}

export interface PendingApproval {
  deliverableId: number
  deliverableTitle: string
  campaignName?: string | null
  brandName?: string | null
  creatorName?: string | null
  platformName?: string | null
  dueAt: string
  deliverableStatus: number
  approvals: DeliverableApproval[]
  hasActiveShareLink: boolean
}

export interface DeliverablePublicView {
  deliverableId: number
  title: string
  description?: string | null
  creatorName?: string | null
  platformName?: string | null
  deliverableKindName?: string | null
  campaignName?: string | null
  brandName?: string | null
  dueAt: string
  publishedUrl?: string | null
  evidenceUrl?: string | null
  status: number
  approvalStatus?: number | null
  approvalComment?: string | null
}
