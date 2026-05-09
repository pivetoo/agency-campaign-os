import { test as base, expect, type Page } from '@playwright/test'

interface ApiFailure {
  url: string
  status: number
  method: string
  body: string
}

export interface KanvasFixtures {
  apiFailures: ApiFailure[]
  expectNoApiFailures: () => void
}

const isApiUrl = (url: string) => {
  return /\/api\/(?!Notifications\/Get)/.test(url)
}

const isAuthRefresh = (url: string) => {
  return /\/connect\/(token|revocation)/.test(url) || /\/refresh/i.test(url)
}

export const test = base.extend<KanvasFixtures>({
  apiFailures: async ({ page }: { page: Page }, use) => {
    const failures: ApiFailure[] = []

    page.on('response', async (response) => {
      const status = response.status()
      const url = response.url()

      if (status < 400) return
      if (!isApiUrl(url)) return
      if (isAuthRefresh(url) && status === 401) return

      let body = ''
      try {
        body = (await response.text()).slice(0, 500)
      } catch {
        body = '<no body>'
      }

      failures.push({
        url,
        status,
        method: response.request().method(),
        body,
      })
    })

    await use(failures)
  },

  expectNoApiFailures: async ({ apiFailures }, use) => {
    const fn = () => {
      if (apiFailures.length === 0) return
      const summary = apiFailures
        .map((failure) => `  ${failure.method} ${failure.status} ${failure.url}\n    body: ${failure.body}`)
        .join('\n')
      throw new Error(`API retornou erro em ${apiFailures.length} request(s):\n${summary}`)
    }
    await use(fn)
  },
})

export { expect }
