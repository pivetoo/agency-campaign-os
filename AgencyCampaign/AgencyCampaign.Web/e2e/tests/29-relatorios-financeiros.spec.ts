import { test, expect } from '../fixtures/test'
import { expectPageTitle } from '../fixtures/helpers'

// Spec 29: Modulo Relatorios — landing, 6 rotas do bloco financeiro, redirect das
// rotas antigas e export CSV.
// Sem mutacoes de dados.

const FINANCIAL_ROUTES = [
  { path: '/relatorios/financeiro/fluxo-caixa', heading: /Fluxo de caixa/i },
  { path: '/relatorios/financeiro/aging', heading: /Aging financeiro/i },
  { path: '/relatorios/financeiro/projecao', heading: /Projeção de Fluxo/i },
  { path: '/relatorios/financeiro/resultado', heading: /Resultado/i },
  { path: '/relatorios/financeiro/rentabilidade', heading: /Rentabilidade/i },
  { path: '/relatorios/financeiro/retencoes', heading: /Retenções|Retencoes/i },
]

test.describe('Relatorios - landing', () => {
  test('landing /relatorios exibe secao Financeiro com cards', async ({ page, expectNoApiFailures }) => {
    await page.goto('/relatorios')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})

    await expectPageTitle(page, /Relatórios|Relatorios/i, 15_000)

    // secao "Financeiro" deve aparecer como heading de grupo
    await expect(page.getByText(/^Financeiro$/i).first()).toBeVisible({ timeout: 10_000 })

    // ao menos 3 cards de relatorio financeiro devem estar presentes
    for (const title of ['Fluxo de Caixa', 'Aging', 'Projeção de Fluxo']) {
      await expect(page.getByText(title, { exact: false }).first()).toBeVisible({ timeout: 10_000 })
    }

    expectNoApiFailures()
  })
})

test.describe('Relatorios - rotas financeiras', () => {
  for (const { path, heading } of FINANCIAL_ROUTES) {
    test(`${path} renderiza pagina`, async ({ page, expectNoApiFailures }) => {
      await page.goto(path)
      await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})

      await expectPageTitle(page, heading, 15_000)

      expectNoApiFailures()
    })
  }
})

test.describe('Relatorios - redirect de rotas legadas', () => {
  test('/financeiro/fluxo-caixa redireciona para /relatorios/financeiro/fluxo-caixa', async ({ page, expectNoApiFailures }) => {
    await page.goto('/financeiro/fluxo-caixa')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})

    // o Navigate replace do react-router deve ter levado para a nova rota
    await expect(page).toHaveURL(/relatorios\/financeiro\/fluxo-caixa/, { timeout: 15_000 })
    await expectPageTitle(page, /Fluxo de caixa/i, 15_000)

    expectNoApiFailures()
  })

  test('/financeiro/aging redireciona para /relatorios/financeiro/aging', async ({ page, expectNoApiFailures }) => {
    await page.goto('/financeiro/aging')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})

    await expect(page).toHaveURL(/relatorios\/financeiro\/aging/, { timeout: 15_000 })
    await expectPageTitle(page, /Aging financeiro/i, 15_000)

    expectNoApiFailures()
  })
})

test.describe('Relatorios - Fluxo de caixa detalhado', () => {
  test('carrega KPIs, troca granularidade e ajusta intervalo', async ({ page, expectNoApiFailures }) => {
    await page.goto('/relatorios/financeiro/fluxo-caixa')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})

    await expectPageTitle(page, /Fluxo de caixa/i, 15_000)

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
      page.locator('[data-tour="cashflow-chart"]').first(),
    ).toBeVisible()

    expectNoApiFailures()
  })
})

test.describe('Relatorios - Aging detalhado', () => {
  test('carrega totais e buckets', async ({ page, expectNoApiFailures }) => {
    await page.goto('/relatorios/financeiro/aging')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})

    await expectPageTitle(page, /Aging financeiro/i, 15_000)

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

test.describe('Relatorios - export CSV', () => {
  test('botao CSV na projecao de fluxo dispara download', async ({ page, expectNoApiFailures }) => {
    await page.goto('/relatorios/financeiro/projecao')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})

    await expectPageTitle(page, /Projeção de Fluxo/i, 15_000)

    // botao "CSV" deve estar visivel (ReportLayout renderiza Button variant=outline com texto CSV)
    const csvButton = page.getByRole('button', { name: /CSV/i }).first()
    await expect(csvButton).toBeVisible({ timeout: 10_000 })

    // intercepta o download: a chamada ao backend deve retornar blob/200 ou o botao
    // dispara a navegacao via href; em ambos os casos aguardamos o evento download OU
    // uma resposta bem-sucedida ao endpoint de export
    const downloadOrResponse = Promise.race([
      page.waitForEvent('download', { timeout: 15_000 }).then(() => 'download'),
      page.waitForResponse(
        (resp) => /\/FinancialReports\/cashflow-projection\/export/i.test(resp.url()) && resp.status() < 400,
        { timeout: 15_000 },
      ).then(() => 'response'),
    ])

    await csvButton.click()

    const result = await downloadOrResponse
    expect(['download', 'response']).toContain(result)

    expectNoApiFailures()
  })
})

// ---------------------------------------------------------------------------
// Bloco Comercial
// ---------------------------------------------------------------------------

