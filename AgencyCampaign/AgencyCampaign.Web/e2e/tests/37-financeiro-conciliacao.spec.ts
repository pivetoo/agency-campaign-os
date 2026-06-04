import { test, expect } from '../fixtures/test'
import { crud, rowWithText } from '../fixtures/helpers'

// Spec 37: Conciliacao bancaria - importar extrato, conciliar o recebivel
// (auto-match 1:1 ou match manual) e desfazer (unmatch) reabrindo o lancamento
// (unico Pago->Pendente permitido).
// Acopla a MESMA conta de forma deterministica: le a conta auto-selecionada na
// conciliacao e cria o recebivel exatamente nessa conta (selecao exata), para o
// auto-match casar. Cai para match manual se o auto-match nao disparar.

test.describe('Financeiro - conciliacao bancaria', () => {
  test('importa extrato, concilia o recebivel e desfaz', async ({ page, expectNoApiFailures }) => {
    const stamp = Date.now()
    const desc = `E2E Concil ${stamp}`
    const amount = 90000 + (stamp % 9000)
    const today = new Date().toISOString().slice(0, 10)

    // 1) conta auto-selecionada na conciliacao
    await page.goto('/financeiro/conciliacao')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})
    await expect(page.getByTestId('page-title').first()).toContainText(/Concilia/i, { timeout: 20_000 })
    const accountCombo = page.getByRole('combobox').first()
    await expect(accountCombo).toBeVisible({ timeout: 15_000 })
    const accountName = (await accountCombo.innerText()).trim().split('\n')[0].trim()
    expect(accountName.length, 'conciliacao precisa de uma conta ativa').toBeGreaterThan(0)

    // 2) recebivel na MESMA conta (selecao exata), valor unico, vencendo hoje
    await page.goto('/financeiro/receber')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})
    await crud.add(page).click()
    const modal = page.getByRole('dialog').filter({ hasText: /Novo lançamento/i })
    await expect(modal).toBeVisible({ timeout: 10_000 })
    const fc = (label: string) => modal.locator(`div.space-y-2:has(> label:text-is("${label}"))`).first()
    const contaTrigger = fc('Conta').locator('button, [role="combobox"]').first()
    await contaTrigger.scrollIntoViewIfNeeded()
    await contaTrigger.dispatchEvent('click')
    const exactOption = page.getByRole('option', { name: accountName, exact: true }).first()
    if (await exactOption.count()) {
      await exactOption.click()
    } else {
      await page.getByRole('option').filter({ hasText: accountName }).first().click()
    }
    await expect(contaTrigger).toContainText(accountName, { timeout: 5_000 })
    await fc('Descrição').locator('input').first().fill(desc)
    await fc('Valor (R$)').locator('input').first().fill(String(amount))
    await fc('Ocorrência').locator('input').first().fill(today)
    await fc('Vencimento').locator('input').first().fill(today)
    await modal.getByRole('button', { name: /^Salvar$/ }).first().click()
    await expect(modal).toBeHidden({ timeout: 15_000 })

    // 3) conciliacao: importar extrato (credito, mesmo valor/hoje, mesma conta)
    await page.goto('/financeiro/conciliacao')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})
    await expect(page.getByRole('combobox').first()).toContainText(accountName, { timeout: 10_000 })
    await page.getByRole('button', { name: /Importar extrato/i }).click()
    const importModal = page.getByRole('dialog').filter({ hasText: /extrato|importar/i })
    await expect(importModal).toBeVisible({ timeout: 10_000 })
    await importModal.locator('textarea').first().fill(`${today};${desc};${amount},00;C`)
    const importResp = page.waitForResponse((r) => /\/api\/BankTransactions\/Import/i.test(r.url()) && r.request().method() === 'POST', { timeout: 15_000 })
    await importModal.getByRole('button', { name: /Importar|Confirmar|^Salvar$/i }).first().click()
    const resp = await importResp
    expect(resp.status(), 'import do extrato nao pode dar erro').toBeLessThan(400)
    await expect(importModal).toBeHidden({ timeout: 15_000 })

    // 4) transacao aparece; concilia (auto-match OU match manual)
    const txRow = rowWithText(page, desc).first()
    await expect(txRow).toBeVisible({ timeout: 15_000 })
    if ((await txRow.getByText(/Conciliad/i).count()) === 0) {
      // auto-match nao disparou: casa manualmente pelo modal
      await txRow.getByRole('button').first().click()
      const matchModal = page.getByRole('dialog').filter({ hasText: /[Cc]oncilia|[Cc]asar|[Vv]incular|lançamento/ }).first()
      await expect(matchModal).toBeVisible({ timeout: 10_000 })
      await matchModal.locator('button[role="combobox"], [role="combobox"]').first().click()
      await page.getByRole('option').filter({ hasText: desc }).first().click()
      await matchModal.getByRole('button', { name: /Conciliar|Casar|Vincular|Confirmar|^Salvar$/i }).first().click()
      await expect(matchModal).toBeHidden({ timeout: 15_000 })
    }
    await expect(rowWithText(page, desc).first().getByText(/Conciliad/i)).toBeVisible({ timeout: 10_000 })

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
