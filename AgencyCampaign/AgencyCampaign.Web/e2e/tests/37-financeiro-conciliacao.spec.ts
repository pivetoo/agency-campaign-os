import { test, expect } from '../fixtures/test'
import { crud, rowWithText } from '../fixtures/helpers'

// Spec 37: Conciliacao bancaria - importar extrato (auto-match 1:1), validar a
// baixa do recebivel e desfazer (unmatch) reabrindo o lancamento (unico
// Pago->Pendente permitido).
// Acopla a mesma conta: le a conta auto-selecionada na conciliacao e cria o
// recebivel nessa conta, com mesmo valor/data, para o auto-match casar.

test.describe('Financeiro - conciliacao bancaria', () => {
  test('importa extrato, auto-concilia o recebivel e desfaz', async ({ page, expectNoApiFailures }) => {
    const stamp = Date.now()
    const desc = `E2E Concil ${stamp}`
    const amount = 7000 + (stamp % 2000)
    const today = new Date().toISOString().slice(0, 10)

    // 1) descobrir a conta auto-selecionada na conciliacao
    await page.goto('/financeiro/conciliacao')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})
    await expect(page.getByTestId('page-title').first()).toContainText(/Concilia/i, { timeout: 20_000 })
    const accountTrigger = page.getByRole('combobox').first()
    await expect(accountTrigger).toBeVisible({ timeout: 15_000 })
    const accountName = (await accountTrigger.innerText()).trim()
    expect(accountName.length, 'conciliacao precisa de uma conta ativa').toBeGreaterThan(0)

    // 2) criar recebivel na MESMA conta, valor unico, vencendo hoje
    await page.goto('/financeiro/receber')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})
    await crud.add(page).click()
    const modal = page.getByRole('dialog').filter({ hasText: /Novo lançamento/i })
    await expect(modal).toBeVisible({ timeout: 10_000 })
    const fc = (label: string) => modal.locator(`div.space-y-2:has(> label:text-is("${label}"))`).first()
    const contaTrigger = fc('Conta').locator('button, [role="combobox"]').first()
    await contaTrigger.scrollIntoViewIfNeeded()
    await contaTrigger.dispatchEvent('click')
    const accountOption = page.getByRole('option').filter({ hasText: accountName }).first()
    if (await accountOption.count()) {
      await accountOption.click()
    } else {
      await page.getByRole('option').first().click()
    }
    await fc('Descrição').locator('input').first().fill(desc)
    await fc('Valor (R$)').locator('input').first().fill(String(amount))
    await fc('Ocorrência').locator('input').first().fill(today)
    await fc('Vencimento').locator('input').first().fill(today)
    await modal.getByRole('button', { name: /^Salvar$/ }).first().click()
    await expect(modal).toBeHidden({ timeout: 15_000 })

    // 3) voltar a conciliacao e importar o extrato (credito, mesmo valor/hoje)
    await page.goto('/financeiro/conciliacao')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})
    await page.getByRole('button', { name: /Importar extrato/i }).click()
    const importModal = page.getByRole('dialog').filter({ hasText: /extrato|importar/i })
    await expect(importModal).toBeVisible({ timeout: 10_000 })
    const csvLine = `${today};${desc};${amount},00;C`
    await importModal.locator('textarea').first().fill(csvLine)
    await importModal.getByRole('button', { name: /Importar|Confirmar|^Salvar$/i }).first().click()
    await expect(importModal).toBeHidden({ timeout: 15_000 })

    // 4) a transacao aparece e ja vem CONCILIADA (auto-match)
    const txRow = rowWithText(page, desc).first()
    await expect(txRow).toBeVisible({ timeout: 15_000 })
    await expect(txRow.getByText(/Conciliad/i)).toBeVisible({ timeout: 10_000 })

    // 5) o recebivel foi baixado (Recebido/Pago)
    await page.goto('/financeiro/receber')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})
    const di = page.locator('input[type="date"]')
    if (await di.count() >= 2) {
      await di.nth(0).fill('1900-01-01')
      await di.nth(1).fill('2100-12-31')
    }
    await expect(rowWithText(page, desc).first().getByText(/Recebido|Pago/i)).toBeVisible({ timeout: 15_000 })

    // 6) desfazer a conciliacao -> recebivel reabre
    await page.goto('/financeiro/conciliacao')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})
    const txRow2 = rowWithText(page, desc).first()
    await expect(txRow2).toBeVisible({ timeout: 15_000 })
    await txRow2.getByRole('button').first().click()
    await expect(page.getByTestId('dialog-confirm')).toBeVisible({ timeout: 10_000 })
    await page.getByTestId('dialog-confirm-button').click()
    await expect(page.getByTestId('dialog-confirm')).toBeHidden({ timeout: 15_000 })
    await expect(rowWithText(page, desc).first().getByText(/Pendente/i)).toBeVisible({ timeout: 10_000 })

    // 7) e o recebivel volta a aberto (Pendente/Vencido)
    await page.goto('/financeiro/receber')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})
    const di2 = page.locator('input[type="date"]')
    if (await di2.count() >= 2) {
      await di2.nth(0).fill('1900-01-01')
      await di2.nth(1).fill('2100-12-31')
    }
    await expect(rowWithText(page, desc).first().getByText(/Pendente|Vencido/i)).toBeVisible({ timeout: 15_000 })

    expectNoApiFailures()
  })
})
