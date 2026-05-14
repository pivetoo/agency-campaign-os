import { test, expect } from '../fixtures/test'
import { expectPageTitle, pageTitle } from '../fixtures/helpers'

interface ScreenSpec {
  path: string
  expectHeading?: RegExp
}

const screens: ScreenSpec[] = [
  { path: '/', expectHeading: /Dashboard/i },
  { path: '/usuarios', expectHeading: /Usuários/i },
  { path: '/comercial/pipeline', expectHeading: /Pipeline/i },
  { path: '/comercial/oportunidades', expectHeading: /Oportunidades/i },
  { path: '/comercial/propostas', expectHeading: /Propostas/i },
  { path: '/comercial/aprovacoes', expectHeading: /Aprovações|Aprovacoes/i },
  { path: '/comercial/followups', expectHeading: /Atividades|Follow|Acompanhamento/i },
  { path: '/marcas', expectHeading: /Marcas/i },
  { path: '/creators', expectHeading: /Creators|Influenciadores/i },
  { path: '/campanhas', expectHeading: /Campanhas/i },
  { path: '/operacao/aprovacoes', expectHeading: /Aprovações|Aprovacoes/i },
  { path: '/financeiro/receber', expectHeading: /receber|recebimento/i },
  { path: '/financeiro/pagar', expectHeading: /pagar|pagamento/i },
  { path: '/financeiro/fluxo-caixa', expectHeading: /fluxo|caixa/i },
  { path: '/financeiro/aging', expectHeading: /aging|inadim|atraso/i },
]

test.describe('Smoke - modulo Sistema', () => {
  for (const screen of screens) {
    test(`abre ${screen.path} sem erros HTTP`, async ({ page, expectNoApiFailures }) => {
      await page.goto(screen.path)
      await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})

      if (screen.expectHeading) {
        // Dashboard usa greeting customizado ("Olá, ...") em vez de literal "Dashboard"
        if (screen.path === '/') {
          await expect(pageTitle(page).first()).toBeVisible({ timeout: 15_000 })
        } else {
          await expectPageTitle(page, screen.expectHeading, 15_000)
        }
      }

      // dar tempo para chamadas paralelas concluirem
      await page.waitForTimeout(500)

      expectNoApiFailures()
    })
  }
})
