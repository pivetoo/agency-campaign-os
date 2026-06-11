import { publicClient } from '../lib/publicClient'

const BASE_URL = '/proposal-public'

export interface ProposalPublicSocial {
  platform: string
  handle: string
  profileUrl?: string
  followers?: number
  engagementRate?: number
  isPrimary?: boolean
}

export interface ProposalPublicItem {
  id: number
  creatorId?: number
  creatorName?: string
  creatorStageName?: string
  creatorPhotoUrl?: string
  creatorNiche?: string
  creatorSocials?: ProposalPublicSocial[]
  description: string
  quantity: number
  unitPrice: number
  total: number
  deliveryDeadline?: string
  observations?: string
  status: number
  kind?: number
  usageDurationMonths?: number
  usageScope?: string
  pricingModel?: number
  variableRate?: number
  variableBasis?: number
  isVariable?: boolean
}

export interface ProposalPublicSnapshot {
  proposalId: number
  name: string
  description?: string
  totalValue: number
  validityUntil?: string
  notes?: string
  items: ProposalPublicItem[]
}

export interface ProposalPublicView {
  proposalId: number
  versionNumber: number
  name: string
  description?: string
  agencyName: string
  agencyLogoUrl?: string
  agencyPrimaryColor?: string
  brandName: string
  brandLogoUrl?: string
  totalValue: number
  discountPercent?: number
  discountValue: number
  netTotalValue: number
  validityUntil?: string
  sentAt: string
  snapshotJson: string
  canDecide: boolean
  decision?: 'accepted' | 'rejected' | null
  decidedByName?: string
  decidedAt?: string
}

export interface ProposalClientDecisionInput {
  name: string
  email?: string
  notes?: string
}

export const proposalPublicService = {
  async getByToken(token: string): Promise<ProposalPublicView | null> {
    try {
      const response = await publicClient.get<ProposalPublicView>(`${BASE_URL}/${encodeURIComponent(token)}`)
      return response.data ?? null
    } catch {
      return null
    }
  },

  accept(token: string, input: ProposalClientDecisionInput) {
    return publicClient.post(`${BASE_URL}/${encodeURIComponent(token)}/accept`, input)
  },

  reject(token: string, input: ProposalClientDecisionInput) {
    return publicClient.post(`${BASE_URL}/${encodeURIComponent(token)}/reject`, input)
  },

  async downloadPdf(token: string): Promise<void> {
    const response = await publicClient.get<Blob>(`${BASE_URL}/${encodeURIComponent(token)}/pdf`, { responseType: 'blob' })
    const blob = response.data
    if (!blob) return
    const url = window.URL.createObjectURL(blob as Blob)
    const link = document.createElement('a')
    link.href = url
    link.download = `proposta-${token}.pdf`
    document.body.appendChild(link)
    link.click()
    link.remove()
    window.URL.revokeObjectURL(url)
  },

  parseSnapshot(snapshotJson: string): ProposalPublicSnapshot | null {
    try {
      return JSON.parse(snapshotJson) as ProposalPublicSnapshot
    } catch {
      return null
    }
  },
}
