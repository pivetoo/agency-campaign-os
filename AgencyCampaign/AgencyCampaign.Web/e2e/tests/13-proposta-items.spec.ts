import { test, expect } from '../fixtures/test'

test.describe('Proposta - itens (caminho critico de receita)', () => {
  test('cria proposta + adiciona item + valida valor total', async ({ page, expectNoApiFailures }) => {
    const stamp = Date.now()
    const oppName = `E2E PropItem ${stamp}`
    const itemDesc = `E2E Item ${stamp}`

    // 1) cria oportunidade com responsavel
    await page.goto('/comercial/oportunidades')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})
    await page.getByRole('button', { name: /^Incluir$|Novo Lead/i }).first().click()
    const oppModal = page.getByRole('dialog').filter({ hasText: /Nova oportunidade/i })
    await expect(oppModal).toBeVisible({ timeout: 10_000 })
    await oppModal.getByLabel(/Nome da oportunidade/i).fill(oppName)
    await oppModal.locator(':text("Marca")').locator('..').locator('button, [role="combobox"]').first().click()
    await page.locator('[role="option"]').first().click()
    await oppModal.locator('#opportunity-estimated-value').fill('20000')
    const respLabel = oppModal.locator('label', { hasText: /^Responsável comercial$/ }).first()
    const respTrigger = respLabel.locator('xpath=following::*[self::button or @role="combobox"][1]').first()
    await respTrigger.click()
    await page.locator('[role="option"]').filter({ hasNotText: /^Nenhum$/i }).first().click()
    await oppModal.getByRole('button', { name: /^Salvar$/i }).first().click()
    await expect(oppModal).toBeHidden({ timeout: 15_000 })

    // 2) cria proposta vinculada
    await page.goto('/comercial/propostas')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})
    await page.getByRole('button', { name: /^Incluir$/i }).first().click()
    const proposalModal = page.getByRole('dialog').filter({ hasText: /Criar proposta comercial/i })
    await expect(proposalModal).toBeVisible({ timeout: 10_000 })
    const oppTrigger = proposalModal.locator(':text("Oportunidade")').locator('..').locator('button, [role="combobox"]').first()
    await oppTrigger.click()
    const search = page.locator('input[placeholder*="Buscar oportunidade" i]').first()
    if (await search.count()) await search.fill(oppName)
    await page.locator('[role="option"]', { hasText: oppName }).first().click()
    await proposalModal.getByRole('button', { name: /Criar e continuar|^Salvar$/i }).first().click()
    await expect(proposalModal).toBeHidden({ timeout: 15_000 })

    // navegou para detalhe da proposta
    await page.waitForURL(/\/comercial\/propostas\/\d+/, { timeout: 15_000 })

    // 3) adicionar item
    await page.getByRole('button', { name: /^Adicionar$/i }).first().click()
    const itemModal = page.getByRole('dialog').filter({ hasText: /Novo item da proposta/i })
    await expect(itemModal).toBeVisible({ timeout: 10_000 })

    const fieldContainer = (label: string) =>
      itemModal.locator(`div.space-y-2:has(> label:text-is("${label}"))`).first()

    await fieldContainer('Descrição').locator('input').first().fill(itemDesc)
    await fieldContainer('Quantidade').locator('input').first().fill('2')
    await fieldContainer('Valor unitário').locator('input').first().fill('5000')

    await itemModal.getByRole('button', { name: /Salvar item|^Salvar$/i }).first().click()
    await expect(itemModal).toBeHidden({ timeout: 15_000 })

    // 4) item aparece na listagem
    await expect(page.getByText(itemDesc).first()).toBeVisible({ timeout: 15_000 })

    // 5) valor total reflete (2 * 5000 = 10000)
    await expect(page.getByText(/10\.000/).first()).toBeVisible({ timeout: 10_000 })

    expectNoApiFailures()
  })
})
