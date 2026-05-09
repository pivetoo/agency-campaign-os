import { useOutletContext } from 'react-router-dom'
import type { PortalSession } from '../../services/creatorPortalService'

export interface PortalContext {
  token: string
  session: PortalSession
  refresh: () => void
}

export function usePortalContext(): PortalContext {
  return useOutletContext<PortalContext>()
}
