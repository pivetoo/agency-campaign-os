import { test, expect } from '../fixtures/test'
import { crud } from '../fixtures/helpers'

test.describe('Financeiro - criar lancamento', () => {
  test('cria entry a receber e valida na listagem', async ({ page, expectNoApiFailures }) => {
    const stamp = Date.now()
    const description = `E2E Recebimento ${stamp}`

    await page.goto('/financeiro/receber')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})

    // botao incluir
    await crud.add(page).click()

    const modal = page.getByRole('dialog').filter({ hasText: /Novo lançamento/i })
    await expect(modal).toBeVisible({ timeout: 10_000 })

    // helper que encontra o trigger/input dentro do MESMO div pai do label
    const fieldContainer = (labelText: string) =>
      modal.locator(`div.space-y-2:has(> label:text-is("${labelText}"))`).first()

    // selecionar Conta — modal grande pode ter campo fora de viewport
    const contaTrigger = fieldContainer('Conta').locator('button, [role="combobox"]').first()
    await contaTrigger.scrollIntoViewIfNeeded()
    await contaTrigger.dispatchEvent('click')
    await page.locator('[role="option"]').first().click()

    // Descricao
    await fieldContainer('Descrição').locator('input').first().fill(description)

    // Valor
    await fieldContainer('Valor (R$)').locator('input').first().fill('5000')

    // Datas (Ocorrencia + Vencimento) — vencimento futuro pra cair no filtro padrao
    const today = new Date().toISOString().slice(0, 10)
    const future = new Date()
    future.setDate(future.getDate() + 14)
    const futureStr = future.toISOString().slice(0, 10)
    await fieldContainer('Ocorrência').locator('input').first().fill(today)
    await fieldContainer('Vencimento').locator('input').first().fill(futureStr)

    // Salvar
    await modal.getByRole('button', { name: /^Salvar$/i }).first().click()

    // modal fecha
    await expect(modal).toBeHidden({ timeout: 15_000 })

    // a tela tem filtro de periodo default que pode ocultar a entry — aplicar range amplo
    const dateInputs = page.locator('input[type="date"]')
    const dateCount = await dateInputs.count()
    if (dateCount >= 2) {
      await dateInputs.nth(0).fill('1900-01-01')
      await dateInputs.nth(1).fill('2100-12-31')
    }

    // entry aparece na listagem
    await expect(page.getByText(description).first()).toBeVisible({ timeout: 15_000 })

    expectNoApiFailures()
  })
})
