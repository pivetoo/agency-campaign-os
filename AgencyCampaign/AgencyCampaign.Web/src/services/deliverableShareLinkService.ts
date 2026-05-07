import { httpClient } from 'archon-ui'
import type {
  DeliverablePublicView,
  DeliverableShareLink,
  PendingApproval,
} from '../types/deliverableShareLink'

export interface CreateDeliverableShareLinkRequest {
  campaignDeliverableId: number
  reviewerName: string
  expiresAt?: string | null
}

export interface PublicDecisionRequest {
  reviewerName: string
  comment?: string | null
}

const SHARE_BASE = '/DeliverableShareLinks'
const PENDING_BASE = '/DeliverablePendingApprovals'

export const deliverableShareLinkService = {
  async getByDeliverable(deliverableId: number): Promise<DeliverableShareLink[]> {
    const response = await httpClient.get<DeliverableShareLink[]>(`${SHARE_BASE}/deliverable/${deliverableId}`)
    return response.data ?? []
  },

  create(data: CreateDeliverableShareLinkRequest) {
    return httpClient.post<DeliverableShareLink>(`${SHARE_BASE}/Create`, data)
  },

  revoke(id: number) {
    return httpClient.post(`${SHARE_BASE}/revoke/${id}`, {})
  },
}

export const deliverableApprovalsService = {
  async getPending(): Promise<PendingApproval[]> {
    const response = await httpClient.get<PendingApproval[]>(`${PENDING_BASE}/pending`)
    return response.data ?? []
  },
}

const PUBLIC_BASE = '/deliverable-public'

export const deliverablePublicService = {
  async getByToken(token: string): Promise<DeliverablePublicView | null> {
    try {
      const response = await httpClient.get<DeliverablePublicView>(`${PUBLIC_BASE}/${encodeURIComponent(token)}`)
      return response.data ?? null
    } catch {
      return null
    }
  },

  approve(token: string, request: PublicDecisionRequest) {
    return httpClient.post<DeliverablePublicView>(`${PUBLIC_BASE}/${encodeURIComponent(token)}/approve`, request)
  },

  reject(token: string, request: PublicDecisionRequest) {
    return httpClient.post<DeliverablePublicView>(`${PUBLIC_BASE}/${encodeURIComponent(token)}/reject`, request)
  },
}
