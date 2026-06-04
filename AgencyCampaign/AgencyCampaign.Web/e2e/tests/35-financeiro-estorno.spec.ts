import { test, expect } from '../fixtures/test'
import { crud, rowWithText } from '../fixtures/helpers'

// Spec 35: Estorno de lancamento pago (correcao por contrapartida).
// Operador: cria recebivel -> marca como pago -> estorna. Valida o badge
// "estornado" no original, o sumico do botao Estornar e a contrapartida
// "Estorno do lancamento #..." em contas a pagar.
// Lancamentos financeiros nao tem DELETE (imutaveis quando pagos); ficam na base
// de homologacao com prefixo E2E, igual aos specs 10/14/33.

test.describe('Financeiro - estorno de lancamento', () => {
  test('cria recebivel, marca pago e estorna gerando contrapartida', async ({ page, expectNoApiFailures }) => {
    const stamp = Date.now()
    const description = `E2E Estorno ${stamp}`

    // 1) cria recebivel
    await page.goto('/financeiro/receber')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})
    await crud.add(page).click()
    const modal = page.getByRole('dialog').filter({ hasText: /Novo lançamento/i })
    await expect(modal).toBeVisible({ timeout: 10_000 })
    const fc = (label: string) => modal.locator(`div.space-y-2:has(> label:text-is("${label}"))`).first()
    const contaTrigger = fc('Conta').locator('button, [role="combobox"]').first()
    await contaTrigger.scrollIntoViewIfNeeded()
    await contaTrigger.dispatchEvent('click')
    await page.locator('[role="option"]').first().click()
    await fc('Descrição').locator('input').first().fill(description)
    await fc('Valor (R$)').locator('input').first().fill('6543')
    const today = new Date().toISOString().slice(0, 10)
    const future = new Date()
    future.setDate(future.getDate() + 10)
    const futureStr = future.toISOString().slice(0, 10)
    await fc('Ocorrência').locator('input').first().fill(today)
    await fc('Vencimento').locator('input').first().fill(futureStr)
    await modal.getByRole('button', { name: /^Salvar$/ }).first().click()
    await expect(modal).toBeHidden({ timeout: 15_000 })

    // 2) localizar e marcar como pago
    const dateInputs = page.locator('input[type="date"]')
    if (await dateInputs.count() >= 2) {
      await dateInputs.nth(0).fill('1900-01-01')
      await dateInputs.nth(1).fill('2100-12-31')
    }
    const row = rowWithText(page, description).first()
    await expect(row).toBeVisible({ timeout: 15_000 })
    await row.getByRole('button', { name: /Confirmar/i }).click()
    const paidModal = page.getByRole('dialog').filter({ hasText: /Confirmar recebimento|Confirmar pagamento/i })
    await expect(paidModal).toBeVisible({ timeout: 10_000 })
    const paidFc = (label: string) => paidModal.locator(`div.space-y-2:has(> label:text-is("${label}"))`).first()
    const destTrigger = paidFc('Conta de destino').locator('button, [role="combobox"]').first()
    await destTrigger.click().catch(() => {})
    const opt = page.locator('[role="option"]').first()
    if (await opt.count()) await opt.click()
    await paidModal.getByRole('button', { name: /Confirmar|^Salvar$|^Marcar/i }).first().click()
    await expect(paidModal).toBeHidden({ timeout: 15_000 })

    const paidRow = rowWithText(page, description).first()
    await expect(paidRow.getByText(/Recebido|Pago/i)).toBeVisible({ timeout: 10_000 })

    // 3) estornar (ConfirmModal -> dialog-confirm-button)
    await paidRow.getByRole('button', { name: /Estornar/i }).click()
    await expect(page.getByTestId('dialog-confirm')).toBeVisible({ timeout: 10_000 })
    await page.getByTestId('dialog-confirm-button').click()
    await expect(page.getByTestId('dialog-confirm')).toBeHidden({ timeout: 15_000 })

    // 4) original marcado como estornado e sem o botao Estornar
    const reversedRow = rowWithText(page, description).first()
    await expect(reversedRow).toBeVisible({ timeout: 15_000 })
    await expect(reversedRow.getByText(/estornad/i)).toBeVisible({ timeout: 10_000 })
    await expect(reversedRow.getByRole('button', { name: /Estornar/i })).toHaveCount(0)

    // 5) contrapartida em contas a pagar
    await page.goto('/financeiro/pagar')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})
    const payDateInputs = page.locator('input[type="date"]')
    if (await payDateInputs.count() >= 2) {
      await payDateInputs.nth(0).fill('1900-01-01')
      await payDateInputs.nth(1).fill('2100-12-31')
    }
    await expect(page.getByText(/Estorno do lan[çc]amento/i).first()).toBeVisible({ timeout: 15_000 })

    expectNoApiFailures()
  })
})
