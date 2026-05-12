import { httpClient } from 'archon-ui'
import type { ApiResponse } from 'archon-ui'

const BASE_URL = '/Proposals'

export interface ProposalItem {
  id: number
  proposalId: number
  description: string
  quantity: number
  unitPrice: number
  deliveryDeadline?: string
  status: number
  observations?: string
  creatorId?: number
  creator?: {
    id: number
    name: string
    stageName?: string
  }
  total: number
}

export interface Proposal {
  id: number
  name: string
  description?: string
  status: number
  validityUntil?: string
  opportunityId: number
  opportunity?: {
    id: number
    name: string
  }
  totalValue: number
  internalOwnerId: number
  internalOwnerName?: string
  notes?: string
  campaignId?: number
  brand?: {
    id: number
    name: string
  }
  campaign?: {
    id: number
    name: string
  }
  items?: ProposalItem[]
  createdAt: string
  updatedAt?: string
}

export interface CreateProposalRequest {
  opportunityId: number
  description?: string
  validityUntil?: string
  notes?: string
  internalOwnerId?: number
  internalOwnerName?: string
}

export interface UpdateProposalRequest extends CreateProposalRequest {
  id: number
}

export interface CreateProposalItemRequest {
  proposalId: number
  description: string
  quantity: number
  unitPrice: number
  deliveryDeadline?: string
  creatorId?: number
  observations?: string
}

export interface UpdateProposalItemRequest {
  description: string
  quantity: number
  unitPrice: number
  deliveryDeadline?: string
  observations?: string
}

export interface ProposalListFilters {
  search?: string
  status?: number
  opportunityId?: number
  internalOwnerId?: number
  validityFrom?: string
  validityTo?: string
}

export interface ProposalVersion {
  id: number
  proposalId: number
  versionNumber: number
  name: string
  description?: string
  totalValue: number
  validityUntil?: string
  sentAt: string
  sentByUserId?: number
  sentByUserName?: string
}

export interface ProposalVersionDetail extends ProposalVersion {
  snapshotJson: string
}

export interface ProposalShareLink {
  id: number
  proposalId: number
  token: string
  publicUrl: string
  expiresAt?: string
  revokedAt?: string
  isActive: boolean
  createdByUserName?: string
  createdAt: string
  lastViewedAt?: string
  viewCount: number
}

export interface CreateProposalShareLinkRequest {
  expiresAt?: string
}

export const proposalService = {
  getAll(params?: { page?: number; pageSize?: number } & ProposalListFilters): Promise<ApiResponse<Proposal[]>> {
    const searchParams = new URLSearchParams()
    if (params?.page) searchParams.set('page', params.page.toString())
    if (params?.pageSize) searchParams.set('pageSize', params.pageSize.toString())
    if (params?.search) searchParams.set('search', params.search)
    if (params?.status !== undefined) searchParams.set('status', params.status.toString())
    if (params?.opportunityId) searchParams.set('opportunityId', params.opportunityId.toString())
    if (params?.internalOwnerId) searchParams.set('internalOwnerId', params.internalOwnerId.toString())
    if (params?.validityFrom) searchParams.set('validityFrom', params.validityFrom)
    if (params?.validityTo) searchParams.set('validityTo', params.validityTo)

    const query = searchParams.toString()
    const url = query ? `${BASE_URL}/Get?${query}` : `${BASE_URL}/Get`
    return httpClient.get<Proposal[]>(url)
  },

  async getById(id: number): Promise<Proposal | undefined> {
    const response = await httpClient.get<Proposal>(`${BASE_URL}/${id}`)
    return response.data
  },

  create(data: CreateProposalRequest) {
    return httpClient.post<Proposal>(`${BASE_URL}/Create`, data)
  },

  update(id: number, data: UpdateProposalRequest) {
    return httpClient.put<Proposal>(`${BASE_URL}/${id}`, data)
  },

  send(id: number) {
    return httpClient.post<Proposal>(`${BASE_URL}/${id}/Send`, {})
  },

  approve(id: number) {
    return httpClient.post<Proposal>(`${BASE_URL}/${id}/Approve`, {})
  },

  reject(id: number) {
    return httpClient.post<Proposal>(`${BASE_URL}/${id}/Reject`, {})
  },

  markAsViewed(id: number) {
    return httpClient.post<Proposal>(`${BASE_URL}/${id}/MarkAsViewed`, {})
  },

  convertToCampaign(id: number, campaignId: number) {
    return httpClient.post<Proposal>(`${BASE_URL}/${id}/ConvertToCampaign`, { campaignId })
  },

  cancel(id: number) {
    return httpClient.post<Proposal>(`${BASE_URL}/${id}/Cancel`, {})
  },

  async getItems(proposalId: number): Promise<ProposalItem[]> {
    const response = await httpClient.get<ProposalItem[]>(`${BASE_URL}/${proposalId}/items/Get`)
    return response.data ?? []
  },

  createItem(proposalId: number, data: CreateProposalItemRequest) {
    return httpClient.post<ProposalItem>(`${BASE_URL}/${proposalId}/items/Create`, data)
  },

  updateItem(id: number, data: UpdateProposalItemRequest) {
    return httpClient.put<ProposalItem>(`${BASE_URL}/items/${id}`, data)
  },

  deleteItem(id: number) {
    return httpClient.delete(`${BASE_URL}/items/${id}`)
  },

  async getVersions(proposalId: number): Promise<ProposalVersion[]> {
    const response = await httpClient.get<ProposalVersion[]>(`${BASE_URL}/${proposalId}/versions/Get`)
    return response.data ?? []
  },

  async getVersionById(versionId: number): Promise<ProposalVersionDetail | null> {
    const response = await httpClient.get<ProposalVersionDetail>(`${BASE_URL}/versions/${versionId}`)
    return response.data ?? null
  },

  async getShareLinks(proposalId: number): Promise<ProposalShareLink[]> {
    const response = await httpClient.get<ProposalShareLink[]>(`${BASE_URL}/${proposalId}/share-links/Get`)
    return response.data ?? []
  },

  createShareLink(proposalId: number, data: CreateProposalShareLinkRequest) {
    return httpClient.post<ProposalShareLink>(`${BASE_URL}/${proposalId}/share-links/Create`, data)
  },

  revokeShareLink(shareLinkId: number) {
    return httpClient.post<ProposalShareLink>(`${BASE_URL}/share-links/${shareLinkId}/Revoke`, {})
  },

  async downloadPdf(id: number): Promise<void> {
    const response = await httpClient.get<Blob>(`${BASE_URL}/pdf/${id}`, { responseType: 'blob' })
    const blob = response.data
    if (!blob) return
    const url = window.URL.createObjectURL(blob as Blob)
    const link = document.createElement('a')
    link.href = url
    link.download = `proposta-${id}.pdf`
    document.body.appendChild(link)
    link.click()
    link.remove()
    window.URL.revokeObjectURL(url)
  },
}
