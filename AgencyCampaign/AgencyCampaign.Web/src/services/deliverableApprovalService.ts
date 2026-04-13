import { httpClient } from 'archon-ui'
import type { DeliverableApproval } from '../types/deliverableApproval'

const BASE_URL = '/DeliverableApprovals'

export interface CreateDeliverableApprovalRequest {
  campaignDeliverableId: number
  approvalType: number
  reviewerName: string
  comment?: string
}

export interface UpdateDeliverableApprovalRequest {
  id: number
  reviewerName: string
  status: number
  comment?: string
}

export const deliverableApprovalService = {
  async getByDeliverable(campaignDeliverableId: number): Promise<DeliverableApproval[]> {
    const response = await httpClient.get<DeliverableApproval[]>(`${BASE_URL}/deliverable/${campaignDeliverableId}`)
    return response.data ?? []
  },

  create(data: CreateDeliverableApprovalRequest) {
    return httpClient.post<DeliverableApproval>(`${BASE_URL}/Create`, data)
  },

  update(id: number, data: UpdateDeliverableApprovalRequest) {
    return httpClient.put<DeliverableApproval>(`${BASE_URL}/${id}`, data)
  },
}
