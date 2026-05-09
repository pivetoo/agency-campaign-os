import { test, expect } from '../fixtures/test'

test.describe('CRUD - entidades principais (criar)', () => {
  test('cria marca em /marcas', async ({ page, expectNoApiFailures }) => {
    const stamp = Date.now()
    const name = `E2E Marca ${stamp}`

    await page.goto('/marcas')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})

    await page.getByRole('button', { name: /^Incluir$|^Novo$|^Nova$/i }).first().click()

    const modal = page.getByRole('dialog').filter({ hasText: /Nova marca/i })
    await expect(modal).toBeVisible({ timeout: 10_000 })

    // primeiro Input do modal e o de Nome
    await modal.locator('input[type="text"], input:not([type])').first().fill(name)

    await modal.getByRole('button', { name: /Salvar/i }).first().click()

    await expect(modal).toBeHidden({ timeout: 15_000 })
    await expect(page.getByText(name).first()).toBeVisible({ timeout: 15_000 })

    expectNoApiFailures()
  })

  test('cria creator em /creators', async ({ page, expectNoApiFailures }) => {
    const stamp = Date.now()
    const name = `E2E Creator ${stamp}`

    await page.goto('/creators')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})

    await page.getByRole('button', { name: /^Incluir$|^Novo$|^Nova$/i }).first().click()

    const modal = page.getByRole('dialog').filter({ hasText: /Novo influenciador|Novo creator/i })
    await expect(modal).toBeVisible({ timeout: 10_000 })

    await modal.locator('input[type="text"], input:not([type])').first().fill(name)

    await modal.getByRole('button', { name: /Salvar/i }).first().click()

    await expect(modal).toBeHidden({ timeout: 15_000 })
    await expect(page.getByText(name).first()).toBeVisible({ timeout: 15_000 })

    expectNoApiFailures()
  })

  test('cria template de e-mail em /configuracao/templates-email', async ({ page, expectNoApiFailures }) => {
    const stamp = Date.now()
    const name = `E2E Template ${stamp}`

    await page.goto('/configuracao/templates-email')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})

    await page.getByRole('button', { name: /^Incluir$|^Novo$/i }).first().click()

    const modal = page.getByRole('dialog').filter({ hasText: /Novo template|template/i })
    await expect(modal).toBeVisible({ timeout: 10_000 })

    // Nome (placeholder unico)
    await modal.getByPlaceholder(/Confirmação de envio|envio de proposta/i).fill(name)

    // Evento (SearchableSelect) — clicar no trigger e escolher primeira
    const eventoTrigger = modal.locator(':text("Evento")').locator('..').locator('button, [role="combobox"]').first()
    await eventoTrigger.click()
    const firstEventOption = page.locator('[role="option"]').first()
    await expect(firstEventOption).toBeVisible({ timeout: 10_000 })
    await firstEventOption.click()

    // Assunto
    await modal.getByPlaceholder(/proposta.*chegou|Assunto/i).fill(`Assunto ${stamp}`)

    // Corpo (textarea) — pegar o primeiro textarea
    await modal.locator('textarea').first().fill(`<p>Corpo ${stamp}</p>`)

    await modal.getByRole('button', { name: /Salvar/i }).first().click()

    await expect(modal).toBeHidden({ timeout: 15_000 })
    await expect(page.getByText(name).first()).toBeVisible({ timeout: 15_000 })

    expectNoApiFailures()
  })

  test('cria campanha em /campanhas', async ({ page, expectNoApiFailures }) => {
    const stamp = Date.now()
    const name = `E2E Campanha ${stamp}`

    await page.goto('/campanhas')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})

    await page.getByRole('button', { name: /^Incluir$|^Novo$|^Nova$/i }).first().click()

    const modal = page.getByRole('dialog').filter({ hasText: /Nova campanha/i })
    await expect(modal).toBeVisible({ timeout: 10_000 })

    // Marca (SearchableSelect)
    const marcaTrigger = modal.locator(':text("Marca")').locator('..').locator('button, [role="combobox"]').first()
    await marcaTrigger.click()
    const firstBrand = page.locator('[role="option"]').first()
    await expect(firstBrand).toBeVisible({ timeout: 10_000 })
    await firstBrand.click()

    // Nome — segundo input de texto (apos status que e select)
    const textInputs = modal.locator('input[type="text"], input:not([type])')
    await textInputs.first().fill(name)

    // Budget (number)
    await modal.locator('input[type="number"]').first().fill('10000')

    // Inicio (date) — primeiro input date
    const dateInput = modal.locator('input[type="date"]').first()
    await dateInput.fill('2026-06-01')

    await modal.getByRole('button', { name: /Salvar/i }).first().click()

    await expect(modal).toBeHidden({ timeout: 15_000 })
    await expect(page.getByText(name).first()).toBeVisible({ timeout: 15_000 })

    expectNoApiFailures()
  })
})
