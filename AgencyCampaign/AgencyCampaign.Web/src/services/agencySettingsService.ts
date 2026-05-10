import { httpClient } from 'archon-ui'
import type { AgencySettings } from '../types/agencySettings'
import { resolveUploadUrl } from '../lib/uploadUrl'

const BASE_URL = '/AgencySettings'

export const resolveAgencyLogoUrl = resolveUploadUrl

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

  uploadLogo(file: File) {
    const formData = new FormData()
    formData.append('file', file)
    return httpClient.post<AgencySettings>(`${BASE_URL}/UploadLogo`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
  },

  removeLogo() {
    return httpClient.delete<AgencySettings>(`${BASE_URL}/RemoveLogo`)
  },
}
