import { test, expect } from '@playwright/test'
import { env } from '../fixtures/env'
import { crud, clickSaveInDialog, proposal, publicProposal, sendProposalViaModal } from '../fixtures/helpers'

// O botao "gerar link" so aparece quando a proposta ainda nao tem links; por isso
// criamos uma proposta nova (em vez de usar a primeira existente, que pode ja ter
// links) para o fluxo ser deterministico.
test.describe('Proposta - share link publico', () => {
  test('cria proposta, gera share link e abre em contexto sem auth', async ({ page, browser }) => {
    const stamp = Date.now()
    const oppName = `E2E ShareOpp ${stamp}`

    // 1) oportunidade com responsavel
    await page.goto('/comercial/oportunidades')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})
    await crud.add(page).click()
    const oppModal = page.getByRole('dialog').filter({ hasText: /Nova oportunidade/i })
    await expect(oppModal).toBeVisible({ timeout: 10_000 })
    await oppModal.getByLabel(/Nome da oportunidade/i).fill(oppName)
    await oppModal.locator(':text("Marca")').locator('..').locator('button, [role="combobox"]').first().click()
    await page.locator('[role="option"]').first().click()
    await oppModal.locator('#opportunity-estimated-value').fill('12000')
    const respLabel = oppModal.locator('label', { hasText: /^Responsável comercial$/ }).first()
    await expect(respLabel).toBeVisible({ timeout: 5_000 })
    await respLabel.locator('xpath=following::*[self::button or @role="combobox"][1]').first().click()
    await page.locator('[role="option"]').filter({ hasNotText: /^Nenhum$/i }).first().click()
    await oppModal.getByRole('button', { name: /^Salvar$/i }).first().click()
    await expect(oppModal).toBeHidden({ timeout: 15_000 })

    // 2) proposta vinculada
    await page.goto('/comercial/propostas')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})
    await crud.add(page).click()
    const propModal = page.getByRole('dialog').filter({ hasText: /Criar proposta comercial/i })
    await expect(propModal).toBeVisible({ timeout: 10_000 })
    await propModal.locator(':text("Oportunidade")').locator('..').locator('button, [role="combobox"]').first().click()
    const search = page.locator('input[placeholder*="Buscar oportunidade" i]').first()
    if (await search.count()) await search.fill(oppName)
    await page.locator('[role="option"]', { hasText: oppName }).first().click()
    await clickSaveInDialog(propModal)
    await expect(propModal).toBeHidden({ timeout: 15_000 })
    await page.waitForURL(/\/comercial\/propostas\/\d+/, { timeout: 15_000 })

    // 3) envia (habilita a geracao de links) e gera o share link
    await sendProposalViaModal(page)
    const generateBtn = proposal.generateLinkButton(page)
    await expect(generateBtn).toBeVisible({ timeout: 15_000 })
    const sharePromise = page.waitForResponse(
      (response) => /\/share-links\/Create/i.test(response.url()) && response.request().method() === 'POST',
      { timeout: 15_000 },
    )
    await generateBtn.click()
    const shareResponse = await sharePromise
    expect(shareResponse.ok(), `share-links/Create retornou ${shareResponse.status()}`).toBeTruthy()
    const body = await shareResponse.json()
    const token: string | undefined = body?.data?.token ?? body?.token
    expect(token, 'esperava token na resposta de share-links/Create').toBeTruthy()

    // 4) abrir /p/:token sem auth
    const anonymousContext = await browser.newContext()
    const anonymousPage = await anonymousContext.newPage()
    await anonymousPage.goto(`${env.baseURL}/p/${token}`)
    await expect(publicProposal.page(anonymousPage)).toBeVisible({ timeout: 20_000 })
    expect(anonymousPage.url()).toContain('/p/')
    await anonymousContext.close()
  })
})
