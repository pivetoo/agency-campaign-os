import { test } from '../fixtures/test'
import { expectPageTitle } from '../fixtures/helpers'

interface ScreenSpec {
  path: string
  expectHeading?: RegExp
}

const screens: ScreenSpec[] = [
  { path: '/configuracao' },
  { path: '/configuracao/integracoes', expectHeading: /Integrações/i },
  { path: '/configuracao/pipeline-comercial', expectHeading: /Estágios|Funil|Pipeline/i },
  { path: '/configuracao/origens-oportunidade', expectHeading: /Origens/i },
  { path: '/configuracao/tags-oportunidade', expectHeading: /Tags/i },
  { path: '/configuracao/templates-proposta', expectHeading: /Templates de proposta|Proposta/i },
  { path: '/configuracao/blocos-proposta', expectHeading: /Blocos de proposta|Blocos/i },
  { path: '/configuracao/plataformas', expectHeading: /Redes sociais|Plataformas/i },
  { path: '/configuracao/status-creators', expectHeading: /Status dos (creators|influenciadores)|Status/i },
  { path: '/configuracao/tipos-entrega', expectHeading: /Tipos de entrega|Entrega/i },
  { path: '/configuracao/contas-financeiras', expectHeading: /Contas|Bancárias/i },
  { path: '/configuracao/subcategorias-financeiras', expectHeading: /Subcategorias|Categorias/i },
]

test.describe('Smoke - modulo Configuracao + Auditoria', () => {
  for (const screen of screens) {
    test(`abre ${screen.path} sem erros HTTP`, async ({ page, expectNoApiFailures }) => {
      await page.goto(screen.path)
      await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})

      if (screen.expectHeading) {
        await expectPageTitle(page, screen.expectHeading, 15_000)
      }

      await page.waitForTimeout(500)

      expectNoApiFailures()
    })
  }
})
