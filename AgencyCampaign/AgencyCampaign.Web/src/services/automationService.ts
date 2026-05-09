import { httpClient } from 'archon-ui'
import type { Automation, CreateAutomationPayload, UpdateAutomationPayload } from '../types/automation'

const API_URL = '/Automations'

export const automationService = {
  async getAutomations(page = 1, pageSize = 10): Promise<{ items: Automation[]; pagination: { totalItems: number; totalPages: number; currentPage: number; pageSize: number } }> {
    const response = await httpClient.get<Automation[]>(`${API_URL}/Get?page=${page}&pageSize=${pageSize}`)
    const items = Array.isArray(response.data) ? response.data : []
    const meta = (response as unknown as { pagination?: { totalCount?: number; totalItems?: number; totalPages?: number; page?: number; currentPage?: number; pageSize?: number } }).pagination
    const pagination = {
      totalItems: meta?.totalItems ?? meta?.totalCount ?? items.length,
      totalPages: meta?.totalPages ?? 1,
      currentPage: meta?.currentPage ?? meta?.page ?? page,
      pageSize: meta?.pageSize ?? pageSize,
    }
    return { items, pagination }
  },

  async getAutomationById(id: number): Promise<Automation> {
    const response = await httpClient.get(`${API_URL}/GetById/${id}`)
    return response.data
  },

  async createAutomation(payload: CreateAutomationPayload): Promise<Automation> {
    const response = await httpClient.post(`${API_URL}/Create`, payload)
    return response.data
  },

  async updateAutomation(id: number, payload: UpdateAutomationPayload): Promise<Automation> {
    const response = await httpClient.put(`${API_URL}/Update/${id}`, payload)
    return response.data
  },
}
