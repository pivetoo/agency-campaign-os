export interface OpportunityApprovalComment {
  id: number
  opportunityApprovalRequestId: number
  userId?: number
  userName: string
  role: string
  body: string
  createdAt: string
  updatedAt?: string
}
