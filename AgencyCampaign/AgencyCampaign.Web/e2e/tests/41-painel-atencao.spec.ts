import { test, expect } from '../fixtures/test'
import { expectPageTitle } from '../fixtures/helpers'

// Smoke do A6: a pagina de Atencao (ultima do grupo Comercial) carrega sem erro de API.

test.describe('Comercial - painel de Atencao (A6)', () => {
  test('a pagina /comercial/atencao carrega sem erro de API', async ({ page, expectNoApiFailures }) => {
    await page.goto('/comercial/atencao')
    await expectPageTitle(page, /Aten/i)

    await expect(page.getByText(/^Atenção$/).first()).toBeVisible({ timeout: 20_000 })
    await expect(page.getByText(/Não foi possível carregar os alertas|Could not load alerts/i)).toHaveCount(0)

    expectNoApiFailures()
  })
})
