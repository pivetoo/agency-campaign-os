export const MonitorAlertSeverity = {
  Critical: 1,
  Warning: 2,
  Info: 3,
} as const

export type MonitorAlertSeverityValue = (typeof MonitorAlertSeverity)[keyof typeof MonitorAlertSeverity]

export interface MonitorAlert {
  type: string
  severity: MonitorAlertSeverityValue
  count: number
  amount?: number | null
  amountIn?: number | null
  amountOut?: number | null
  referenceDate?: string | null
  worstDays?: number | null
  accountId?: number | null
  accountName?: string | null
}

export interface MonitorPulse {
  realBalance: number
  projectedBalance30d: number
  projectionNegativeAt?: string | null
  receivableOpen: number
  receivableOverdue: number
  receivableOverdueCount: number
  payableOpen: number
  payableOverdue: number
  payableOverdueCount: number
  payoutQueueCount: number
  payoutQueueAmount: number
}

export interface MonitorUpcomingDay {
  date: string
  inCount: number
  inAmount: number
  outCount: number
  outAmount: number
}

export interface MonitorPayoutFunnel {
  pendingApproval: number
  readyToPay: number
  scheduled: number
  failed: number
}

export interface MonitorReconciliationAccount {
  accountId: number
  accountName: string
  pending: number
  lastImportAt?: string | null
}

export interface MonitorPeriod {
  year: number
  month: number
  isClosed: boolean
  closedAt?: string | null
}

export interface CashFlowProjectionWeek {
  weekStart: string
  inflow: number
  outflow: number
  net: number
  projectedBalance: number
}

export interface FinancialMonitor {
  generatedAt: string
  pulse: MonitorPulse
  alerts: MonitorAlert[]
  projection: { generatedAt: string; openingBalance: number; weeks: number; series: CashFlowProjectionWeek[] }
  upcoming: MonitorUpcomingDay[]
  payoutFunnel: MonitorPayoutFunnel
  reconciliation: MonitorReconciliationAccount[]
  periods: { current: MonitorPeriod; previous: MonitorPeriod }
}
