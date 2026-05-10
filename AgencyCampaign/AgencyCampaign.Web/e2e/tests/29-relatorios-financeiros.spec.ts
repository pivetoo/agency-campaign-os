import { test, expect } from '../fixtures/test'

// Spec 29: Relatorios financeiros (Fluxo de caixa + Aging)
// Cobre as duas paginas de relatorio: filtros, KPIs e mudanca de granularidade
// no fluxo de caixa. Sem mutacoes.

test.describe('Financeiro - Relatorios', () => {
  test('Fluxo de caixa carrega KPIs, troca granularidade e ajusta intervalo', async ({ page, expectNoApiFailures }) => {
    await page.goto('/financeiro/fluxo-caixa')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})

    await expect(page.getByRole('heading', { name: /Fluxo de caixa/i })).toBeVisible({ timeout: 15_000 })

    // 4 KPIs do fluxo
    for (const label of [
      'A receber (período)',
      'A pagar (período)',
      'Recebido (período)',
      'Pago (período)',
    ]) {
      await expect(page.getByText(label, { exact: false }).first()).toBeVisible()
    }

    // filtros: 2 datas + granularidade
    await expect(page.getByText(/^De$/)).toBeVisible()
    await expect(page.getByText(/^Até$/)).toBeVisible()
    await expect(page.getByText(/^Granularidade$/)).toBeVisible()

    // troca granularidade Mensal -> Semanal e aguarda recarregamento
    const granularityCombobox = page.getByRole('combobox').last()
    await granularityCombobox.click()
    await expect.poll(async () => page.getByRole('option').count(), { timeout: 10_000 }).toBeGreaterThan(0)

    const cashFlowReload = page.waitForResponse(
      (resp) => /\/api\/FinancialReports\/CashFlow/i.test(resp.url()) && resp.status() < 400,
      { timeout: 15_000 },
    )
    await page.getByRole('option', { name: /^Semanal$/ }).first().click()
    await cashFlowReload

    // grafico ou empty state visivel
    await expect(
      page
        .locator('[data-tour="cashflow-chart"]')
        .first(),
    ).toBeVisible()

    expectNoApiFailures()
  })

  test('Aging carrega totais e buckets', async ({ page, expectNoApiFailures }) => {
    await page.goto('/financeiro/aging')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})

    await expect(page.getByRole('heading', { name: /Aging financeiro/i })).toBeVisible({ timeout: 15_000 })

    // 2 cards de totais no topo
    await expect(page.getByText(/Total a receber pendente/i)).toBeVisible()
    await expect(page.getByText(/Total a pagar pendente/i)).toBeVisible()

    // ou ha buckets renderizados, ou empty/loading
    const buckets = page.locator('[data-tour="aging-buckets"]').first()
    const semDados = page.getByText(/^Sem dados\.$/)
    const carregando = page.getByText(/Carregando aging/i)
    await expect(buckets.or(semDados).or(carregando)).toBeVisible({ timeout: 15_000 })

    expectNoApiFailures()
  })
})
