import { httpClient } from 'archon-ui'
import type { AgencyIntegrationBinding, IntegrationIntentDescriptor, SaveAgencyIntegrationBindingRequest } from '../types/integrationBinding'

const BASE_URL = '/AgencyIntegrationBindings'

export const agencyIntegrationBindingService = {
  async list(): Promise<AgencyIntegrationBinding[]> {
    const response = await httpClient.get<AgencyIntegrationBinding[]>(`${BASE_URL}/List`)
    return response.data ?? []
  },

  async catalog(): Promise<IntegrationIntentDescriptor[]> {
    const response = await httpClient.get<IntegrationIntentDescriptor[]>(`${BASE_URL}/Catalog`)
    return response.data ?? []
  },

  async getByIntentKey(intentKey: string): Promise<AgencyIntegrationBinding | null> {
    try {
      const response = await httpClient.get<AgencyIntegrationBinding>(`${BASE_URL}/GetByIntentKey/${encodeURIComponent(intentKey)}`)
      return response.data ?? null
    } catch {
      return null
    }
  },

  save(data: SaveAgencyIntegrationBindingRequest) {
    return httpClient.put<AgencyIntegrationBinding>(`${BASE_URL}/Save`, data)
  },

  delete(intentKey: string) {
    return httpClient.delete(`${BASE_URL}/Delete/${encodeURIComponent(intentKey)}`)
  },
}
