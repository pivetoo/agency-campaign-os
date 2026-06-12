import { httpClient } from 'archon-ui'
import type { ApiResponse } from 'archon-ui'
import type { BankTransaction } from '../types/bankTransaction'

const BASE_URL = '/BankTransactions'

export interface ImportBankTransactionItem {
  externalId: string
  occurredAt: string
  amount: number
  direction: number
  description: string
  category?: string
}

export interface ImportBankTransactionsRequest {
  accountId: number
  syncedBalance?: number
  syncedAt?: string
  transactions: ImportBankTransactionItem[]
}

export interface ImportBankTransactionsResult {
  imported: number
  skipped: number
  autoMatched: number
}

export interface ReconciliationSummary {
  total: number
  matched: number
  pending: number
}

export const bankTransactionService = {
  getByAccount(accountId: number, page = 1, pageSize = 20): Promise<ApiResponse<BankTransaction[]>> {
    return httpClient.get<BankTransaction[]>(`${BASE_URL}/GetByAccount?accountId=${accountId}&page=${page}&pageSize=${pageSize}`)
  },

  getSummary(accountId: number): Promise<ApiResponse<ReconciliationSummary>> {
    return httpClient.get<ReconciliationSummary>(`${BASE_URL}/summary?accountId=${accountId}`)
  },

  match(id: number, financialEntryId: number) {
    return httpClient.post<BankTransaction>(`${BASE_URL}/match/${id}`, { financialEntryId })
  },

  unmatch(id: number) {
    return httpClient.post<BankTransaction>(`${BASE_URL}/unmatch/${id}`, {})
  },

  import(data: ImportBankTransactionsRequest) {
    return httpClient.post<ImportBankTransactionsResult>(`${BASE_URL}/Import`, data)
  },
}
