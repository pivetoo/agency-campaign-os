import { test, expect } from '../fixtures/test'
import { crud, clickSaveInDialog } from '../fixtures/helpers'

// Spec 20 (reescrito): a aba/modal "Negociacoes" foi APOSENTADA (OpportunityNegotiation
// removida; /comercial/negociacoes so redireciona). A aprovacao agora e ancorada na
// Proposta ("Solicitar aprovacao") e decidida na inbox /comercial/aprovacoes.
// Cobre o caminho atual ate onde e deterministico em homologacao:
//  - cria opp (com responsavel) + proposta + item
//  - abre "Solicitar aprovacao" e valida os campos do fluxo atual + o gate de
//    maker-checker (submit desabilitado sem aprovador designado)
//  - valida que a inbox /comercial/aprovacoes renderiza sem erro de API
// O ciclo completo (selecionar aprovador != solicitante, submeter e decidir na
// inbox) exige um 2o usuario ativo no contrato; documentado como gap em
// ideas/mvp-kanvas/e2e-mapeamento-cobertura.md.

test.describe('Comercial - aprovacao ancorada na proposta (fluxo atual)', () => {
  test('proposta abre o modal de solicitar aprovacao (maker-checker) e a inbox renderiza', async ({ page, expectNoApiFailures }) => {
    const stamp = Date.now()
    const oppName = `E2E OppApr ${stamp}`
    const itemDesc = `E2E ItemApr ${stamp}`

    // 1) oportunidade com responsavel (necessario para criar proposta)
    await page.goto('/comercial/oportunidades')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})
    await crud.add(page).click()
    const oppModal = page.getByRole('dialog').filter({ hasText: /Nova oportunidade/i })
    await expect(oppModal).toBeVisible({ timeout: 10_000 })
    await oppModal.getByLabel(/Nome da oportunidade/i).fill(oppName)
    const brandTrigger = oppModal.locator(':text("Marca")').locator('..').locator('button, [role="combobox"]').first()
    await brandTrigger.click()
    await page.locator('[role="option"]').first().click()
    await oppModal.locator('#opportunity-estimated-value').fill('18000')
    const respLabel = oppModal.locator('label', { hasText: /^Responsável comercial$/ }).first()
    await expect(respLabel).toBeVisible({ timeout: 5_000 })
    const respTrigger = respLabel.locator('xpath=following::*[self::button or @role="combobox"][1]').first()
    await respTrigger.click()
    const respOption = page.locator('[role="option"]').filter({ hasNotText: /^Nenhum$/i }).first()
    await expect(respOption).toBeVisible({ timeout: 10_000 })
    await respOption.click()
    await oppModal.getByRole('button', { name: /^Salvar$/i }).first().click()
    await expect(oppModal).toBeHidden({ timeout: 15_000 })

    // 2) proposta vinculada
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

    // 3) item (para a proposta ter valor)
    await page.getByRole('button', { name: /^Adicionar$/i }).first().click()
    const itemModal = page.getByRole('dialog').filter({ hasText: /Novo item da proposta/i })
    await expect(itemModal).toBeVisible({ timeout: 10_000 })
    const itemFc = (label: string) => itemModal.locator(`div.space-y-2:has(> label:text-is("${label}"))`).first()
    await itemFc('Descrição').locator('input').first().fill(itemDesc)
    await itemFc('Quantidade').locator('input').first().fill('1')
    await itemFc('Valor unitário').locator('input').first().fill('18000')
    await itemModal.getByRole('button', { name: /Salvar item|^Salvar$/i }).first().click()
    await expect(itemModal).toBeHidden({ timeout: 15_000 })

    // 4) abrir "Solicitar aprovacao" e validar campos atuais + gate maker-checker
    const requestBtn = page.getByRole('button', { name: /Solicitar aprova/i }).first()
    await expect(requestBtn).toBeVisible({ timeout: 10_000 })
    await requestBtn.click()
    const aprModal = page.getByRole('dialog').filter({ hasText: /aprova/i }).first()
    await expect(aprModal).toBeVisible({ timeout: 10_000 })
    await expect(aprModal.getByText(/Solicitado por/i)).toBeVisible()
    await expect(aprModal.getByText(/Motivo/i)).toBeVisible()
    await expect(aprModal.getByText(/Aprovador/i)).toBeVisible()
    // sem designar aprovador, o submit fica desabilitado (segregacao de funcoes)
    const submitBtn = aprModal.getByRole('button', { name: /Solicitar|Enviar|Confirmar/i }).last()
    await expect(submitBtn).toBeDisabled()
    await aprModal.getByRole('button', { name: /^Cancelar$/i }).first().click().catch(() => {})
    await expect(aprModal).toBeHidden({ timeout: 10_000 })

    // 5) a inbox de aprovacoes renderiza sem erro de API
    await page.goto('/comercial/aprovacoes')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})
    await expect(page.getByTestId('page-title').first()).toBeVisible({ timeout: 20_000 })

    expectNoApiFailures()
  })
})
