export const OpportunityApprovalDiffKind = {
  Change: 1,
  Add: 2,
  Remove: 3,
} as const
export type OpportunityApprovalDiffKindValue = (typeof OpportunityApprovalDiffKind)[keyof typeof OpportunityApprovalDiffKind]

export interface OpportunityApprovalDiff {
  id: number
  opportunityApprovalRequestId: number
  field: string
  policyValue: string
  requestedValue: string
  delta?: string
  kind: OpportunityApprovalDiffKindValue
  displayOrder: number
}
