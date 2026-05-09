import { httpClient } from 'archon-ui'
import type { EmailTemplate, EmailEventTypeValue } from '../types/emailTemplate'

export interface CreateEmailTemplateRequest {
  name: string
  eventType: EmailEventTypeValue
  subject: string
  htmlBody: string
}

export interface UpdateEmailTemplateRequest extends CreateEmailTemplateRequest {
  id: number
  isActive: boolean
}

const BASE = '/EmailTemplates'

export const emailTemplateService = {
  async getAll(includeInactive = false): Promise<EmailTemplate[]> {
    const url = includeInactive ? `${BASE}/Get?includeInactive=true` : `${BASE}/Get`
    const response = await httpClient.get<EmailTemplate[]>(url)
    return response.data ?? []
  },

  async getById(id: number): Promise<EmailTemplate | null> {
    const response = await httpClient.get<EmailTemplate>(`${BASE}/GetById/${id}`)
    return response.data ?? null
  },

  create(data: CreateEmailTemplateRequest) {
    return httpClient.post<EmailTemplate>(`${BASE}/Create`, data)
  },

  update(id: number, data: UpdateEmailTemplateRequest) {
    return httpClient.put<EmailTemplate>(`${BASE}/Update/${id}`, data)
  },

  delete(id: number) {
    return httpClient.delete(`${BASE}/Delete/${id}`)
  },
}
