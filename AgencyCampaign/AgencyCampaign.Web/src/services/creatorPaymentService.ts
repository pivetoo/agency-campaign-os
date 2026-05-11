import { httpClient } from 'archon-ui'
import type {
  CreatorPayment,
  PaymentMethodValue,
  PaymentStatusValue,
} from '../types/creatorPayment'

const BASE_URL = '/CreatorPayments'

export interface CreateCreatorPaymentRequest {
  campaignCreatorId: number
  campaignDocumentId?: number
  grossAmount: number
  discounts?: number
  method: PaymentMethodValue
  description?: string
}

export interface UpdateCreatorPaymentRequest {
  id: number
  grossAmount: number
  discounts?: number
  method: PaymentMethodValue
  description?: string
}

export interface AttachInvoiceRequest {
  invoiceNumber?: string
  invoiceUrl?: string
  issuedAt?: string
}

export interface MarkCreatorPaymentPaidRequest {
  paidAt: string
  providerTransactionId?: string
  provider?: string
}

export interface SchedulePaymentBatchRequest {
  connectorId: number
  pipelineId: number
  creatorPaymentIds: number[]
  scheduledFor?: string
}

export const creatorPaymentService = {
  async getById(id: number): Promise<CreatorPayment | null> {
    const response = await httpClient.get<CreatorPayment>(`${BASE_URL}/GetById/${id}`)
    return response.data ?? null
  },

  async getByCampaign(campaignId: number): Promise<CreatorPayment[]> {
    const response = await httpClient.get<CreatorPayment[]>(`${BASE_URL}/campaign/${campaignId}`)
    return response.data ?? []
  },

  async getByStatus(status: PaymentStatusValue): Promise<CreatorPayment[]> {
    const response = await httpClient.get<CreatorPayment[]>(`${BASE_URL}/status/${status}`)
    return response.data ?? []
  },

  create(data: CreateCreatorPaymentRequest) {
    return httpClient.post<CreatorPayment>(`${BASE_URL}/Create`, data)
  },

  update(id: number, data: UpdateCreatorPaymentRequest) {
    return httpClient.put<CreatorPayment>(`${BASE_URL}/Update/${id}`, data)
  },

  attachInvoice(id: number, data: AttachInvoiceRequest) {
    return httpClient.post<CreatorPayment>(`${BASE_URL}/${id}/invoice`, data)
  },

  markPaid(id: number, data: MarkCreatorPaymentPaidRequest) {
    return httpClient.post<CreatorPayment>(`${BASE_URL}/${id}/mark-paid`, data)
  },

  cancel(id: number) {
    return httpClient.post<CreatorPayment>(`${BASE_URL}/${id}/cancel`, {})
  },

  scheduleBatch(data: SchedulePaymentBatchRequest) {
    return httpClient.post<CreatorPayment[]>(`${BASE_URL}/ScheduleBatch`, data)
  },
}
