import { test, expect } from '../fixtures/test'
import { expectPageTitle } from '../fixtures/helpers'

// Spec 28: Repasses para creators (/financeiro/repasses-creators)
// Cobre navegacao da pagina, troca de filtro de status, abertura do modal de
// novo pagamento e validacao das acoes em massa quando ha selecao.
// Nao cria pagamento real (depende de campanha + creator vinculado).

test.describe('Financeiro - Repasses para creators', () => {
  test('renderiza, troca filtro, abre modal Novo e valida acoes contextuais', async ({ page, expectNoApiFailures }) => {
    await page.goto('/financeiro/repasses-creators')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})

    // 1) header
    await expectPageTitle(page, /Repasses para (creators|influenciadores|influencers)/i, 15_000)

    // 2) 4 stats: Pagamentos, Total bruto, Total liquido, Selecionados
    for (const label of ['Pagamentos', 'Total bruto', 'Total líquido', 'Selecionados']) {
      await expect(page.getByText(label, { exact: false }).first()).toBeVisible()
    }

    // 3) filtros
    await expect(page.getByText('Filtrar por status')).toBeVisible()
    await expect(page.getByText('Filtrar por campanha')).toBeVisible()

    // 4) acoes contextuais visiveis e disabled sem selecao
    for (const label of ['Editar', 'Anexar NF', 'Marcar pago', 'Cancelar']) {
      const btn = page.getByRole('button', { name: new RegExp(`^${label}$`) }).first()
      await expect(btn).toBeVisible()
      await expect(btn).toBeDisabled()
    }
    await expect(page.getByRole('button', { name: /^Novo$/ })).toBeEnabled()

    // 5) trocar filtro de status para "Pago" (o seletor inicial e Pendente)
    const statusCombobox = page.getByRole('combobox').first()
    await statusCombobox.click()
    await expect.poll(async () => page.getByRole('option').count(), { timeout: 10_000 }).toBeGreaterThan(0)
    await page.getByRole('option', { name: /^Pago$/ }).first().click()

    // aguarda recarga
    await page.waitForLoadState('networkidle', { timeout: 10_000 }).catch(() => {})

    // 6) abre modal "Novo"
    await page.getByRole('button', { name: /^Novo$/ }).click()
    const modal = page.getByRole('dialog').filter({ hasText: /Novo pagamento ao (creator|influenciador)/i })
    await expect(modal).toBeVisible({ timeout: 10_000 })

    // labels obrigatorios
    for (const label of ['Creator vinculado à campanha', 'Valor bruto (R$)', 'Valor líquido', 'Método']) {
      await expect(modal.getByText(label, { exact: false }).first()).toBeVisible()
    }

    // Salvar deve estar disabled sem dados
    const salvarBtn = modal.getByRole('button', { name: /^Salvar$/ })
    if (await salvarBtn.count()) {
      await expect(salvarBtn.first()).toBeDisabled()
    }

    // fecha modal
    const cancelarBtn = modal.getByRole('button', { name: /^Cancelar$/ })
    if (await cancelarBtn.count()) {
      await cancelarBtn.first().click()
    } else {
      await page.keyboard.press('Escape')
    }
    await expect(modal).toBeHidden({ timeout: 10_000 })

    expectNoApiFailures()
  })
})
