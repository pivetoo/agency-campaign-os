import { test, expect } from '../fixtures/test'
import { crud, rowWithText, expectPageTitle } from '../fixtures/helpers'

// Spec 33: Contas a Pagar (paridade do spec 14 que cobria "a receber")
// Cria entry tipo "A pagar" e marca como Pago.

test.describe('Financeiro - contas a pagar (caminho critico)', () => {
  test('cria entry pendente em /financeiro/pagar e marca como pago', async ({ page, expectNoApiFailures }) => {
    const stamp = Date.now()
    const description = `E2E Pagar fornecedor ${stamp}`

    // 1) cria entry a pagar
    await page.goto('/financeiro/pagar')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})

    // header esperado: "Contas a pagar"
    await expectPageTitle(page, /Contas a pagar/i, 15_000)

    await crud.add(page).click()
    const modal = page.getByRole('dialog').filter({ hasText: /Novo lançamento/i })
    await expect(modal).toBeVisible({ timeout: 10_000 })

    const fieldContainer = (label: string) =>
      modal.locator(`div.space-y-2:has(> label:text-is("${label}"))`).first()

    // o defaultType vem como 2 (A pagar) automaticamente — confere
    const tipoTrigger = fieldContainer('Tipo').locator('button, [role="combobox"]').first()
    await expect(tipoTrigger).toContainText(/A pagar/i)

    // conta (primeira ativa)
    const contaTrigger = fieldContainer('Conta').locator('button, [role="combobox"]').first()
    await contaTrigger.scrollIntoViewIfNeeded()
    await contaTrigger.dispatchEvent('click')
    await page.locator('[role="option"]').first().click()

    await fieldContainer('Descrição').locator('input').first().fill(description)
    await fieldContainer('Valor (R$)').locator('input').first().fill('1250')

    const today = new Date().toISOString().slice(0, 10)
    const future = new Date()
    future.setDate(future.getDate() + 14)
    const futureStr = future.toISOString().slice(0, 10)
    await fieldContainer('Ocorrência').locator('input').first().fill(today)
    await fieldContainer('Vencimento').locator('input').first().fill(futureStr)

    await modal.getByRole('button', { name: /^Salvar$/ }).first().click()
    await expect(modal).toBeHidden({ timeout: 15_000 })

    // 2) limpar filtro de data
    const dateInputs = page.locator('input[type="date"]')
    if (await dateInputs.count() >= 2) {
      await dateInputs.nth(0).fill('1900-01-01')
      await dateInputs.nth(1).fill('2100-12-31')
    }

    // 3) entry aparece
    const row = rowWithText(page, description).first()
    await expect(row).toBeVisible({ timeout: 15_000 })

    // 4) clicar Confirmar pra marcar como pago
    await row.getByRole('button', { name: /Confirmar/i }).click()
    const confirmModal = page.getByRole('dialog').filter({ hasText: /Confirmar pagamento|Confirmar recebimento/i })
    await expect(confirmModal).toBeVisible({ timeout: 10_000 })

    // conta de destino (primeira)
    const confirmField = (label: string) =>
      confirmModal.locator(`div.space-y-2:has(> label:text-is("${label}"))`).first()
    const accountTrigger = confirmField('Conta de destino').locator('button, [role="combobox"]').first()
    await accountTrigger.click().catch(() => {})
    const firstOpt = page.locator('[role="option"]').first()
    if (await firstOpt.count()) await firstOpt.click()

    await confirmModal.getByRole('button', { name: /Confirmar|^Salvar$|^Marcar/i }).first().click()
    await expect(confirmModal).toBeHidden({ timeout: 15_000 })

    // 5) status vira "Pago"
    const updatedRow = page.locator('[data-row="true"]', { hasText: description }).first()
    await expect(updatedRow).toBeVisible({ timeout: 15_000 })
    await expect(updatedRow.getByText(/^Pago$/i)).toBeVisible({ timeout: 10_000 })

    expectNoApiFailures()
  })
})
