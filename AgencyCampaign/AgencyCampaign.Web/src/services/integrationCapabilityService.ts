import { httpClient } from 'archon-ui'
import type { IntegrationCapability, IntegrationIntentDescriptor, SetIntegrationCapabilityPayload } from '../types/integrationCapability'

export const integrationCapabilityService = {
  async getAll(): Promise<IntegrationCapability[]> {
    const response = await httpClient.get<IntegrationCapability[]>('/IntegrationCapabilities/Get')
    return response.data ?? []
  },

  async getCatalog(): Promise<IntegrationIntentDescriptor[]> {
    const response = await httpClient.get<IntegrationIntentDescriptor[]>('/IntegrationCapabilities/catalog')
    return response.data ?? []
  },

  async getByIntent(intentKey: string): Promise<IntegrationCapability | null> {
    const response = await httpClient.get<IntegrationCapability>(`/IntegrationCapabilities/by-intent/${encodeURIComponent(intentKey)}`)
    return response.data ?? null
  },

  async setCapability(payload: SetIntegrationCapabilityPayload): Promise<IntegrationCapability> {
    const response = await httpClient.post<IntegrationCapability>('/IntegrationCapabilities/Set', payload)
    if (!response.data) throw new Error('Falha ao salvar vínculo de integração.')
    return response.data
  },

  async remove(intentKey: string): Promise<void> {
    await httpClient.delete(`/IntegrationCapabilities/Remove/${encodeURIComponent(intentKey)}`)
  },
}
