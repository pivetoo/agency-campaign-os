import { httpClient } from 'archon-ui'

const BASE_URL = '/proposal-public'

export interface ProposalPublicItem {
  id: number
  creatorId?: number
  creatorName?: string
  description: string
  quantity: number
  unitPrice: number
  total: number
  deliveryDeadline?: string
  observations?: string
  status: number
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
      const response = await httpClient.get<ProposalPublicView>(`${BASE_URL}/${encodeURIComponent(token)}`)
      return response.data ?? null
    } catch {
      return null
    }
  },

  accept(token: string, input: ProposalClientDecisionInput) {
    return httpClient.post(`${BASE_URL}/${encodeURIComponent(token)}/accept`, input)
  },

  reject(token: string, input: ProposalClientDecisionInput) {
    return httpClient.post(`${BASE_URL}/${encodeURIComponent(token)}/reject`, input)
  },

  async downloadPdf(token: string): Promise<void> {
    const response = await httpClient.get<Blob>(`${BASE_URL}/${encodeURIComponent(token)}/pdf`, { responseType: 'blob' })
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
