import { httpClient } from 'archon-ui'
import type { Automation, CreateAutomationPayload, UpdateAutomationPayload } from '../types/automation'

const API_URL = '/Automations'

export const automationService = {
  async getAutomations(page = 1, pageSize = 10): Promise<{ items: Automation[]; pagination: { totalItems: number; totalPages: number; currentPage: number; pageSize: number } }> {
    const response = await httpClient.get(`${API_URL}/Get?page=${page}&pageSize=${pageSize}`)
    return response.data
  },

  async getAutomationById(id: number): Promise<Automation> {
    const response = await httpClient.get(`${API_URL}/${id}`)
    return response.data
  },

  async createAutomation(payload: CreateAutomationPayload): Promise<Automation> {
    const response = await httpClient.post(`${API_URL}/Create`, payload)
    return response.data
  },

  async updateAutomation(id: number, payload: UpdateAutomationPayload): Promise<Automation> {
    const response = await httpClient.put(`${API_URL}/${id}`, payload)
    return response.data
  },
}
