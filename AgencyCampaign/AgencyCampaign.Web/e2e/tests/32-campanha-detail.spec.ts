import { test, expect } from '../fixtures/test'
import { crud, rowWithText, campaign } from '../fixtures/helpers'

// Spec 32: Campanha detail - abas internas (Creators, Documentos, Entregas)
// Cobre os fluxos basicos das 3 tabs sem criar dado em massa.
// 1) cria campanha
// 2) abre /campanhas/:id pelo botao Eye
// 3) valida header + KPIs + 3 abas
// 4) percorre cada tab validando empty state e action buttons

test.describe('Campanha Detail - abas internas', () => {
  test('cria campanha, abre detalhe e percorre as 3 abas', async ({ page, expectNoApiFailures }) => {
    const stamp = Date.now()
    const campaignName = `E2E CampDetail ${stamp}`

    // 1) cria campanha
    await page.goto('/campanhas')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})

    await crud.add(page).click()
    const createModal = page.getByRole('dialog').filter({ hasText: /Nova campanha/i })
    await expect(createModal).toBeVisible({ timeout: 10_000 })

    // marca (combobox)
    await createModal.locator(':text("Marca")').locator('..').locator('button, [role="combobox"]').first().click()
    await expect.poll(async () => page.getByRole('option').count(), { timeout: 10_000 }).toBeGreaterThan(0)
    await page.getByRole('option').first().click()

    // nome, budget, data inicio
    await createModal.locator('input[type="text"], input:not([type])').first().fill(campaignName)
    await createModal.locator('input[type="number"]').first().fill('25000')
    await createModal.locator('input[type="date"]').first().fill('2026-09-01')

    await createModal.getByRole('button', { name: /^Salvar$/ }).first().click()
    await expect(createModal).toBeHidden({ timeout: 15_000 })

    // 2) abre detalhe via botao Eye da linha
    const pageSizeSelect = page.locator('select').filter({ hasText: /5|10|20|50/ }).first()
    if (await pageSizeSelect.count()) await pageSizeSelect.selectOption('50').catch(() => {})

    const row = rowWithText(page, campaignName).first()
    await expect(row).toBeVisible({ timeout: 15_000 })
    await row.scrollIntoViewIfNeeded()
    // botao Eye eh o ultimo button da linha
    await row.locator('button').last().click()
    await page.waitForURL(/\/campanhas\/\d+$/, { timeout: 10_000 })

    // 3) header com nome
    await expect(page.getByRole('heading', { name: new RegExp(campaignName, 'i') })).toBeVisible({ timeout: 15_000 })

    // 4) 6 KPIs no card de resumo
    for (const label of ['Status', 'Budget', /Creators|Influenciadores/, 'Entregas', 'Período', 'Objetivo']) {
      const matcher = typeof label === 'string' ? label : label
      await expect(page.getByText(matcher, { exact: false }).first()).toBeVisible()
    }

    // 5) 3 abas
    for (const label of [/Creators|Influenciadores/, /Documentos/, /Entregas/]) {
      await expect(page.getByRole('tab', { name: label }).first()).toBeVisible()
    }

    // 6) Tab Creators (default)
    await expect(page.getByText(/Nenhum (creator|influenciador) vinculado à campanha/i)).toBeVisible({ timeout: 10_000 })
    await expect(campaign.addCreatorButton(page)).toBeVisible()

    // 7) Tab Documentos
    await page.getByRole('tab', { name: /Documentos/i }).click()
    await expect(page.getByText(/Nenhum documento cadastrado/i)).toBeVisible({ timeout: 10_000 })
    for (const label of [
      'Enviar para assinatura',
      'Marcar assinado',
      'Enviar e-mail',
      'Gerar de template',
      'Novo documento',
    ]) {
      await expect(page.getByRole('button', { name: new RegExp(label, 'i') })).toBeVisible()
    }
    // acoes contextuais devem estar disabled sem selecao
    for (const label of ['Enviar para assinatura', 'Marcar assinado', 'Enviar e-mail']) {
      await expect(page.getByRole('button', { name: new RegExp(label, 'i') })).toBeDisabled()
    }

    // 8) Tab Entregas
    await page.getByRole('tab', { name: /^Entregas$/i }).click()
    await expect(page.getByText(/Nenhuma entrega cadastrada/i)).toBeVisible({ timeout: 10_000 })
    await expect(page.getByRole('button', { name: /Nova entrega/i })).toBeVisible()

    expectNoApiFailures()
  })
})
