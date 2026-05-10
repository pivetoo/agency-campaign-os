import { getApiBaseURL } from 'archon-ui'

export const resolveUploadUrl = (path?: string | null): string | undefined => {
  if (!path) {
    return undefined
  }

  if (/^https?:\/\//i.test(path)) {
    return path
  }

  const base = (getApiBaseURL() || '').replace(/\/api\/?$/, '').replace(/\/+$/, '')
  return `${base}${path.startsWith('/') ? '' : '/'}${path}`
}
