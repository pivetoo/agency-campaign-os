import { createContext, useCallback, useContext, useMemo, useState } from 'react'
import type { ReactNode } from 'react'

interface TourContextValue {
  isOpen: boolean
  openTour: () => void
  closeTour: () => void
}

const TourContext = createContext<TourContextValue | null>(null)

export function TourProvider({ children }: { children: ReactNode }) {
  const [isOpen, setIsOpen] = useState(false)

  const openTour = useCallback(() => setIsOpen(true), [])
  const closeTour = useCallback(() => setIsOpen(false), [])

  const value = useMemo(() => ({ isOpen, openTour, closeTour }), [isOpen, openTour, closeTour])

  return <TourContext.Provider value={value}>{children}</TourContext.Provider>
}

export function useTour(): TourContextValue {
  const ctx = useContext(TourContext)
  if (!ctx) {
    throw new Error('useTour deve ser usado dentro de <TourProvider>')
  }
  return ctx
}
