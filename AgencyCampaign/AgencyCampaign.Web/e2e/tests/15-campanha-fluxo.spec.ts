import { test, expect } from '../fixtures/test'
import { crud, rowWithText, campaign } from '../fixtures/helpers'

test.describe('Campanha - fluxo operacional (caminho critico)', () => {
  test('cria campanha + adiciona creator + cria deliverable', async ({ page, expectNoApiFailures }) => {
    const stamp = Date.now()
    const campaignName = `E2E Campanha ${stamp}`
    const deliverableTitle = `E2E Entrega ${stamp}`

    // 1) cria campanha
    await page.goto('/campanhas')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})
    await crud.add(page).click()
    const campModal = page.getByRole('dialog').filter({ hasText: /Nova campanha/i })
    await expect(campModal).toBeVisible({ timeout: 10_000 })

    // marca
    await campModal.locator(':text("Marca")').locator('..').locator('button, [role="combobox"]').first().click()
    await page.locator('[role="option"]').first().click()

    // nome (primeiro input texto)
    await campModal.locator('input[type="text"], input:not([type])').first().fill(campaignName)
    // budget
    await campModal.locator('input[type="number"]').first().fill('20000')
    // inicio
    await campModal.locator('input[type="date"]').first().fill('2026-06-01')

    await campModal.getByRole('button', { name: /^Salvar$/i }).first().click()
    await expect(campModal).toBeHidden({ timeout: 15_000 })

    // 2) abrir detalhe da campanha — clicar no botao "Abrir" (ExternalLink) da linha
    const pageSizeSelect = page.locator('select').filter({ hasText: /5|10|20|50/ }).first()
    if (await pageSizeSelect.count()) await pageSizeSelect.selectOption('50').catch(() => {})

    const row = rowWithText(page, campaignName).first()
    await expect(row).toBeVisible({ timeout: 15_000 })
    // botao de "abrir detalhe" (ExternalLink icon — primeiro button na coluna actions)
    const openBtn = row.locator('button').filter({ hasNotText: /.+/ }).first()
    await openBtn.click()
    await page.waitForURL(/\/campanhas\/\d+/, { timeout: 10_000 })

    // 3) adicionar creator
    await campaign.addCreatorButton(page).click()
    const creatorModal = page.getByRole('dialog').filter({ hasText: /Adicionar creator|Adicionar influenciador/i })
    await expect(creatorModal).toBeVisible({ timeout: 10_000 })

    const fieldContainerCreator = (label: string) =>
      creatorModal.locator(`div.space-y-2:has(> label:text-is("${label}"))`).first()

    // creator (primeiro disponivel)
    await creatorModal.getByTestId('form-field-creator').locator('button, [role="combobox"]').first().click()
    await page.locator('[role="option"]').first().click()
    // valor combinado
    await fieldContainerCreator('Valor combinado').locator('input').first().fill('5000')

    await creatorModal.getByRole('button', { name: /^Salvar$|Adicionar/i }).first().click()
    await expect(creatorModal).toBeHidden({ timeout: 15_000 })

    // 4) trocar para tab Entregas
    await page.getByRole('tab', { name: /Entregas/i }).click().catch(async () => {
      await page.getByText('Entregas', { exact: false }).first().click()
    })

    // 5) criar deliverable
    await page.getByRole('button', { name: /Nova entrega/i }).first().click()
    const delivModal = page.getByRole('dialog').filter({ hasText: /Nova entrega/i })
    await expect(delivModal).toBeVisible({ timeout: 10_000 })

    const fc = (label: string) =>
      delivModal.locator(`div.space-y-2:has(> label:text-is("${label}"))`).first()

    // creator/influenciador (primeiro)
    const creatorFieldDeliv = delivModal
      .locator(`div.space-y-2:has(> label:text-matches("(Creator|Influenciador) da campanha", "i"))`)
      .first()
    await creatorFieldDeliv.locator('button, [role="combobox"]').first().click()
    await page.locator('[role="option"]').first().click()

    // titulo
    await fc('Título').locator('input').first().fill(deliverableTitle)

    // tipo (primeira opcao)
    const tipoTrigger = fc('Tipo').locator('button, [role="combobox"]').first()
    await tipoTrigger.click()
    await page.locator('[role="option"]').first().click()

    // plataforma (primeira opcao)
    const platTrigger = fc('Plataforma').locator('button, [role="combobox"]').first()
    await platTrigger.click()
    await page.locator('[role="option"]').first().click()

    // prazo
    await fc('Prazo').locator('input').first().fill('2026-06-15')

    // valor bruto
    await fc('Valor bruto').locator('input').first().fill('5000')

    await delivModal.getByRole('button', { name: /^Salvar$/i }).first().click()
    await expect(delivModal).toBeHidden({ timeout: 15_000 })

    // 5) deliverable aparece na tabela
    await expect(page.getByText(deliverableTitle).first()).toBeVisible({ timeout: 15_000 })

    expectNoApiFailures()
  })
})
