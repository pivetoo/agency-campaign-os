export const env = {
  baseURL: process.env.E2E_BASE_URL ?? 'https://kanvas.mainstay.com.br',
  user: process.env.E2E_USER ?? '',
  password: process.env.E2E_PASSWORD ?? '',
}

export function assertCredentials() {
  if (!env.user || !env.password) {
    throw new Error(
      'Credenciais ausentes. Defina E2E_USER e E2E_PASSWORD no e2e/.env (use .env.example como base).'
    )
  }
}
