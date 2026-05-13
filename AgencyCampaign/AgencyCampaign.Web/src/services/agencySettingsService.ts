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

  getProposalLayouts() {
    return httpClient.get<ProposalLayout[]>(`${BASE_URL}/GetProposalLayouts`)
  },

  saveProposalTemplate(template: string | null) {
    return httpClient.put<AgencySettings>(`${BASE_URL}/SaveProposalTemplate`, { template })
  },

  previewProposalTemplate(template: string) {
    return httpClient.post<{ html: string }>(`${BASE_URL}/PreviewProposalTemplate`, { template })
  },

  getProposalTemplateVersions() {
    return httpClient.get<ProposalTemplateVersion[]>(`${BASE_URL}/GetProposalTemplateVersions`)
  },

  saveProposalTemplateVersion(name: string, template: string, activate: boolean) {
    return httpClient.post<ProposalTemplateVersion>(`${BASE_URL}/SaveProposalTemplateVersion`, { name, template, activate })
  },

  activateProposalTemplateVersion(id: number) {
    return httpClient.put<ProposalTemplateVersion>(`${BASE_URL}/ActivateProposalTemplateVersion?id=${id}`)
  },

  deleteProposalTemplateVersion(id: number) {
    return httpClient.delete<unknown>(`${BASE_URL}/DeleteProposalTemplateVersion?id=${id}`)
  },

  setWhatsAppConnector(connectorId: number | null) {
    return httpClient.put<AgencySettings>(`${BASE_URL}/SetWhatsAppConnector`, { connectorId })
  },
}

export interface ProposalLayout {
  key: string
  name: string
  template: string
}

export interface ProposalTemplateVersion {
  id: number
  name: string
  template: string
  isActive: boolean
  createdAt: string
}
