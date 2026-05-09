import { test, expect } from '../fixtures/test'

// Fluxo: opp -> negociacao -> solicitar aprovacao -> aprovar na fila

test.describe('Aprovacao comercial - fluxo completo', () => {
  test('cria negociacao + solicita aprovacao + aprova na fila', async ({ page, expectNoApiFailures }) => {
    const stamp = Date.now()
    const oppName = `E2E OppApr ${stamp}`
    const negName = `E2E Neg ${stamp}`
    const reason = `E2E Aprov ${stamp}`

    // 1) cria oportunidade com responsavel
    await page.goto('/comercial/oportunidades')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})
    await page.getByRole('button', { name: /^Incluir$|Novo Lead/i }).first().click()
    const oppModal = page.getByRole('dialog').filter({ hasText: /Nova oportunidade/i })
    await expect(oppModal).toBeVisible({ timeout: 10_000 })
    await oppModal.getByLabel(/Nome da oportunidade/i).fill(oppName)
    await oppModal.locator(':text("Marca")').locator('..').locator('button, [role="combobox"]').first().click()
    await page.locator('[role="option"]').first().click()
    await oppModal.locator('#opportunity-estimated-value').fill('15000')
    await oppModal.locator('button', { hasText: 'Nenhum' }).first().click()
    const respOption = page.locator('[role="option"]').filter({ hasNotText: /^Nenhum$/i }).first()
    await expect(respOption).toBeVisible({ timeout: 10_000 })
    await respOption.click()
    await oppModal.getByRole('button', { name: /^Salvar$/i }).first().click()
    await expect(oppModal).toBeHidden({ timeout: 15_000 })

    // 2) abre detalhe
    const pageSizeSelect = page.locator('select').filter({ hasText: /5|10|20|50/ }).first()
    if (await pageSizeSelect.count()) await pageSizeSelect.selectOption('50').catch(() => {})

    const row = page.locator('[data-row="true"]', { hasText: oppName }).first()
    await expect(row).toBeVisible({ timeout: 15_000 })
    const openBtn = row.locator('button').filter({ hasText: /Abrir/i }).first()
    if (await openBtn.count()) await openBtn.click()
    else await row.dblclick()
    await page.waitForURL(/\/comercial\/oportunidades\/\d+/, { timeout: 15_000 })

    // 3) tab Negociações
    await page.getByRole('tab', { name: /Negociações/i }).click().catch(async () => {
      await page.getByText('Negociações', { exact: false }).first().click()
    })
    await page.waitForTimeout(500)

    // 4) criar negociacao
    await page.getByRole('button', { name: /Nova negociação/i }).first().click()
    const negModal = page.getByRole('dialog').filter({ hasText: /Nova negociação/i })
    await expect(negModal).toBeVisible({ timeout: 10_000 })
    const negFc = (label: string) =>
      negModal.locator(`div.space-y-2:has(> label:text-is("${label}"))`).first()
    await negFc('Título').locator('input').first().fill(negName)
    await negFc('Valor').locator('input').first().fill('14500')
    await negModal.getByRole('button', { name: /^Salvar$/i }).first().click()
    await expect(negModal).toBeHidden({ timeout: 15_000 })

    // 5) selecionar negociacao na tabela e solicitar aprovacao
    const negRow = page.locator('[data-row="true"]', { hasText: negName }).first()
    await expect(negRow).toBeVisible({ timeout: 15_000 })
    await negRow.click()
    await expect(negRow).toHaveAttribute('data-state', 'selected', { timeout: 5_000 })

    // botao Solicitar aprovação
    await page.getByRole('button', { name: /Solicitar aprovação/i }).first().click()
    const aprModal = page.getByRole('dialog').filter({ hasText: /Solicitar aprovação/i })
    await expect(aprModal).toBeVisible({ timeout: 10_000 })

    const aprFc = (label: string) =>
      aprModal.locator(`div.space-y-2:has(> label:text-is("${label}"))`).first()

    // tipo de aprovacao ja vem default; preencher Solicitado por + Motivo
    await aprFc('Solicitado por').locator('input').first().fill('E2E QA')
    await aprFc('Motivo').locator('input').first().fill(reason)

    await aprModal.getByRole('button', { name: /Solicitar/i }).first().click()
    await expect(aprModal).toBeHidden({ timeout: 15_000 })

    // 6) ir pra fila de aprovacoes comerciais
    await page.goto('/comercial/aprovacoes')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})

    // 7) o pedido aparece na lista (busca pelo nome da negociacao que e mostrado na coluna)
    await expect(page.getByText(negName).first()).toBeVisible({ timeout: 15_000 })

    // 8) selecionar e aprovar
    const aprovacaoRow = page.locator('[data-row="true"]', { hasText: negName }).first()
    await aprovacaoRow.click()
    await expect(aprovacaoRow).toHaveAttribute('data-state', 'selected', { timeout: 5_000 })

    await page.getByRole('button', { name: /Aprovar selecionada|^Aprovar$/i }).first().click()

    // dar tempo para o backend processar; valida via expectNoApiFailures
    await page.waitForTimeout(1500)

    expectNoApiFailures()
  })
})
