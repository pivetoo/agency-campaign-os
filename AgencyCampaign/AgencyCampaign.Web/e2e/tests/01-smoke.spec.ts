import { test, expect } from '@playwright/test'

test.describe('Smoke - login + dashboard', () => {
  test('usuario autenticado abre o dashboard com KPIs', async ({ page }) => {
    await page.goto('/')

    await expect(page).toHaveURL(/kanvas\.mainstay\.com\.br\//)

    for (const label of ['Campanhas ativas', 'Marcas', 'Creators', 'Entregas pendentes']) {
      await expect(page.getByText(label, { exact: false }).first()).toBeVisible({ timeout: 20_000 })
    }

    await expect(page.getByRole('heading', { name: /Dashboard/i }).first()).toBeVisible()
  })
})
