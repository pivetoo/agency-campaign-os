import { httpClient } from 'archon-ui'
import type { AgencySettings } from '../types/agencySettings'

const BASE_URL = '/AgencySettings'

export interface UpdateAgencySettingsRequest {
  agencyName: string
  tradeName?: string | null
  document?: string | null
  primaryEmail?: string | null
  phone?: string | null
  address?: string | null
  logoUrl?: string | null
  primaryColor?: string | null
  defaultEmailConnectorId?: number | null
  defaultEmailPipelineId?: number | null
}

export const agencySettingsService = {
  async get(): Promise<AgencySettings | null> {
    const response = await httpClient.get<AgencySettings>(`${BASE_URL}/Get`)
    return response.data ?? null
  },

  update(data: UpdateAgencySettingsRequest) {
    return httpClient.put<AgencySettings>(`${BASE_URL}/Update`, data)
  },
}
