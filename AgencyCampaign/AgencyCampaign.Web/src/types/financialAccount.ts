export const FinancialAccountType = {
  Bank: 1,
  Cash: 2,
} as const

export type FinancialAccountTypeValue = (typeof FinancialAccountType)[keyof typeof FinancialAccountType]

export const financialAccountTypeLabels: Record<number, string> = {
  1: 'Banco',
  2: 'Caixa',
}

export const FinancialAccountSyncStatus = {
  NotConfigured: 0,
  Pending: 1,
  Synced: 2,
  Error: 3,
} as const

export type FinancialAccountSyncStatusValue = (typeof FinancialAccountSyncStatus)[keyof typeof FinancialAccountSyncStatus]

export interface FinancialAccount {
  id: number
  name: string
  type: FinancialAccountTypeValue
  bankId?: number | null
  bankCompe?: string | null
  bankShortName?: string | null
  bankLogoUrl?: string | null
  bank?: string | null
  agency?: string | null
  number?: string | null
  initialBalance: number
  currentBalance: number
  color: string
  isActive: boolean
  hasEntries: boolean
  integrationConnectorId?: number | null
  lastSyncedBalance?: number | null
  lastSyncedAt?: string | null
  syncStatus: FinancialAccountSyncStatusValue
}

export interface FinancialAccountSummary {
  activeCount: number
  inactiveCount: number
  totalKanvasBalance: number
  totalLastSyncedBalance: number
  syncedAccountsCount: number
  pendingSyncAccountsCount: number
  erroredSyncAccountsCount: number
}
