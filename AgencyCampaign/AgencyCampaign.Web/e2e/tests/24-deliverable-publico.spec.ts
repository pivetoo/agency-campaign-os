import { test, expect } from '../fixtures/test'

// Onda G: fluxo completo de aprovacao publica de deliverable
// 1) cria campanha + creator + deliverable
// 2) vai pra /operacao/aprovacoes
// 3) clica "Gerar link" — captura URL do alert
// 4) abre /d/:token em contexto NOVO sem auth
// 5) aprova como marca

test.describe('Deliverable publico - aprovacao pela marca', () => {
  test('cria deliverable, gera share link e aprova publicamente', async ({ page, browser }) => {
    const stamp = Date.now()
    const campaignName = `E2E CampG ${stamp}`
    const deliverableTitle = `E2E EntregaG ${stamp}`

    // 1) cria campanha
    await page.goto('/campanhas')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})
    await page.getByRole('button', { name: /^Incluir$|^Nova$/i }).first().click()
    const campModal = page.getByRole('dialog').filter({ hasText: /Nova campanha/i })
    await expect(campModal).toBeVisible({ timeout: 10_000 })
    await campModal.locator(':text("Marca")').locator('..').locator('button, [role="combobox"]').first().click()
    await page.locator('[role="option"]').first().click()
    await campModal.locator('input[type="text"], input:not([type])').first().fill(campaignName)
    await campModal.locator('input[type="number"]').first().fill('15000')
    await campModal.locator('input[type="date"]').first().fill('2026-09-01')
    await campModal.getByRole('button', { name: /^Salvar$/i }).first().click()
    await expect(campModal).toBeHidden({ timeout: 15_000 })

    // 2) abre detalhe da campanha
    const pageSizeSelect = page.locator('select').filter({ hasText: /5|10|20|50/ }).first()
    if (await pageSizeSelect.count()) await pageSizeSelect.selectOption('50').catch(() => {})

    const row = page.locator('[data-row="true"]', { hasText: campaignName }).first()
    await expect(row).toBeVisible({ timeout: 15_000 })
    const openBtn = row.locator('button').filter({ hasNotText: /.+/ }).first()
    await openBtn.click()
    await page.waitForURL(/\/campanhas\/\d+/, { timeout: 10_000 })

    // 3) adicionar creator
    await page.getByRole('button', { name: /Adicionar creator/i }).first().click()
    const creatorModal = page.getByRole('dialog').filter({ hasText: /Adicionar creator/i })
    await expect(creatorModal).toBeVisible({ timeout: 10_000 })
    const fcCreator = (label: string) =>
      creatorModal.locator(`div.space-y-2:has(> label:text-is("${label}"))`).first()
    await fcCreator('Creator').locator('button, [role="combobox"]').first().click()
    await page.locator('[role="option"]').first().click()
    await fcCreator('Valor combinado').locator('input').first().fill('5000')
    await creatorModal.getByRole('button', { name: /^Salvar$|Adicionar/i }).first().click()
    await expect(creatorModal).toBeHidden({ timeout: 15_000 })

    // 4) tab Entregas + criar deliverable
    await page.getByRole('tab', { name: /Entregas/i }).click().catch(async () => {
      await page.getByText('Entregas', { exact: false }).first().click()
    })

    await page.getByRole('button', { name: /Nova entrega/i }).first().click()
    const delivModal = page.getByRole('dialog').filter({ hasText: /Nova entrega/i })
    await expect(delivModal).toBeVisible({ timeout: 10_000 })
    const fc = (label: string) =>
      delivModal.locator(`div.space-y-2:has(> label:text-is("${label}"))`).first()
    await fc('Creator da campanha').locator('button, [role="combobox"]').first().click()
    await page.locator('[role="option"]').first().click()
    await fc('Título').locator('input').first().fill(deliverableTitle)
    await fc('Tipo').locator('button, [role="combobox"]').first().click()
    await page.locator('[role="option"]').first().click()
    await fc('Plataforma').locator('button, [role="combobox"]').first().click()
    await page.locator('[role="option"]').first().click()
    await fc('Prazo').locator('input').first().fill('2026-09-15')
    await fc('Valor bruto').locator('input').first().fill('5000')
    await delivModal.getByRole('button', { name: /^Salvar$/i }).first().click()
    await expect(delivModal).toBeHidden({ timeout: 15_000 })

    // 5) ir pra fila de aprovacoes operacionais
    await page.goto('/operacao/aprovacoes')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})

    // page size grande pra ver o item recem criado
    const opsPageSize = page.locator('select').filter({ hasText: /5|10|20|50/ }).first()
    if (await opsPageSize.count()) await opsPageSize.selectOption('50').catch(() => {})

    // 6) localizar a entrega na fila
    const delivRow = page.locator('[data-row="true"]', { hasText: deliverableTitle }).first()
    await expect(delivRow).toBeVisible({ timeout: 15_000 })
    await delivRow.scrollIntoViewIfNeeded()
    const genBtn = delivRow.getByRole('button', { name: /^(Gerar link|Novo link)$/i }).first()
    await expect(genBtn).toBeVisible({ timeout: 10_000 })

    // 7) intercepta dialog (silenciar) e response da API pra capturar o token
    page.on('dialog', (d) => { void d.accept() })
    const responsePromise = page.waitForResponse(
      (resp) => /\/api\/DeliverableShareLinks\/Create/i.test(resp.url()) && resp.status() === 201,
      { timeout: 15_000 },
    )
    await genBtn.click()
    const response = await responsePromise
    const body = await response.json()
    const token = body?.data?.token
    expect(token, `token nao encontrado na response: ${JSON.stringify(body)}`).toBeTruthy()
    const shareUrl = `${page.url().split('/operacao')[0]}/d/${token}`

    // 8) abrir contexto sem auth e acessar URL publica
    const publicContext = await browser.newContext()
    const publicPage = await publicContext.newPage()
    await publicPage.goto(shareUrl)
    await publicPage.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})

    // 9) tela publica deve mostrar o deliverable
    await expect(publicPage.getByText(deliverableTitle).first()).toBeVisible({ timeout: 15_000 })

    // 10) preencher nome do reviewer (obrigatorio)
    await publicPage.locator('input').filter({ hasNot: publicPage.locator('[type="hidden"]') }).first().fill('E2E Marca QA')

    // 11) clica "Aprovar"
    await publicPage.getByRole('button', { name: /^Aprovar$/i }).click()

    // 12) feedback de sucesso aparece (mensagem ou Decisão registrada)
    await expect(publicPage.getByText(/Entrega aprovada|Decisão registrada/i)).toBeVisible({ timeout: 15_000 })

    await publicContext.close()
  })
})
