import { test, expect } from '../fixtures/test'

test.describe('Proposta - fluxo de criacao', () => {
  test('cria proposta nova vinculada a oportunidade existente', async ({ page, expectNoApiFailures }) => {
    const stamp = Date.now()

    // garantir que existe ao menos uma oportunidade — criar uma nova rapida
    const oppName = `E2E Prop Opp ${stamp}`
    await page.goto('/comercial/oportunidades')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})

    await page.getByRole('button', { name: /^Incluir$|Novo Lead/i }).first().click()
    const oppModal = page.getByRole('dialog').filter({ hasText: /Nova oportunidade/i })
    await expect(oppModal).toBeVisible({ timeout: 10_000 })
    await oppModal.getByLabel(/Nome da oportunidade/i).fill(oppName)
    const brandTrigger = oppModal.locator(':text("Marca")').locator('..').locator('button, [role="combobox"]').first()
    await brandTrigger.click()
    await page.locator('[role="option"]').first().click()
    const valueInput = oppModal.locator('#opportunity-estimated-value')
    await valueInput.fill('15000')
    await oppModal.getByRole('button', { name: /^Salvar$/i }).first().click()
    await expect(oppModal).toBeHidden({ timeout: 15_000 })

    // ir para Propostas e criar uma nova
    await page.goto('/comercial/propostas')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})

    await page.getByRole('button', { name: /^Incluir$/i }).first().click()
    const proposalModal = page.getByRole('dialog').filter({ hasText: /Criar proposta comercial/i })
    await expect(proposalModal).toBeVisible({ timeout: 10_000 })

    // selecionar a oportunidade recem criada via SearchableSelect
    const oppTrigger = proposalModal.locator(':text("Oportunidade")').locator('..').locator('button, [role="combobox"]').first()
    await oppTrigger.click()
    // buscar pela oportunidade que acabei de criar
    const search = page.locator('input[placeholder*="Buscar oportunidade" i]').first()
    if (await search.count()) {
      await search.fill(oppName)
    }
    const oppOption = page.locator('[role="option"]', { hasText: oppName }).first()
    await expect(oppOption).toBeVisible({ timeout: 10_000 })
    await oppOption.click()

    // submeter
    await proposalModal.getByRole('button', { name: /Criar e continuar|^Salvar$/i }).first().click()

    // modal fecha
    await expect(proposalModal).toBeHidden({ timeout: 15_000 })

    // url muda para /comercial/propostas/{id}
    await page.waitForURL(/\/comercial\/propostas\/\d+/, { timeout: 15_000 })

    // tela de detalhe carrega — deve aparecer o nome da oportunidade
    await expect(page.getByText(oppName).first()).toBeVisible({ timeout: 15_000 })

    expectNoApiFailures()
  })
})
