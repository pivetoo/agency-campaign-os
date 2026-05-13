import { useEffect, useRef } from 'react'
import * as signalR from '@microsoft/signalr'
import { getApiBaseURL } from 'archon-ui'
import type { WhatsAppConversation, WhatsAppMessage } from '../types/whatsApp'

interface UseWhatsAppHubOptions {
  onNewMessage?: (conversationId: number, message: WhatsAppMessage) => void
  onConversationUpdated?: (conversationId: number, preview: string | null, unreadCount: number) => void
  onMessageSendFailed?: (conversationId: number, messageId: number, error: string) => void
  enabled?: boolean
}

export function useWhatsAppHub({ onNewMessage, onConversationUpdated, onMessageSendFailed, enabled = true }: UseWhatsAppHubOptions) {
  const connectionRef = useRef<signalR.HubConnection | null>(null)
  const onNewMessageRef = useRef(onNewMessage)
  const onConversationUpdatedRef = useRef(onConversationUpdated)
  const onMessageSendFailedRef = useRef(onMessageSendFailed)

  useEffect(() => { onNewMessageRef.current = onNewMessage }, [onNewMessage])
  useEffect(() => { onConversationUpdatedRef.current = onConversationUpdated }, [onConversationUpdated])
  useEffect(() => { onMessageSendFailedRef.current = onMessageSendFailed }, [onMessageSendFailed])

  useEffect(() => {
    if (!enabled) {
      return
    }

    const token = localStorage.getItem('@Archon:accessToken')
    if (!token) {
      return
    }

    const baseUrl = getApiBaseURL() || ''
    const hubUrl = `${baseUrl}/hubs/whatsapp?access_token=${encodeURIComponent(token)}`

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, { skipNegotiation: true, transport: signalR.HttpTransportType.WebSockets })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build()

    connection.on('NewMessage', (conversationId: number, message: WhatsAppMessage) => {
      onNewMessageRef.current?.(conversationId, message)
    })

    connection.on('ConversationUpdated', (conversationId: number, preview: string | null, unreadCount: number) => {
      onConversationUpdatedRef.current?.(conversationId, preview, unreadCount)
    })

    connection.on('MessageSendFailed', (conversationId: number, messageId: number, error: string) => {
      onMessageSendFailedRef.current?.(conversationId, messageId, error)
    })

    connection.start().catch((err) => {
      console.warn('[WhatsAppHub] connection failed:', err)
    })

    connectionRef.current = connection

    return () => {
      connection.stop()
      connectionRef.current = null
    }
  }, [enabled])
}

export type { WhatsAppConversation, WhatsAppMessage }
