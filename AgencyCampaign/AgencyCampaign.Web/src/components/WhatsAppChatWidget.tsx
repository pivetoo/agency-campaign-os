import { useEffect, useRef, useState } from 'react'
import { ChevronLeft, Send, X } from 'lucide-react'

interface Conversation {
  id: number
  name: string
  phone: string
  lastMessage: string
  time: string
  unread: number
}

interface Message {
  id: number
  text: string
  from: 'me' | 'contact'
  time: string
}

const mockConversations: Conversation[] = [
  { id: 1, name: 'João Silva', phone: '+55 11 98765-0001', lastMessage: 'Oi! Tudo bem?', time: '10:32', unread: 2 },
  { id: 2, name: 'Maria Santos', phone: '+55 11 91234-0002', lastMessage: 'Obrigada pelo contato!', time: '09:15', unread: 0 },
  { id: 3, name: 'Pedro Costa', phone: '+55 21 99876-0003', lastMessage: 'Pode me enviar o contrato?', time: 'Ontem', unread: 1 },
  { id: 4, name: 'Ana Ferreira', phone: '+55 31 98888-0004', lastMessage: 'Combinado, até sexta!', time: 'Ontem', unread: 0 },
]

const mockMessages: Record<number, Message[]> = {
  1: [
    { id: 1, text: 'Oi! Tudo bem?', from: 'contact', time: '10:28' },
    { id: 2, text: 'Olá, João! Tudo ótimo. Temos uma proposta para você.', from: 'me', time: '10:30' },
    { id: 3, text: 'Que ótimo, me conta mais!', from: 'contact', time: '10:32' },
  ],
  2: [
    { id: 1, text: 'Olá, Maria! Entramos em contato sobre a campanha de verão.', from: 'me', time: '09:10' },
    { id: 2, text: 'Obrigada pelo contato!', from: 'contact', time: '09:15' },
  ],
  3: [
    { id: 1, text: 'Pedro, seguem os detalhes da parceria.', from: 'me', time: 'Ontem' },
    { id: 2, text: 'Pode me enviar o contrato?', from: 'contact', time: 'Ontem' },
  ],
  4: [
    { id: 1, text: 'Ana, confirmado o briefing para sexta?', from: 'me', time: 'Ontem' },
    { id: 2, text: 'Combinado, até sexta!', from: 'contact', time: 'Ontem' },
  ],
}

function Avatar({ name, size = 40 }: { name: string; size?: number }) {
  const initials = name.split(' ').slice(0, 2).map((w) => w[0]).join('').toUpperCase()
  const colors = ['#0EA5E9', '#8B5CF6', '#F59E0B', '#10B981', '#EF4444', '#EC4899']
  const color = colors[name.charCodeAt(0) % colors.length]
  return (
    <div
      className="flex shrink-0 items-center justify-center rounded-full text-white font-medium"
      style={{ width: size, height: size, background: color, fontSize: size * 0.36 }}
    >
      {initials}
    </div>
  )
}

function WhatsAppIcon({ size = 24, color = 'white' }: { size?: number; color?: string }) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" fill={color}>
      <path d="M17.472 14.382c-.297-.149-1.758-.867-2.03-.967-.273-.099-.471-.148-.67.15-.197.297-.767.966-.94 1.164-.173.199-.347.223-.644.075-.297-.15-1.255-.463-2.39-1.475-.883-.788-1.48-1.761-1.653-2.059-.173-.297-.018-.458.13-.606.134-.133.298-.347.446-.52.149-.174.198-.298.298-.497.099-.198.05-.371-.025-.52-.075-.149-.669-1.612-.916-2.207-.242-.579-.487-.5-.669-.51-.173-.008-.371-.01-.57-.01-.198 0-.52.074-.792.372-.272.297-1.04 1.016-1.04 2.479 0 1.462 1.065 2.875 1.213 3.074.149.198 2.096 3.2 5.077 4.487.709.306 1.262.489 1.694.625.712.227 1.36.195 1.871.118.571-.085 1.758-.719 2.006-1.413.248-.694.248-1.289.173-1.413-.074-.124-.272-.198-.57-.347m-5.421 7.403h-.004a9.87 9.87 0 01-5.031-1.378l-.361-.214-3.741.982.998-3.648-.235-.374a9.86 9.86 0 01-1.51-5.26c.001-5.45 4.436-9.884 9.888-9.884 2.64 0 5.122 1.03 6.988 2.898a9.825 9.825 0 012.893 6.994c-.003 5.45-4.437 9.884-9.885 9.884m8.413-18.297A11.815 11.815 0 0012.05 0C5.495 0 .16 5.335.157 11.892c0 2.096.547 4.142 1.588 5.945L.057 24l6.305-1.654a11.882 11.882 0 005.683 1.448h.005c6.554 0 11.89-5.335 11.893-11.893a11.821 11.821 0 00-3.48-8.413z" />
    </svg>
  )
}

