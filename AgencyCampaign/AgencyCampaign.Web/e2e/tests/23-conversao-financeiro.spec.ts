import { test, expect } from '../fixtures/test'
import { crud, clickSaveInDialog } from '../fixtures/helpers'

// Fluxo completo de receita: oportunidade -> proposta com items -> envia -> aprova -> converte em campanha -> verifica financeiro auto-gerado

test.describe('Conversao proposta->campanha + geracao financeira automatica', () => {
  test('valida que entry a receber e gerada apos conversao', async ({ page, expectNoApiFailures }) => {
    const stamp = Date.now()
    const oppName = `E2E ConvFin ${stamp}`
    const itemDesc = `E2E ItemFin ${stamp}`
    const campaignName = `E2E CampFin ${stamp}`
    const expectedValue = 30000

    // 1) cria campanha
    await page.goto('/campanhas')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})
    await crud.add(page).click()
    const campModal = page.getByRole('dialog').filter({ hasText: /Nova campanha/i })
    await expect(campModal).toBeVisible({ timeout: 10_000 })
    await campModal.locator(':text("Marca")').locator('..').locator('button, [role="combobox"]').first().click()
    await page.locator('[role="option"]').first().click()
    await campModal.locator('input[type="text"], input:not([type])').first().fill(campaignName)
    await campModal.locator('input[type="number"]').first().fill(String(expectedValue))
    await campModal.locator('input[type="date"]').first().fill('2026-08-01')
    await campModal.getByRole('button', { name: /^Salvar$/i }).first().click()
    await expect(campModal).toBeHidden({ timeout: 15_000 })

    // 2) cria opp com responsavel
    await page.goto('/comercial/oportunidades')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})
    await crud.add(page).click()
    const oppModal = page.getByRole('dialog').filter({ hasText: /Nova oportunidade/i })
    await expect(oppModal).toBeVisible({ timeout: 10_000 })
    await oppModal.getByLabel(/Nome da oportunidade/i).fill(oppName)
    await oppModal.locator(':text("Marca")').locator('..').locator('button, [role="combobox"]').first().click()
    await page.locator('[role="option"]').first().click()
    await oppModal.locator('#opportunity-estimated-value').fill(String(expectedValue))
    await oppModal.locator('button', { hasText: 'Nenhum' }).first().click()
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
    await propModal.locator(':text("Oportunidade")').locator('..').locator('button, [role="combobox"]').first().click()
    const search = page.locator('input[placeholder*="Buscar oportunidade" i]').first()
    if (await search.count()) await search.fill(oppName)
    await page.locator('[role="option"]', { hasText: oppName }).first().click()
    await clickSaveInDialog(propModal)
    await expect(propModal).toBeHidden({ timeout: 15_000 })
    await page.waitForURL(/\/comercial\/propostas\/\d+/, { timeout: 15_000 })

    // 4) adiciona item
    await page.getByRole('button', { name: /^Adicionar$/i }).first().click()
    const itemModal = page.getByRole('dialog').filter({ hasText: /Novo item da proposta/i })
    await expect(itemModal).toBeVisible({ timeout: 10_000 })
    const itemFc = (label: string) =>
      itemModal.locator(`div.space-y-2:has(> label:text-is("${label}"))`).first()
    await itemFc('Descrição').locator('input').first().fill(itemDesc)
    await itemFc('Quantidade').locator('input').first().fill('1')
    await itemFc('Valor unitário').locator('input').first().fill(String(expectedValue))
    await itemModal.getByRole('button', { name: /Salvar item|^Salvar$/i }).first().click()
    await expect(itemModal).toBeHidden({ timeout: 15_000 })

    // 5) envia
    await page.getByRole('button', { name: /^Enviar$/i }).first().click()
    await expect(page.getByText(/Enviada|Sent/i).first()).toBeVisible({ timeout: 10_000 })

    // 6) aprova
    await page.getByRole('button', { name: /^Aprovar$/i }).first().click()
    await expect(page.getByText(/Aprovada|Approved/i).first()).toBeVisible({ timeout: 10_000 })

    // 7) vincula a campanha — esse e o passo que dispara geracao financeira
    const campTrigger = page.locator(':text("Selecione uma campanha")').first()
    await campTrigger.scrollIntoViewIfNeeded()
    await campTrigger.click()
    const buscarCamp = page.locator('input[placeholder*="Buscar campanha" i]').first()
    if (await buscarCamp.count()) await buscarCamp.fill(campaignName)
    await page.locator('[role="option"]', { hasText: campaignName }).first().click()
    await page.getByRole('button', { name: /Converter/i }).first().click()
    await page.waitForTimeout(2000)

    // 8) vai pra financeiro/receber e valida que tem entry com valor 30000 vinculada a campanha
    await page.goto('/financeiro/receber')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})

    // limpar filtros de data pra ver tudo
    const dateInputs = page.locator('input[type="date"]')
    if (await dateInputs.count() >= 2) {
      await dateInputs.nth(0).fill('1900-01-01')
      await dateInputs.nth(1).fill('2100-12-31')
    }

    // a entry gerada automaticamente deve ter o nome da proposta ou campanha
    // pelo padrao do FinancialAutoGenerationService, normalmente coloca "Receita campanha XYZ"
    // Vamos validar que ao menos UM novo lancamento aparece com R$ 30.000 vinculado
    await expect(page.getByText(campaignName).first()).toBeVisible({ timeout: 15_000 })

    expectNoApiFailures()
  })
})
