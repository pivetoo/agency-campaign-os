import { httpClient } from 'archon-ui'
import type { ContentLicense, ContentLicenseInput } from '../types/contentLicense'

const BASE = '/ContentLicense'

export const contentLicenseService = {
  async getByDeliverable(deliverableId: number): Promise<ContentLicense[]> {
    const response = await httpClient.get<ContentLicense[]>(`${BASE}/deliverable/${deliverableId}`)
    return response.data ?? []
  },

  add(deliverableId: number, data: ContentLicenseInput) {
    return httpClient.post<ContentLicense>(`${BASE}/deliverable/${deliverableId}`, data)
  },

  update(licenseId: number, data: ContentLicenseInput) {
    return httpClient.put<ContentLicense>(`${BASE}/Update/${licenseId}`, { id: licenseId, ...data })
  },

  remove(licenseId: number) {
    return httpClient.delete(`${BASE}/Delete/${licenseId}`)
  },

  applyToCampaign(licenseId: number) {
    return httpClient.post<number>(`${BASE}/apply-to-campaign/${licenseId}`, {})
  },

  async getExpiring(days = 30): Promise<ContentLicense[]> {
    const response = await httpClient.get<ContentLicense[]>(`${BASE}/expiring?days=${days}`)
    return response.data ?? []
  },
}
