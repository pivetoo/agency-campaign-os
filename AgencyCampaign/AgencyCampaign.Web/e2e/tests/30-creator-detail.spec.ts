import { test, expect } from '../fixtures/test'
import { crud, rowWithText, creator, confirmDelete } from '../fixtures/helpers'

// Spec 30: Detail do Creator (perfil 360) com gestao de redes sociais
// Cobre: navegacao /creators -> "Abrir 360", validacao do header e KPIs,
// 3 abas (Redes sociais, Campanhas, Performance), CRUD de social handle.

test.describe('Creator Detail - perfil 360 + redes sociais', () => {
  test('cria creator, abre 360, adiciona/edita/remove handle social', async ({ page, expectNoApiFailures }) => {
    const stamp = Date.now()
    const creatorName = `E2E DetailCreator ${stamp}`

    // 1) cria creator
    await page.goto('/creators')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})

    await crud.add(page).click()
    const createModal = page.getByRole('dialog').filter({ hasText: /Novo influenciador|Novo creator/i })
    await expect(createModal).toBeVisible({ timeout: 10_000 })
    await createModal.locator('input[type="text"], input:not([type])').first().fill(creatorName)
    await createModal.getByRole('button', { name: /^Salvar$/ }).first().click()
    await expect(createModal).toBeHidden({ timeout: 15_000 })

    // 2) abre 360 via botao "Abrir 360" da linha
    const pageSizeSelect = page.locator('select').filter({ hasText: /5|10|20|50/ }).first()
    if (await pageSizeSelect.count()) await pageSizeSelect.selectOption('50').catch(() => {})

    const row = rowWithText(page, creatorName).first()
    await expect(row).toBeVisible({ timeout: 15_000 })
    await row.scrollIntoViewIfNeeded()
    await row.getByRole('button', { name: /Abrir 360/i }).click()

    await page.waitForURL(/\/creators\/\d+$/, { timeout: 10_000 })

    // 3) header com nome
    await expect(page.getByRole('heading', { name: new RegExp(creatorName, 'i') })).toBeVisible({ timeout: 15_000 })

    // 4) 4 KPIs no card de resumo
    for (const label of [
      'Campanhas',
      'Entregas publicadas',
      'Faturamento (bruto)',
      'On-time delivery',
    ]) {
      await expect(page.getByText(label, { exact: false }).first()).toBeVisible()
    }

    // 5) 3 abas
    for (const label of [/Redes sociais/, /Campanhas/, /Performance por plataforma/]) {
      await expect(page.getByRole('tab', { name: label }).first()).toBeVisible()
    }

    // 6) aba Redes sociais (default) - empty state
    await expect(page.getByText(/Nenhum handle social cadastrado/i)).toBeVisible({ timeout: 10_000 })

    // 7) cria handle
    await creator.addHandleButton(page).click()
    const handleModal = page.getByRole('dialog').filter({ hasText: /Novo handle social/i })
    await expect(handleModal).toBeVisible({ timeout: 10_000 })

    // plataforma (combobox)
    await handleModal.getByRole('combobox').first().click()
    await expect.poll(async () => page.getByRole('option').count(), { timeout: 10_000 }).toBeGreaterThan(0)
    await page.getByRole('option').first().click()

    // handle
    await handleModal.getByPlaceholder(/@usuario/i).fill(`@e2e_${stamp}`)
    // URL opcional
    await handleModal.getByPlaceholder(/^https:/).fill('https://instagram.com/e2e')

    await handleModal.getByRole('button', { name: /^Salvar$/ }).first().click()
    await expect(handleModal).toBeHidden({ timeout: 15_000 })

    // valida que o handle apareceu na tabela
    await expect(page.getByText(`@e2e_${stamp}`).first()).toBeVisible({ timeout: 10_000 })

    // 8) edita handle (clica icone Pencil)
    const handleRow = rowWithText(page, `@e2e_${stamp}`).first()
    await handleRow.locator('button').first().click()
    const editModal = page.getByRole('dialog').filter({ hasText: /Editar handle social/i })
    await expect(editModal).toBeVisible({ timeout: 10_000 })
    const handleInput = editModal.getByPlaceholder(/@usuario/i)
    await handleInput.fill(`@e2e_${stamp}_v2`)
    await editModal.getByRole('button', { name: /^Salvar$/ }).first().click()
    await expect(editModal).toBeHidden({ timeout: 15_000 })
    await expect(page.getByText(`@e2e_${stamp}_v2`).first()).toBeVisible({ timeout: 10_000 })

    // 9) deleta handle (botao Trash + ConfirmModal)
    const updatedRow = rowWithText(page, `@e2e_${stamp}_v2`).first()
    await updatedRow.locator('button').last().click()
    await confirmDelete(page)
    await expect(page.getByText(/Nenhum handle social cadastrado/i)).toBeVisible({ timeout: 15_000 })

    // 10) trocar para aba Campanhas - empty state esperado em creator novo
    await page.getByRole('tab', { name: /Campanhas/i }).click()
    await expect(page.getByTestId('creator-no-campaigns-empty')).toBeVisible({ timeout: 10_000 })

    // 11) trocar para aba Performance - empty state
    await page.getByRole('tab', { name: /Performance por plataforma/i }).click()
    await expect(page.getByText(/Sem entregas registradas para o creator/i)).toBeVisible({ timeout: 10_000 })

    expectNoApiFailures()
  })
})
