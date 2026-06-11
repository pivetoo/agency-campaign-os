import { test, expect } from '../fixtures/test'
import { expectPageTitle } from '../fixtures/helpers'

// Smoke do M4: o relatorio de receita por creator carrega sem erro de API.

test.describe('Relatorios - Receita por Creator (M4)', () => {
  test('a pagina /relatorios/comercial/receita-creator carrega sem erro de API', async ({ page, expectNoApiFailures }) => {
    await page.goto('/relatorios/comercial/receita-creator')
    await expectPageTitle(page, /Receita por Creator/i)
    expectNoApiFailures()
  })
})
