export const OpportunityApprovalReviewerStatus = {
  Pending: 1,
  Approved: 2,
  Rejected: 3,
  Commented: 4,
} as const
export type OpportunityApprovalReviewerStatusValue = (typeof OpportunityApprovalReviewerStatus)[keyof typeof OpportunityApprovalReviewerStatus]

export interface OpportunityApprovalReviewer {
  id: number
  opportunityApprovalRequestId: number
  userId?: number
  userName: string
  role?: string
  required: boolean
  status: OpportunityApprovalReviewerStatusValue
  decidedAt?: string
  decisionNotes?: string
  createdAt: string
}