export default function WhatsAppChatWidget() {
  const [isOpen, setIsOpen] = useState(false)
  const [activeId, setActiveId] = useState<number | null>(null)
  const [conversations, setConversations] = useState<Conversation[]>(mockConversations)
  const [messages, setMessages] = useState<Record<number, Message[]>>(mockMessages)
  const [input, setInput] = useState('')
  const messagesEndRef = useRef<HTMLDivElement>(null)

  const totalUnread = conversations.reduce((sum, c) => sum + c.unread, 0)
  const activeConversation = conversations.find((c) => c.id === activeId) ?? null
  const activeMessages = activeId ? (messages[activeId] ?? []) : []

  useEffect(() => {
    if (activeId) {
      messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' })
    }
  }, [activeMessages, activeId])

  const openConversation = (id: number) => {
    setActiveId(id)
    setConversations((prev) => prev.map((c) => (c.id === id ? { ...c, unread: 0 } : c)))
  }

  const sendMessage = () => {
    if (!input.trim() || !activeId) return
    const now = new Date()
    const time = `${now.getHours().toString().padStart(2, '0')}:${now.getMinutes().toString().padStart(2, '0')}`
    const newMessage: Message = { id: Date.now(), text: input.trim(), from: 'me', time }
    setMessages((prev) => ({ ...prev, [activeId]: [...(prev[activeId] ?? []), newMessage] }))
    setConversations((prev) =>
      prev.map((c) => (c.id === activeId ? { ...c, lastMessage: input.trim(), time } : c)),
    )
    setInput('')
  }

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault()
      sendMessage()
    }
  }

  return (
    <div className="fixed bottom-6 right-6 z-50 flex flex-col items-end gap-3">
      {isOpen && (
        <div className="flex h-[520px] w-[360px] flex-col overflow-hidden rounded-2xl shadow-2xl ring-1 ring-black/10">
          {/* Header */}
          <div className="flex items-center gap-3 bg-[#075E54] px-4 py-3 text-white">
            {activeConversation ? (
              <>
                <button
                  type="button"
                  onClick={() => setActiveId(null)}
                  className="rounded p-0.5 hover:bg-white/10 transition-colors"
                >
                  <ChevronLeft size={20} />
                </button>
                <Avatar name={activeConversation.name} size={36} />
                <div className="flex-1 min-w-0">
                  <p className="truncate text-sm font-semibold leading-tight">{activeConversation.name}</p>
                  <p className="truncate text-xs text-white/70">{activeConversation.phone}</p>
                </div>
              </>
            ) : (
              <>
                <div className="flex h-9 w-9 items-center justify-center rounded-full bg-[#25D366]">
                  <WhatsAppIcon size={20} />
                </div>
                <div className="flex-1">
                  <p className="text-sm font-semibold leading-tight">WhatsApp</p>
                  <p className="text-xs text-white/70">Conversas recentes</p>
                </div>
              </>
            )}
            <button
              type="button"
              onClick={() => { setIsOpen(false); setActiveId(null) }}
              className="rounded p-0.5 hover:bg-white/10 transition-colors"
            >
              <X size={18} />
            </button>
          </div>

          {/* Conversation list */}
          {!activeConversation && (
            <div className="flex-1 overflow-y-auto bg-white divide-y divide-gray-100">
              {conversations.map((conv) => (
                <button
                  key={conv.id}
                  type="button"
                  onClick={() => openConversation(conv.id)}
                  className="flex w-full items-center gap-3 px-4 py-3 text-left hover:bg-gray-50 transition-colors"
                >
                  <Avatar name={conv.name} />
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center justify-between gap-2">
                      <span className="truncate text-sm font-medium text-gray-900">{conv.name}</span>
                      <span className="shrink-0 text-xs text-gray-400">{conv.time}</span>
                    </div>
                    <div className="flex items-center justify-between gap-2 mt-0.5">
                      <span className="truncate text-xs text-gray-500">{conv.lastMessage}</span>
                      {conv.unread > 0 && (
                        <span className="flex h-5 min-w-5 shrink-0 items-center justify-center rounded-full bg-[#25D366] px-1 text-[10px] font-bold text-white">
                          {conv.unread}
                        </span>
                      )}
                    </div>
                  </div>
                </button>
              ))}
            </div>
          )}

          {/* Message thread */}
          {activeConversation && (
            <>
              <div className="flex-1 overflow-y-auto bg-[#ECE5DD] px-4 py-3 space-y-1.5">
                {activeMessages.map((msg) => (
                  <div
                    key={msg.id}
                    className={`flex ${msg.from === 'me' ? 'justify-end' : 'justify-start'}`}
                  >
                    <div
                      className={`max-w-[78%] rounded-lg px-3 py-2 shadow-sm ${
                        msg.from === 'me'
                          ? 'rounded-tr-sm bg-[#DCF8C6] text-gray-800'
                          : 'rounded-tl-sm bg-white text-gray-800'
                      }`}
                    >
                      <p className="text-sm leading-snug">{msg.text}</p>
                      <p className="mt-1 text-right text-[10px] text-gray-400">{msg.time}</p>
                    </div>
                  </div>
                ))}
                <div ref={messagesEndRef} />
              </div>

              {/* Input */}
              <div className="flex items-end gap-2 bg-[#F0F0F0] px-3 py-2.5">
                <textarea
                  value={input}
                  onChange={(e) => setInput(e.target.value)}
                  onKeyDown={handleKeyDown}
                  placeholder="Digite uma mensagem..."
                  rows={1}
                  className="flex-1 resize-none rounded-2xl bg-white px-3 py-2 text-sm text-gray-800 placeholder-gray-400 outline-none ring-0 focus:ring-1 focus:ring-[#25D366]/50 max-h-24 overflow-y-auto"
                  style={{ scrollbarWidth: 'none' }}
                />
                <button
                  type="button"
                  onClick={sendMessage}
                  disabled={!input.trim()}
                  className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-[#25D366] text-white transition-colors hover:bg-[#22C35E] disabled:opacity-40"
                >
                  <Send size={16} />
                </button>
              </div>
            </>
          )}
        </div>
      )}

      {/* Floating button */}
      <button
        type="button"
        onClick={() => { setIsOpen((v) => !v); if (!isOpen) setActiveId(null) }}
        className="relative flex h-14 w-14 items-center justify-center rounded-full bg-[#25D366] shadow-lg transition-transform hover:scale-105 active:scale-95"
        aria-label="Abrir WhatsApp"
      >
        {isOpen ? <X size={24} color="white" /> : <WhatsAppIcon size={28} />}
        {!isOpen && totalUnread > 0 && (
          <span className="absolute -right-1 -top-1 flex h-5 min-w-5 items-center justify-center rounded-full bg-red-500 px-1 text-[10px] font-bold text-white ring-2 ring-white">
            {totalUnread}
          </span>
        )}
      </button>
    </div>
  )
}
