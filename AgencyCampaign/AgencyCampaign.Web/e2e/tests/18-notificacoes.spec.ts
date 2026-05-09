import { test, expect } from '../fixtures/test'

test.describe('Notificacoes - sino + dropdown', () => {
  test('abre dropdown de notificacoes pela navbar', async ({ page, expectNoApiFailures }) => {
    await page.goto('/')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})

    // sino na navbar (data-tour="notifications-bell")
    const bell = page.locator('[data-tour="notifications-bell"] button').first()
    await expect(bell).toBeVisible({ timeout: 10_000 })
    await bell.click()

    // dropdown com header de notificacoes
    await expect(page.getByText(/Notificações|Notifications/i).first()).toBeVisible({ timeout: 10_000 })

    // fechar clicando fora (overlay)
    await page.mouse.click(50, 50)
    await page.waitForTimeout(300)

    expectNoApiFailures()
  })
})
