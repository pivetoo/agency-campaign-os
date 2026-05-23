export interface PolicyDeviation {
  field: string
  policyValue: string
  requestedValue: string
  delta: string
  kind: number
}

export interface PolicyImpact {
  label: string
  value: string
  isGood: boolean
}

export interface PolicyEvaluation {
  hasDeviations: boolean
  policyMissing: boolean
  suggestedApprovalType?: string
  deviations: PolicyDeviation[]
  impacts: PolicyImpact[]
}
