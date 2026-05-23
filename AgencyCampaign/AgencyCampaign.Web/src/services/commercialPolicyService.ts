import { httpClient } from 'archon-ui'
import type { CommercialPolicy } from '../types/commercialPolicy'

export interface UpsertCommercialPolicyRequest {
  maxDiscountPercent?: number | null
  minMarginPercent?: number | null
  defaultPaymentTermDays?: number | null
  maxPaymentTermDays?: number | null
  notes?: string
}

const BASE = '/CommercialPolicy'

export const commercialPolicyService = {
  async get(): Promise<CommercialPolicy | null> {
    const response = await httpClient.get<CommercialPolicy>(`${BASE}/Get`)
    return response.data ?? null
  },

  upsert(data: UpsertCommercialPolicyRequest) {
    return httpClient.put<CommercialPolicy>(`${BASE}/Upsert`, data)
  },
}
