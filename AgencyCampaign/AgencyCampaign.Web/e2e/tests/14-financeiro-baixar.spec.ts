import { test, expect } from '../fixtures/test'
import { crud, rowWithText } from '../fixtures/helpers'

test.describe('Financeiro - marcar como pago (caminho critico)', () => {
  test('cria entry pendente e marca como pago', async ({ page, expectNoApiFailures }) => {
    const stamp = Date.now()
    const description = `E2E Pagar ${stamp}`

    // 1) cria entry a receber
    await page.goto('/financeiro/receber')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})
    await crud.add(page).click()
    const modal = page.getByRole('dialog').filter({ hasText: /Novo lançamento/i })
    await expect(modal).toBeVisible({ timeout: 10_000 })

    const fieldContainer = (label: string) =>
      modal.locator(`div.space-y-2:has(> label:text-is("${label}"))`).first()

    const contaTrigger = fieldContainer('Conta').locator('button, [role="combobox"]').first()
    await contaTrigger.scrollIntoViewIfNeeded()
    await contaTrigger.dispatchEvent('click')
    await page.locator('[role="option"]').first().click()

    await fieldContainer('Descrição').locator('input').first().fill(description)
    await fieldContainer('Valor (R$)').locator('input').first().fill('3500')

    const today = new Date().toISOString().slice(0, 10)
    const future = new Date(); future.setDate(future.getDate() + 14)
    const futureStr = future.toISOString().slice(0, 10)
    await fieldContainer('Ocorrência').locator('input').first().fill(today)
    await fieldContainer('Vencimento').locator('input').first().fill(futureStr)

    await modal.getByRole('button', { name: /^Salvar$/i }).first().click()
    await expect(modal).toBeHidden({ timeout: 15_000 })

    // 2) limpar filtro de data pra ver entry
    const dateInputs = page.locator('input[type="date"]')
    if (await dateInputs.count() >= 2) {
      await dateInputs.nth(0).fill('1900-01-01')
      await dateInputs.nth(1).fill('2100-12-31')
    }

    // 3) localizar a linha da entry e clicar em "Confirmar" (marcar como pago)
    const row = rowWithText(page, description).first()
    await expect(row).toBeVisible({ timeout: 15_000 })
    await row.getByRole('button', { name: /Confirmar/i }).click()

    // 4) modal de confirmacao
    const confirmModal = page.getByRole('dialog').filter({ hasText: /Confirmar recebimento|Confirmar pagamento/i })
    await expect(confirmModal).toBeVisible({ timeout: 10_000 })

    // conta destino: pegar primeira opcao
    const confirmFieldContainer = (label: string) =>
      confirmModal.locator(`div.space-y-2:has(> label:text-is("${label}"))`).first()
    const accountTrigger = confirmFieldContainer('Conta de destino').locator('button, [role="combobox"]').first()
    await accountTrigger.click().catch(() => {})
    const firstOpt = page.locator('[role="option"]').first()
    if (await firstOpt.count()) await firstOpt.click()

    // submeter
    await confirmModal.getByRole('button', { name: /Confirmar|^Salvar$|^Marcar/i }).first().click()
    await expect(confirmModal).toBeHidden({ timeout: 15_000 })

    // 5) entry agora aparece com status "Recebido" (a receber baixado) ou "Pago"
    const updatedRow = page.locator('[data-row="true"]', { hasText: description }).first()
    await expect(updatedRow).toBeVisible({ timeout: 15_000 })
    await expect(updatedRow.getByText(/Recebido|Pago/i)).toBeVisible({ timeout: 10_000 })

    expectNoApiFailures()
  })
})
