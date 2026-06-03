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

export const bankTransactionService = {
  getByAccount(accountId: number, page = 1, pageSize = 20): Promise<ApiResponse<BankTransaction[]>> {
    return httpClient.get<BankTransaction[]>(`${BASE_URL}/GetByAccount?accountId=${accountId}&page=${page}&pageSize=${pageSize}`)
  },

  match(id: number, financialEntryId: number) {
    return httpClient.post<BankTransaction>(`${BASE_URL}/${id}/match`, { financialEntryId })
  },

  unmatch(id: number) {
    return httpClient.post<BankTransaction>(`${BASE_URL}/${id}/unmatch`, {})
  },

  import(data: ImportBankTransactionsRequest) {
    return httpClient.post<ImportBankTransactionsResult>(`${BASE_URL}/Import`, data)
  },
}
