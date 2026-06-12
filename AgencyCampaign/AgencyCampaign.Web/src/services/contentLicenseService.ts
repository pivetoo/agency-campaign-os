import { httpClient, buildPaginationQuery } from 'archon-ui'
import type { ApiResponse } from 'archon-ui'
import type { ContentLicense, ContentLicenseInput } from '../types/contentLicense'

const BASE = '/ContentLicense'

export const contentLicenseService = {
  getLicenses(params?: { page?: number; pageSize?: number; status?: number; type?: number; campaignId?: number; search?: string }): Promise<ApiResponse<ContentLicense[]>> {
    const query = buildPaginationQuery(params)
    const extras: string[] = []
    if (params?.status) extras.push(`status=${params.status}`)
    if (params?.type) extras.push(`type=${params.type}`)
    if (params?.campaignId) extras.push(`campaignId=${params.campaignId}`)
    if (params?.search) extras.push(`search=${encodeURIComponent(params.search)}`)
    const extraQuery = extras.length > 0 ? `${query ? '&' : '?'}${extras.join('&')}` : ''
    return httpClient.get<ContentLicense[]>(`${BASE}/list${query}${extraQuery}`)
  },

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
