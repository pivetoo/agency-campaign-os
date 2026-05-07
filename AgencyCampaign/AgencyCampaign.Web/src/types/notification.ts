export const NotificationType = {
  Info: 0,
  Success: 1,
  Warning: 2,
  Error: 3,
} as const

export type NotificationTypeValue = (typeof NotificationType)[keyof typeof NotificationType]

export interface Notification {
  id: number
  userId?: number | null
  title: string
  message: string
  type: NotificationTypeValue
  isRead: boolean
  readAt?: string | null
  link?: string | null
  source?: string | null
  referenceEntityName?: string | null
  referenceEntityId?: string | null
  createdAt: string
  updatedAt?: string | null
}
