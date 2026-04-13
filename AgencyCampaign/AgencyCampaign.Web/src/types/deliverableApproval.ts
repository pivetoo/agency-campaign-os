export interface DeliverableApproval {
  id: number
  campaignDeliverableId: number
  approvalType: number
  status: number
  reviewerName: string
  comment?: string
  approvedAt?: string
  rejectedAt?: string
  campaignDeliverable?: {
    id: number
    title: string
  }
  createdAt: string
  updatedAt?: string
}
