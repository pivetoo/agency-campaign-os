import { httpClient } from 'archon-ui'
import type { WhatsAppConversation, WhatsAppMessage } from '../types/whatsApp'

const BASE = '/WhatsAppConversations'

export const whatsAppService = {
  async getConversations(page = 1, pageSize = 30): Promise<WhatsAppConversation[]> {
    const res = await httpClient.get<WhatsAppConversation[]>(`${BASE}/Get?page=${page}&pageSize=${pageSize}`)
    return Array.isArray(res.data) ? res.data : []
  },

  async getMessages(conversationId: number): Promise<WhatsAppMessage[]> {
    const res = await httpClient.get<WhatsAppMessage[]>(`${BASE}/messages/${conversationId}`)
    return Array.isArray(res.data) ? res.data : []
  },

  async sendMessage(conversationId: number, message: string): Promise<WhatsAppMessage> {
    const res = await httpClient.post<WhatsAppMessage>(`${BASE}/send/${conversationId}`, { message })
    return res.data
  },

  async markAsRead(conversationId: number): Promise<void> {
    await httpClient.put(`${BASE}/read/${conversationId}`)
  },
}
