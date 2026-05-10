import { getApiBaseURL } from 'archon-ui'

// Resolve um caminho de asset (ex.: "/uploads/brands/.../1.webp") para uma
// URL absoluta apontando pro host do backend. Em producao funciona porque
// o frontend e o backend ficam no mesmo host (resolvido pelo browser); em
// localhost o Vite roda numa porta diferente do backend, entao caminhos
// relativos quebram. Esse helper resolve para o origin de VITE_API_BASE_URL.
export function resolveAssetUrl(path: string | null | undefined): string {
  if (!path) return ''
  if (path.startsWith('http://') || path.startsWith('https://') || path.startsWith('//') || path.startsWith('data:')) {
    return path
  }
  if (!path.startsWith('/')) return path

  const apiBase = getApiBaseURL()
  if (!apiBase) return path

  try {
    return `${new URL(apiBase).origin}${path}`
  } catch {
    return path
  }
}
