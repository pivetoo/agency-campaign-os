import { httpClient, buildPaginationQuery } from 'archon-ui'
import type { ApiResponse } from 'archon-ui'

const BASE_URL = '/ProposalBlocks'

export interface ProposalBlock {
  id: number
  name: string
  body: string
  category: string
  isActive: boolean
  createdByUserName?: string
  createdAt: string
  updatedAt?: string
}

export interface CreateProposalBlockRequest {
  name: string
  body: string
  category: string
}

export interface UpdateProposalBlockRequest extends CreateProposalBlockRequest {
  id: number
  isActive: boolean
}

export const proposalBlockService = {
  getAll(params?: { page?: number; pageSize?: number; search?: string; category?: string; includeInactive?: boolean }): Promise<ApiResponse<ProposalBlock[]>> {
    const query = buildPaginationQuery(params)
    const extras = new URLSearchParams()
    if (params?.search) extras.set('search', params.search)
    if (params?.category) extras.set('category', params.category)
    if (params?.includeInactive) extras.set('includeInactive', 'true')
    const extrasString = extras.toString()
    const url = extrasString
      ? `${BASE_URL}/Get${query}${query ? '&' : '?'}${extrasString}`
      : `${BASE_URL}/Get${query}`
    return httpClient.get<ProposalBlock[]>(url)
  },

  async getById(id: number): Promise<ProposalBlock | null> {
    const response = await httpClient.get<ProposalBlock>(`${BASE_URL}/${id}`)
    return response.data ?? null
  },

  create(data: CreateProposalBlockRequest) {
    return httpClient.post<ProposalBlock>(`${BASE_URL}/Create`, data)
  },

  update(id: number, data: UpdateProposalBlockRequest) {
    return httpClient.put<ProposalBlock>(`${BASE_URL}/${id}`, data)
  },

  delete(id: number) {
    return httpClient.delete(`${BASE_URL}/${id}`)
  },
}