const COMMERCIAL_ROUTES = [
  { path: '/relatorios/comercial/funil', heading: /Funil de Conversão/i },
  { path: '/relatorios/comercial/ganhos-perdas', heading: /Ganhos|Perdas/i },
  { path: '/relatorios/comercial/forecast', heading: /Forecast|Previsão/i },
  { path: '/relatorios/comercial/metas', heading: /Metas/i },
  { path: '/relatorios/comercial/propostas', heading: /Propostas/i },
  { path: '/relatorios/comercial/ranking', heading: /Ranking/i },
]

test.describe('Relatorios - landing secao Comercial', () => {
  test('landing /relatorios exibe secao Comercial com cards', async ({ page, expectNoApiFailures }) => {
    await page.goto('/relatorios')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})

    await expectPageTitle(page, /Relatórios|Relatorios/i, 15_000)

    // secao "Comercial" deve aparecer como heading de grupo
    await expect(page.getByText(/^Comercial$/i).first()).toBeVisible({ timeout: 10_000 })

    // ao menos 2 cards do bloco comercial devem estar presentes
    for (const title of ['Funil de Conversão', 'Ranking por Marca']) {
      await expect(page.getByText(title, { exact: false }).first()).toBeVisible({ timeout: 10_000 })
    }

    expectNoApiFailures()
  })
})

test.describe('Relatorios - rotas comerciais', () => {
  for (const { path, heading } of COMMERCIAL_ROUTES) {
    test(`${path} renderiza pagina`, async ({ page, expectNoApiFailures }) => {
      await page.goto(path)
      await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})

      await expectPageTitle(page, heading, 15_000)

      expectNoApiFailures()
    })
  }
})

test.describe('Relatorios - redirect de rota legada comercial', () => {
  test('/comercial/analytics redireciona para /relatorios/comercial/funil', async ({ page, expectNoApiFailures }) => {
    await page.goto('/comercial/analytics')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})

    await expect(page).toHaveURL(/relatorios\/comercial\/funil/, { timeout: 15_000 })
    await expectPageTitle(page, /Funil de Conversão/i, 15_000)

    expectNoApiFailures()
  })
})

test.describe('Relatorios - export CSV comercial', () => {
  test('botao CSV no ranking por marca dispara download', async ({ page, expectNoApiFailures }) => {
    await page.goto('/relatorios/comercial/ranking')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})

    await expectPageTitle(page, /Ranking/i, 15_000)

    const csvButton = page.getByRole('button', { name: /CSV/i }).first()
    await expect(csvButton).toBeVisible({ timeout: 10_000 })

    const downloadOrResponse = Promise.race([
      page.waitForEvent('download', { timeout: 15_000 }).then(() => 'download'),
      page.waitForResponse(
        (resp) => /\/CommercialReports\/brand-ranking\/export/i.test(resp.url()) && resp.status() < 400,
        { timeout: 15_000 },
      ).then(() => 'response'),
    ])

    await csvButton.click()

    const result = await downloadOrResponse
    expect(['download', 'response']).toContain(result)

    expectNoApiFailures()
  })
})

// ---------------------------------------------------------------------------
// Bloco Producao
// ---------------------------------------------------------------------------

const PRODUCTION_ROUTES = [
  { path: '/relatorios/producao/campanhas', heading: /Performance de Campanhas/i },
  { path: '/relatorios/producao/creators', heading: /Desempenho por Creator/i },
  { path: '/relatorios/producao/plataforma', heading: /Produção por Plataforma/i },
  { path: '/relatorios/producao/sla', heading: /Entregáveis|Prazo|Atraso/i },
  { path: '/relatorios/producao/aprovacoes', heading: /Aprovação e Rodadas/i },
  { path: '/relatorios/producao/licencas', heading: /Licenças de Conteúdo/i },
]

test.describe('Relatorios - landing secao Producao', () => {
  test('landing /relatorios exibe secao Producao com cards', async ({ page, expectNoApiFailures }) => {
    await page.goto('/relatorios')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})

    await expectPageTitle(page, /Relatórios|Relatorios/i, 15_000)

    // secao "Producao" deve aparecer como heading de grupo
    await expect(page.getByText(/^Produção$/i).first()).toBeVisible({ timeout: 10_000 })

    // ao menos 2 cards do bloco de producao devem estar presentes
    for (const title of ['Performance de Campanhas', 'Licenças de Conteúdo']) {
      await expect(page.getByText(title, { exact: false }).first()).toBeVisible({ timeout: 10_000 })
    }

    expectNoApiFailures()
  })
})

test.describe('Relatorios - rotas de producao', () => {
  for (const { path, heading } of PRODUCTION_ROUTES) {
    test(`${path} renderiza pagina`, async ({ page, expectNoApiFailures }) => {
      await page.goto(path)
      await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})

      await expectPageTitle(page, heading, 15_000)

      expectNoApiFailures()
    })
  }
})

test.describe('Relatorios - export CSV producao', () => {
  test('botao CSV em Performance de Campanhas dispara download', async ({ page, expectNoApiFailures }) => {
    await page.goto('/relatorios/producao/campanhas')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})

    await expectPageTitle(page, /Performance de Campanhas/i, 15_000)

    const csvButton = page.getByRole('button', { name: /CSV/i }).first()
    await expect(csvButton).toBeVisible({ timeout: 10_000 })

    const downloadOrResponse = Promise.race([
      page.waitForEvent('download', { timeout: 15_000 }).then(() => 'download'),
      page.waitForResponse(
        (resp) => /\/ProductionReports\/campaign-performance\/export/i.test(resp.url()) && resp.status() < 400,
        { timeout: 15_000 },
      ).then(() => 'response'),
    ])

    await csvButton.click()

    const result = await downloadOrResponse
    expect(['download', 'response']).toContain(result)

    expectNoApiFailures()
  })
})
