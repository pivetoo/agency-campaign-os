import { httpClient } from 'archon-ui'
import type { FinancialEntry, FinancialSummary } from '../types/financialEntry'

const BASE_URL = '/FinancialEntries'

export interface CreateFinancialEntryRequest {
  accountId: number
  campaignId?: number | null
  campaignDeliverableId?: number
  type: number
  category: number
  description: string
  amount: number
  dueAt: string
  occurredAt: string
  paymentMethod?: string
  referenceCode?: string
  paidAt?: string
  status: number
  counterpartyName?: string
  notes?: string
  subcategoryId?: number | null
  invoiceNumber?: string
  invoiceUrl?: string
  invoiceIssuedAt?: string
}

export interface UpdateFinancialEntryRequest extends CreateFinancialEntryRequest {
  id: number
}

export interface CreateInstallmentSeriesRequest extends CreateFinancialEntryRequest {
  installmentTotal: number
}

export interface FinancialEntryFilters {
  type?: number
  status?: number
  accountId?: number
  campaignId?: number
  dueFrom?: string
  dueTo?: string
  search?: string
  pageSize?: number
  page?: number
}

export interface MarkAsPaidRequest {
  paidAt?: string
  accountId: number
  paymentMethod?: string
}

export const financialEntryService = {
  async getAll(filters: FinancialEntryFilters = {}): Promise<FinancialEntry[]> {
    const params = new URLSearchParams()
    Object.entries(filters).forEach(([key, value]) => {
      if (value !== undefined && value !== null && value !== '') {
        params.append(key, String(value))
      }
    })
    if (!params.has('pageSize')) params.append('pageSize', '500')
    const response = await httpClient.get<FinancialEntry[]>(`${BASE_URL}/Get?${params.toString()}`)
    // backend retorna paginado pelo Archon: data e o array, pagination vem fora
    return Array.isArray(response.data) ? response.data : []
  },

  async getByCampaign(campaignId: number): Promise<FinancialEntry[]> {
    const response = await httpClient.get<FinancialEntry[]>(`${BASE_URL}/campaign/${campaignId}`)
    return response.data ?? []
  },

  async getSummary(type: number): Promise<FinancialSummary | null> {
    const response = await httpClient.get<FinancialSummary>(`${BASE_URL}/summary/${type}`)
    return response.data ?? null
  },

  create(data: CreateFinancialEntryRequest) {
    return httpClient.post<FinancialEntry>(`${BASE_URL}/Create`, data)
  },

  update(id: number, data: UpdateFinancialEntryRequest) {
    return httpClient.put<FinancialEntry>(`${BASE_URL}/Update/${id}`, data)
  },

  markAsPaid(id: number, data: MarkAsPaidRequest) {
    return httpClient.post<FinancialEntry>(`${BASE_URL}/markaspaid/${id}`, data)
  },

  createInstallments(data: CreateInstallmentSeriesRequest) {
    return httpClient.post<FinancialEntry[]>(`${BASE_URL}/CreateInstallments`, data)
  },
}
