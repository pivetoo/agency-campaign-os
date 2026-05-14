import { test, expect } from '../fixtures/test'
import { crud, rowWithText, clickSaveInDialog } from '../fixtures/helpers'

// EDIT em Opportunity, Campaign, Proposal (entidades grandes que so tinham CREATE testado)
// Nenhuma delas tem DELETE via UI — somente EDIT e operacoes de status

test.describe('EDIT - oportunidade', () => {
  test('cria oportunidade, edita nome e valor', async ({ page, expectNoApiFailures }) => {
    const stamp = Date.now()
    const original = `E2E OppEdit ${stamp}`
    const renamed = `${original} (atualizado)`

    await page.goto('/comercial/oportunidades')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})

    // CREATE
    await crud.add(page).click()
    const newModal = page.getByRole('dialog').filter({ hasText: /Nova oportunidade/i })
    await expect(newModal).toBeVisible({ timeout: 10_000 })
    await newModal.getByLabel(/Nome da oportunidade/i).fill(original)
    await newModal.locator(':text("Marca")').locator('..').locator('button, [role="combobox"]').first().click()
    await page.locator('[role="option"]').first().click()
    await newModal.locator('#opportunity-estimated-value').fill('10000')
    await newModal.getByRole('button', { name: /^Salvar$/i }).first().click()
    await expect(newModal).toBeHidden({ timeout: 15_000 })

    // EDIT — selecionar a linha + Editar
    const pageSizeSelect = page.locator('select').filter({ hasText: /5|10|20|50/ }).first()
    if (await pageSizeSelect.count()) await pageSizeSelect.selectOption('50').catch(() => {})

    const row = rowWithText(page, original).first()
    await expect(row).toBeVisible({ timeout: 15_000 })
    await row.click()
    await expect(row).toHaveAttribute('data-state', 'selected', { timeout: 5_000 })

    await crud.edit(page).click()
    const editModal = page.getByRole('dialog').filter({ hasText: /Editar oportunidade/i })
    await expect(editModal).toBeVisible({ timeout: 10_000 })

    // mudar nome (input com id #opportunity-name)
    await editModal.locator('#opportunity-name').fill(renamed)
    // mudar valor
    await editModal.locator('#opportunity-estimated-value').fill('15000')

    await editModal.getByRole('button', { name: /^Salvar$/i }).first().click()
    await expect(editModal).toBeHidden({ timeout: 15_000 })

    // valida que linha agora tem o nome novo
    await expect(rowWithText(page, renamed).first()).toBeVisible({ timeout: 15_000 })

    expectNoApiFailures()
  })
})

test.describe('EDIT - campanha', () => {
  test('cria campanha, edita nome', async ({ page, expectNoApiFailures }) => {
    const stamp = Date.now()
    const original = `E2E CampEdit ${stamp}`
    const renamed = `${original} (atualizado)`

    await page.goto('/campanhas')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})

    // CREATE
    await crud.add(page).click()
    const newModal = page.getByRole('dialog').filter({ hasText: /Nova campanha/i })
    await expect(newModal).toBeVisible({ timeout: 10_000 })
    await newModal.locator(':text("Marca")').locator('..').locator('button, [role="combobox"]').first().click()
    await page.locator('[role="option"]').first().click()
    await newModal.locator('input[type="text"], input:not([type])').first().fill(original)
    await newModal.locator('input[type="number"]').first().fill('10000')
    await newModal.locator('input[type="date"]').first().fill('2026-07-01')
    await newModal.getByRole('button', { name: /^Salvar$/i }).first().click()
    await expect(newModal).toBeHidden({ timeout: 15_000 })

    // EDIT
    const pageSizeSelect = page.locator('select').filter({ hasText: /5|10|20|50/ }).first()
    if (await pageSizeSelect.count()) await pageSizeSelect.selectOption('50').catch(() => {})

    const row = rowWithText(page, original).first()
    await expect(row).toBeVisible({ timeout: 15_000 })
    await row.click()
    await expect(row).toHaveAttribute('data-state', 'selected', { timeout: 5_000 })

    await crud.edit(page).click()
    const editModal = page.getByRole('dialog').filter({ hasText: /Editar campanha/i })
    await expect(editModal).toBeVisible({ timeout: 10_000 })

    // mudar nome (primeiro input texto)
    await editModal.locator('input[type="text"], input:not([type])').first().fill(renamed)
    await editModal.getByRole('button', { name: /^Salvar$/i }).first().click()
    await expect(editModal).toBeHidden({ timeout: 15_000 })

    await expect(rowWithText(page, renamed).first()).toBeVisible({ timeout: 15_000 })

    expectNoApiFailures()
  })
})

test.describe('EDIT - proposta', () => {
  test('cria proposta, edita descricao no detalhe', async ({ page, expectNoApiFailures }) => {
    const stamp = Date.now()
    const oppName = `E2E PropEdit ${stamp}`
    const newDescription = `Descrição editada ${stamp}`

    // 1) cria opp com responsavel
    await page.goto('/comercial/oportunidades')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})
    await crud.add(page).click()
    const oppModal = page.getByRole('dialog').filter({ hasText: /Nova oportunidade/i })
    await expect(oppModal).toBeVisible({ timeout: 10_000 })
    await oppModal.getByLabel(/Nome da oportunidade/i).fill(oppName)
    await oppModal.locator(':text("Marca")').locator('..').locator('button, [role="combobox"]').first().click()
    await page.locator('[role="option"]').first().click()
    await oppModal.locator('#opportunity-estimated-value').fill('10000')
    await oppModal.locator('button', { hasText: 'Nenhum' }).first().click()
    const respOption = page.locator('[role="option"]').filter({ hasNotText: /^Nenhum$/i }).first()
    await expect(respOption).toBeVisible({ timeout: 10_000 })
    await respOption.click()
    await oppModal.getByRole('button', { name: /^Salvar$/i }).first().click()
    await expect(oppModal).toBeHidden({ timeout: 15_000 })

    // 2) cria proposta vinculada
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

    // 3) clicar em "Editar" da proposta no header da pagina de detalhe
    await crud.edit(page).click()
    const editModal = page.getByRole('dialog').filter({ hasText: /Editar proposta/i })
    await expect(editModal).toBeVisible({ timeout: 10_000 })

    // editar descricao (input com label "Descrição")
    const descLabel = editModal.locator('label', { hasText: /^Descrição$/ }).first()
    const descInput = descLabel.locator('xpath=following::input[1]').first()
    await descInput.fill(newDescription)

    await editModal.getByRole('button', { name: /^Salvar$/i }).first().click()
    await expect(editModal).toBeHidden({ timeout: 15_000 })

    // detalhe da proposta nao renderiza descricao explicitamente; basta validar update sem erro
    await page.waitForTimeout(800)

    expectNoApiFailures()
  })
})
