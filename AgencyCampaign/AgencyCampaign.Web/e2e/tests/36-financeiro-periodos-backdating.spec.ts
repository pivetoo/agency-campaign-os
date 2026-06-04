import { test, expect } from '@playwright/test'
import { crud } from '../fixtures/helpers'

// Spec 36: Fechamento/reabertura de periodo + trava de back-dating.
// Fecha um mes NAO-corrente (3 meses atras, sem atividade dos demais specs, que
// usam hoje/futuro), valida que lancar com competencia nesse mes e BLOQUEADO
// (financialPeriod.closed -> 4xx), e SEMPRE reabre no final (finally) para nao
// deixar a base de homologacao travada.
// NB: usa o test base (nao a fixture expectNoApiFailures) porque o bloqueio
// retorna 4xx de proposito.

test.describe('Financeiro - fechamento de periodo + trava back-dating', () => {
  test('fecha mes passado, bloqueia lancamento retroativo e reabre', async ({ page }) => {
    const stamp = Date.now()
    const now = new Date()
    const target = new Date(now.getFullYear(), now.getMonth() - 3, 1)
    const ty = target.getFullYear()
    const tm = target.getMonth() // 0-based
    const monthLabel = target.toLocaleDateString('pt-BR', { month: 'long', year: 'numeric' })
    const labelRe = new RegExp(monthLabel.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'), 'i')
    const periodRow = () => page.locator('div.flex.items-center.justify-between.py-3').filter({ hasText: labelRe }).first()

    const reopenIfClosed = async () => {
      try {
        await page.goto('/financeiro/periodos')
        await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})
        const row = periodRow()
        await row.waitFor({ state: 'visible', timeout: 15_000 })
        const reopenBtn = row.getByRole('button', { name: /Reabrir/i })
        if (await reopenBtn.count()) {
          await reopenBtn.first().click()
          await page.getByTestId('dialog-confirm-button').click().catch(() => {})
          await page.getByTestId('dialog-confirm').waitFor({ state: 'hidden', timeout: 15_000 }).catch(() => {})
        }
      } catch {
        // best-effort: nao deixar o finally derrubar o teste
      }
    }

    try {
      await page.goto('/financeiro/periodos')
      await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})
      await expect(page.getByTestId('page-title').first()).toContainText(/Per[ií]odos/i, { timeout: 20_000 })
      await expect(periodRow()).toBeVisible({ timeout: 15_000 })

      // normaliza para ABERTO (caso uma execucao anterior tenha deixado fechado)
      if (await periodRow().getByRole('button', { name: /Reabrir/i }).count()) {
        await periodRow().getByRole('button', { name: /Reabrir/i }).first().click()
        await page.getByTestId('dialog-confirm-button').click()
        await expect(page.getByTestId('dialog-confirm')).toBeHidden({ timeout: 15_000 })
        await page.waitForTimeout(800)
      }
      await expect(periodRow().getByText(/Aberto/i)).toBeVisible({ timeout: 10_000 })

      // 1) FECHAR o mes alvo
      await periodRow().getByRole('button', { name: /Fechar/i }).click()
      await expect(page.getByTestId('dialog-confirm')).toBeVisible({ timeout: 10_000 })
      await page.getByTestId('dialog-confirm-button').click()
      await expect(page.getByTestId('dialog-confirm')).toBeHidden({ timeout: 15_000 })
      await page.waitForTimeout(800)

      // 2) validar fechado
      await expect(periodRow().getByText(/Fechado/i)).toBeVisible({ timeout: 10_000 })
      await expect(periodRow().getByRole('button', { name: /Reabrir/i })).toBeVisible({ timeout: 10_000 })

      // 3) tentar lancar com competencia no mes fechado -> deve ser BLOQUEADO
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
      await fc('Descrição').locator('input').first().fill(`E2E BackDating ${stamp}`)
      await fc('Valor (R$)').locator('input').first().fill('1234')
      const closedDate = `${ty}-${String(tm + 1).padStart(2, '0')}-15`
      await fc('Ocorrência').locator('input').first().fill(closedDate)
      await fc('Vencimento').locator('input').first().fill(closedDate)

      const createResp = page.waitForResponse(
        (r) => /\/api\/FinancialEntries\/Create/i.test(r.url()) && r.request().method() === 'POST',
        { timeout: 15_000 },
      )
      await modal.getByRole('button', { name: /^Salvar$/ }).first().click()
      const resp = await createResp
      expect(resp.status(), 'criar lancamento em mes fechado deve ser bloqueado (>=400)').toBeGreaterThanOrEqual(400)

      // modal continua aberto (save rejeitado, nada persistido)
      await expect(modal).toBeVisible({ timeout: 5_000 })
      await modal.getByRole('button', { name: /^Cancelar$/ }).first().click().catch(() => {})
    } finally {
      // 4) SEMPRE reabrir o mes alvo
      await reopenIfClosed()
      await expect(periodRow().getByText(/Aberto/i)).toBeVisible({ timeout: 10_000 }).catch(() => {})
    }
  })
})
