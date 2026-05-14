import { test, expect } from '../fixtures/test'
import { crud, rowWithText } from '../fixtures/helpers'

// EDIT nas 4 entidades principais (Marca, Creator, Email Template, Campanha)
// CREATE ja esta coberto em 07-crud-entidades-principais.spec.ts

test.describe('EDIT - entidades principais', () => {
  test('cria e edita marca', async ({ page, expectNoApiFailures }) => {
    const stamp = Date.now()
    const original = `E2E Marca ${stamp}`
    const renamed = `${original} (renomeado)`

    await page.goto('/marcas')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})

    // CREATE
    await crud.add(page).click()
    const newModal = page.getByRole('dialog').filter({ hasText: /Nova marca/i })
    await expect(newModal).toBeVisible({ timeout: 10_000 })
    await newModal.locator('input[type="text"], input:not([type])').first().fill(original)
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
    const editModal = page.getByRole('dialog').filter({ hasText: /Editar marca/i })
    await expect(editModal).toBeVisible({ timeout: 10_000 })
    await editModal.locator('input[type="text"], input:not([type])').first().fill(renamed)
    await editModal.getByRole('button', { name: /^Salvar$/i }).first().click()
    await expect(editModal).toBeHidden({ timeout: 15_000 })

    await expect(rowWithText(page, renamed).first()).toBeVisible({ timeout: 15_000 })

    expectNoApiFailures()
  })

  test('cria e edita creator', async ({ page, expectNoApiFailures }) => {
    const stamp = Date.now()
    const original = `E2E Creator ${stamp}`
    const renamed = `${original} (renomeado)`

    await page.goto('/creators')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})

    await crud.add(page).click()
    const newModal = page.getByRole('dialog').filter({ hasText: /Novo influenciador|Novo creator/i })
    await expect(newModal).toBeVisible({ timeout: 10_000 })
    await newModal.locator('input[type="text"], input:not([type])').first().fill(original)
    await newModal.getByRole('button', { name: /^Salvar$/i }).first().click()
    await expect(newModal).toBeHidden({ timeout: 15_000 })

    const pageSizeSelect = page.locator('select').filter({ hasText: /5|10|20|50/ }).first()
    if (await pageSizeSelect.count()) await pageSizeSelect.selectOption('50').catch(() => {})

    const row = rowWithText(page, original).first()
    await expect(row).toBeVisible({ timeout: 15_000 })
    await row.click()
    await expect(row).toHaveAttribute('data-state', 'selected', { timeout: 5_000 })

    await crud.edit(page).click()
    const editModal = page.getByRole('dialog').filter({ hasText: /Editar influenciador|Editar creator/i })
    await expect(editModal).toBeVisible({ timeout: 10_000 })
    await editModal.locator('input[type="text"], input:not([type])').first().fill(renamed)
    await editModal.getByRole('button', { name: /^Salvar$/i }).first().click()
    await expect(editModal).toBeHidden({ timeout: 15_000 })

    await expect(rowWithText(page, renamed).first()).toBeVisible({ timeout: 15_000 })

    expectNoApiFailures()
  })

  test('cria e edita template de e-mail', async ({ page, expectNoApiFailures }) => {
    const stamp = Date.now()
    const original = `E2E Template ${stamp}`
    const renamed = `${original} (renomeado)`

    await page.goto('/configuracao/templates-email')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})

    // CREATE
    await crud.add(page).click()
    const newModal = page.getByRole('dialog').filter({ hasText: /Novo template|template/i })
    await expect(newModal).toBeVisible({ timeout: 10_000 })

    await newModal.getByPlaceholder(/Confirmação de envio|envio de proposta/i).fill(original)
    const eventoTrigger = newModal.locator(':text("Evento")').locator('..').locator('button, [role="combobox"]').first()
    await eventoTrigger.click()
    const firstEventOption = page.locator('[role="option"]').first()
    await expect(firstEventOption).toBeVisible({ timeout: 10_000 })
    await firstEventOption.click()
    await newModal.getByPlaceholder(/proposta.*chegou|Assunto/i).fill(`Assunto ${stamp}`)
    await newModal.locator('textarea').first().fill(`<p>Corpo ${stamp}</p>`)
    await newModal.getByRole('button', { name: /^Salvar$/i }).first().click()
    await expect(newModal).toBeHidden({ timeout: 15_000 })

    const pageSizeSelect = page.locator('select').filter({ hasText: /5|10|20|50/ }).first()
    if (await pageSizeSelect.count()) await pageSizeSelect.selectOption('50').catch(() => {})

    // EDIT
    const row = rowWithText(page, original).first()
    await expect(row).toBeVisible({ timeout: 15_000 })
    await row.click()
    await expect(row).toHaveAttribute('data-state', 'selected', { timeout: 5_000 })

    await crud.edit(page).click()
    const editModal = page.getByRole('dialog').filter({ hasText: /Editar template|template/i })
    await expect(editModal).toBeVisible({ timeout: 10_000 })

    // primeiro input texto = nome
    await editModal.getByPlaceholder(/Confirmação de envio|envio de proposta/i).fill(renamed)
    await editModal.getByRole('button', { name: /^Salvar$/i }).first().click()
    await expect(editModal).toBeHidden({ timeout: 15_000 })

    await expect(rowWithText(page, renamed).first()).toBeVisible({ timeout: 15_000 })

    expectNoApiFailures()
  })
})
