import { getApiBaseURL, getRequestLanguage } from 'archon-ui'

// Cliente HTTP para paginas PUBLICAS por token (portal do creator, /r, /d, /p). Diferente do
// httpClient da SPA logada, ele NAO envia Bearer, NAO faz refresh e NAO redireciona para o login
// OIDC da agencia no 401 - um link expirado/invalido deve mostrar a tela de "link invalido" da
// propria pagina publica, nao jogar o creator/cliente no login da agencia (beco sem saida). Lanca
// um erro com { status, message } em respostas >= 400; o chamador trata (try/catch -> estado vazio).
// Usa fetch nativo (axios fica encapsulado no archon-ui e nao e dependencia direta do app).
interface PublicResponse<T> {
  message: string
  data: T | null
}

interface PublicError {
  message: string
  status: number
  isApiError: true
}

interface PublicConfig {
  responseType?: 'blob'
}

const baseUrl = (): string => (getApiBaseURL() || '').replace(/\/+$/, '')

async function request<T>(method: 'GET' | 'POST', url: string, body?: unknown, config?: PublicConfig): Promise<PublicResponse<T>> {
  const headers: Record<string, string> = { 'Accept-Language': getRequestLanguage() }
  let payload: BodyInit | undefined

  if (body instanceof FormData) {
    payload = body
  } else if (body !== undefined) {
    headers['Content-Type'] = 'application/json'
    payload = JSON.stringify(body)
  }

  let response: Response
  try {
    response = await fetch(`${baseUrl()}${url}`, { method, headers, body: payload })
  } catch {
    throw { message: 'Falha de conexao.', status: 0, isApiError: true } as PublicError
  }

  if (!response.ok) {
    let message = ''
    try {
      const error = await response.json()
      message = error?.message || error?.title || ''
    } catch {
      message = ''
    }
    throw { message: message || 'Nao foi possivel completar a solicitacao.', status: response.status, isApiError: true } as PublicError
  }

  if (config?.responseType === 'blob') {
    const blob = await response.blob()
    return { message: '', data: blob as unknown as T }
  }

  const text = await response.text()
  if (!text) {
    return { message: '', data: null }
  }

  const raw = JSON.parse(text)
  if (raw && typeof raw === 'object' && 'message' in raw) {
    return raw as PublicResponse<T>
  }
  return { message: '', data: raw as T }
}

export const publicClient = {
  get<T = unknown>(url: string, config?: PublicConfig): Promise<PublicResponse<T>> {
    return request<T>('GET', url, undefined, config)
  },
  post<T = unknown>(url: string, data?: unknown, config?: PublicConfig): Promise<PublicResponse<T>> {
    return request<T>('POST', url, data, config)
  },
}
