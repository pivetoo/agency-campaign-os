import { test, expect } from '../fixtures/test'
import { expectPageTitle } from '../fixtures/helpers'

// Smoke do A6: a pagina de Atencao carrega, mostra os chips de resumo e a secao
// de alertas, sem erro de API (os 4 endpoints ja existiam no backend sem tela).

test.describe('Comercial - painel de Atencao (A6)', () => {
  test('a pagina /comercial/atencao carrega sem erro de API', async ({ page, expectNoApiFailures }) => {
    await page.goto('/comercial/atencao')
    await expectPageTitle(page, /Aten/i)

    // a secao de alertas resolve para uma das tres saidas: lista, vazio ou (nunca) erro
    const alertsResolved = page.getByText(/Alertas/i).first()
    await expect(alertsResolved).toBeVisible({ timeout: 20_000 })

    // nao deve mostrar o estado de erro de carregamento
    await expect(page.getByText(/Não foi possível carregar os alertas|Could not load alerts/i)).toHaveCount(0)

    expectNoApiFailures()
  })
})
