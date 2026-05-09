import { test, expect } from '../fixtures/test'

test.describe('Tour pelo sistema (smoke)', () => {
  test('abre tour, avanca um step e fecha', async ({ page, expectNoApiFailures }) => {
    await page.goto('/')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})

    // botao do tour no Dashboard
    const tourButton = page.getByRole('button', { name: /Tour pelo sistema|Refazer tour/i }).first()
    await expect(tourButton).toBeVisible({ timeout: 15_000 })
    await tourButton.click()

    // tooltip do react-joyride aparece com role=alertdialog ou tem texto "Próximo"
    const tourPopup = page.locator('.react-joyride__tooltip, [role="alertdialog"]').first()
    await expect(tourPopup).toBeVisible({ timeout: 10_000 })

    // avanca um step
    const nextBtn = page.getByRole('button', { name: /Próximo|Next/i }).first()
    if (await nextBtn.count()) {
      await nextBtn.click()
      await page.waitForTimeout(500)
    }

    // fecha tour
    const closeBtn = page.getByRole('button', { name: /Fechar|Close|Skip|Pular/i }).first()
    if (await closeBtn.count()) {
      await closeBtn.click()
    }

    expectNoApiFailures()
  })
})
