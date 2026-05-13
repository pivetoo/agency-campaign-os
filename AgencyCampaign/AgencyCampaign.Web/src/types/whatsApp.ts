export interface WhatsAppConversation {
  id: number
  connectorId?: number | null
  contactPhone: string
  contactName?: string | null
  lastMessageAt?: string | null
  lastMessagePreview?: string | null
  unreadCount: number
  isActive: boolean
  createdAt: string
}

export interface WhatsAppMessage {
  id: number
  conversationId: number
  externalId?: string | null
  direction: 1 | 2
  content: string
  sentAt: string
  isRead: boolean
  createdAt: string
}
