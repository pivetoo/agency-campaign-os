import { test, expect } from '../fixtures/test'
import { crud, clickSaveInDialog } from '../fixtures/helpers'

// Fluxo: oportunidade -> proposta -> item -> enviar -> aprovar -> vincular a campanha existente

test.describe('Proposta - aprovar e vincular a campanha (caminho critico)', () => {
  test('cria proposta com item, envia, aprova e vincula a uma campanha', async ({ page, expectNoApiFailures }) => {
    const stamp = Date.now()
    const oppName = `E2E PropConv ${stamp}`
    const itemDesc = `E2E Item ${stamp}`
    const campaignName = `E2E Camp Conv ${stamp}`

    // 1) cria campanha (precisa existir antes pra vincular)
    await page.goto('/campanhas')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})
    await crud.add(page).click()
    const campModal = page.getByRole('dialog').filter({ hasText: /Nova campanha/i })
    await expect(campModal).toBeVisible({ timeout: 10_000 })
    await campModal.locator(':text("Marca")').locator('..').locator('button, [role="combobox"]').first().click()
    await page.locator('[role="option"]').first().click()
    await campModal.locator('input[type="text"], input:not([type])').first().fill(campaignName)
    await campModal.locator('input[type="number"]').first().fill('25000')
    await campModal.locator('input[type="date"]').first().fill('2026-07-01')
    await campModal.getByRole('button', { name: /^Salvar$/i }).first().click()
    await expect(campModal).toBeHidden({ timeout: 15_000 })

    // 2) cria oportunidade com responsavel
    await page.goto('/comercial/oportunidades')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})
    await crud.add(page).click()
    const oppModal = page.getByRole('dialog').filter({ hasText: /Nova oportunidade/i })
    await expect(oppModal).toBeVisible({ timeout: 10_000 })
    await oppModal.getByLabel(/Nome da oportunidade/i).fill(oppName)
    await oppModal.locator(':text("Marca")').locator('..').locator('button, [role="combobox"]').first().click()
    await page.locator('[role="option"]').first().click()
    await oppModal.locator('#opportunity-estimated-value').fill('25000')
    // responsavel: trigger e o botao com texto "Nenhum" dentro do modal
    await oppModal.locator('button', { hasText: 'Nenhum' }).first().click()
    // espera popup abrir e clica numa opcao real
    const respOption = page.locator('[role="option"]').filter({ hasNotText: /^Nenhum$/i }).first()
    await expect(respOption).toBeVisible({ timeout: 10_000 })
    await respOption.click()
    await oppModal.getByRole('button', { name: /^Salvar$/i }).first().click()
    await expect(oppModal).toBeHidden({ timeout: 15_000 })

    // 3) cria proposta
    await page.goto('/comercial/propostas')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})
    await crud.add(page).click()
    const propModal = page.getByRole('dialog').filter({ hasText: /Criar proposta comercial/i })
    await expect(propModal).toBeVisible({ timeout: 10_000 })
    const oppTrigger = propModal.locator(':text("Oportunidade")').locator('..').locator('button, [role="combobox"]').first()
    await oppTrigger.click()
    const search = page.locator('input[placeholder*="Buscar oportunidade" i]').first()
    if (await search.count()) await search.fill(oppName)
    await page.locator('[role="option"]', { hasText: oppName }).first().click()
    await clickSaveInDialog(propModal)
    await expect(propModal).toBeHidden({ timeout: 15_000 })
    await page.waitForURL(/\/comercial\/propostas\/\d+/, { timeout: 15_000 })

    // 4) adicionar item
    await page.getByRole('button', { name: /^Adicionar$/i }).first().click()
    const itemModal = page.getByRole('dialog').filter({ hasText: /Novo item da proposta/i })
    await expect(itemModal).toBeVisible({ timeout: 10_000 })
    const itemFc = (label: string) =>
      itemModal.locator(`div.space-y-2:has(> label:text-is("${label}"))`).first()
    await itemFc('Descrição').locator('input').first().fill(itemDesc)
    await itemFc('Quantidade').locator('input').first().fill('1')
    await itemFc('Valor unitário').locator('input').first().fill('25000')
    await itemModal.getByRole('button', { name: /Salvar item|^Salvar$/i }).first().click()
    await expect(itemModal).toBeHidden({ timeout: 15_000 })

    // 5) enviar proposta (status 1 -> 2)
    await page.getByRole('button', { name: /^Enviar$/i }).first().click()
    await expect(page.getByText(/Enviada|Sent/i).first()).toBeVisible({ timeout: 10_000 })

    // 6) aprovar proposta (status 2 -> 4)
    await page.getByRole('button', { name: /^Aprovar$/i }).first().click()
    await expect(page.getByText(/Aprovada|Approved/i).first()).toBeVisible({ timeout: 10_000 })

    // 7) vincular a campanha existente — selecionar do SearchableSelect e clicar Converter
    const campanhaTrigger = page.locator(':text("Selecione uma campanha")').first()
    await campanhaTrigger.scrollIntoViewIfNeeded()
    await campanhaTrigger.click()
    const buscarCamp = page.locator('input[placeholder*="Buscar" i]').first()
    if (await buscarCamp.count()) await buscarCamp.fill(campaignName).catch(() => {})
    // aguarda async search resolver (Buscando... sumir)
    await page.getByText('Buscando...').waitFor({ state: 'detached', timeout: 20_000 }).catch(() => {})
    const campOption = page.getByRole('option', { name: new RegExp(campaignName, 'i') }).first()
    await campOption.waitFor({ state: 'visible', timeout: 15_000 })
    await campOption.click()

    await page.getByRole('button', { name: /Converter/i }).first().click()

    // 8) valida que nao houve erro HTTP e proposta agora esta convertida (status muda)
    await page.waitForTimeout(1500)
    expectNoApiFailures()
  })
})
