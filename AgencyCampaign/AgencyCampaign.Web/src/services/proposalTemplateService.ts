import { httpClient, buildPaginationQuery } from 'archon-ui'
import type { ApiResponse } from 'archon-ui'

const BASE_URL = '/ProposalTemplates'

export interface ProposalTemplateItem {
  id?: number
  proposalTemplateId?: number
  description: string
  defaultQuantity: number
  defaultUnitPrice: number
  defaultDeliveryDays?: number
  observations?: string
  displayOrder: number
}

export interface ProposalTemplate {
  id: number
  name: string
  description?: string
  isActive: boolean
  createdByUserName?: string
  createdAt: string
  updatedAt?: string
  items: ProposalTemplateItem[]
}

export interface CreateProposalTemplateRequest {
  name: string
  description?: string
  items: ProposalTemplateItem[]
}

export interface UpdateProposalTemplateRequest extends CreateProposalTemplateRequest {
  id: number
  isActive: boolean
}

export const proposalTemplateService = {
  getAll(params?: { page?: number; pageSize?: number; search?: string; includeInactive?: boolean }): Promise<ApiResponse<ProposalTemplate[]>> {
    const query = buildPaginationQuery(params)
    const searchParam = params?.search ? `${query ? '&' : '?'}search=${encodeURIComponent(params.search)}` : ''
    const inactiveParam = params?.includeInactive ? `${query || searchParam ? '&' : '?'}includeInactive=true` : ''
    return httpClient.get<ProposalTemplate[]>(`${BASE_URL}/Get${query}${searchParam}${inactiveParam}`)
  },

  async getById(id: number): Promise<ProposalTemplate | null> {
    const response = await httpClient.get<ProposalTemplate>(`${BASE_URL}/${id}`)
    return response.data ?? null
  },

  create(data: CreateProposalTemplateRequest) {
    return httpClient.post<ProposalTemplate>(`${BASE_URL}/Create`, data)
  },

  update(id: number, data: UpdateProposalTemplateRequest) {
    return httpClient.put<ProposalTemplate>(`${BASE_URL}/${id}`, data)
  },

  delete(id: number) {
    return httpClient.delete(`${BASE_URL}/${id}`)
  },

  applyToProposal(templateId: number, proposalId: number) {
    return httpClient.post<{ itemsCreated: number }>(`${BASE_URL}/${templateId}/ApplyToProposal/${proposalId}`, {})
  },
}
