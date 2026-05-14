import { test, expect } from '@playwright/test'
import { dashboardKpi, pageTitle } from '../fixtures/helpers'

test.describe('Smoke - login + dashboard', () => {
  test('usuario autenticado abre o dashboard com KPIs', async ({ page }) => {
    await page.goto('/')

    await expect(page).toHaveURL(/kanvas\.mainstay\.com\.br\//)

    await expect(dashboardKpi.campanhasAtivas(page)).toBeVisible({ timeout: 20_000 })
    await expect(dashboardKpi.marcas(page)).toBeVisible({ timeout: 20_000 })
    await expect(dashboardKpi.influenciadores(page)).toBeVisible({ timeout: 20_000 })
    await expect(dashboardKpi.entregasPendentes(page)).toBeVisible({ timeout: 20_000 })

    await expect(pageTitle(page).first()).toBeVisible({ timeout: 15_000 })
  })
})
