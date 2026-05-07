import { useCallback, useState } from 'react'

const STORAGE_KEY = 'kanvas_product_tour_completed_v1'

function loadCompleted(): boolean {
  if (typeof window === 'undefined') return false
  try {
    return window.localStorage.getItem(STORAGE_KEY) === 'true'
  } catch {
    return false
  }
}

export function useProductTour() {
  const [completed, setCompleted] = useState<boolean>(() => loadCompleted())

  const markCompleted = useCallback(() => {
    setCompleted(true)
    try {
      window.localStorage.setItem(STORAGE_KEY, 'true')
    } catch {
      // ignore quota errors
    }
  }, [])

  const reset = useCallback(() => {
    setCompleted(false)
    try {
      window.localStorage.removeItem(STORAGE_KEY)
    } catch {
      // ignore
    }
  }, [])

  return { completed, markCompleted, reset }
}
