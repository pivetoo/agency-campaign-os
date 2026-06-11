import { test, expect } from '../fixtures/test'
import { expectPageTitle } from '../fixtures/helpers'

// Smoke do painel de Atencao (consolidado no sheet de insights do Funil):
// abrir o Funil, abrir o sheet lateral e ver a secao de Atencao sem erro de API.

test.describe('Comercial - painel de Atencao no Funil (A6)', () => {
  test('o sheet de insights do Funil mostra a Atencao sem erro de API', async ({ page, expectNoApiFailures }) => {
    await page.goto('/comercial/pipeline')
    await expectPageTitle(page, /Funil/i)

    // abre o sheet lateral (botao Insights)
    await page.getByRole('button', { name: /Insights|Metas|Atenç/i }).first().click()

    // a secao de Atencao aparece dentro do sheet
    await expect(page.getByText(/^Atenção$/).first()).toBeVisible({ timeout: 20_000 })

    // nao deve mostrar o estado de erro de carregamento dos alertas
    await expect(page.getByText(/Não foi possível carregar os alertas|Could not load alerts/i)).toHaveCount(0)

    expectNoApiFailures()
  })
})
