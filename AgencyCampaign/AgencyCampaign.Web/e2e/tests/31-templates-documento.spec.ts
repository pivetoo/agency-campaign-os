import { test, expect } from '../fixtures/test'

// Spec 31: Templates de documento (Configuracao)
// CRUD: criar template novo com tipo, corpo e descricao -> editar nome
// -> excluir.

test.describe('Configuracao - Templates de documento', () => {
  test('cria, edita e exclui template de documento', async ({ page, expectNoApiFailures }) => {
    const stamp = Date.now()
    const name = `E2E Template Doc ${stamp}`
    const renamed = `${name} - editado`

    await page.goto('/configuracao/templates-documento')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})

    // 1) header
    await expect(page.getByRole('heading', { name: /Templates de documento/i })).toBeVisible({ timeout: 15_000 })

    // 2) cria template
    await page.getByRole('button', { name: /^Incluir$|^Novo$|^Nova$/i }).first().click()
    const modal = page.getByRole('dialog').filter({ hasText: /Novo template de documento/i })
    await expect(modal).toBeVisible({ timeout: 10_000 })

    await modal.getByPlaceholder(/Contrato padrão de creator/i).fill(name)

    // tipo (combobox)
    await modal.getByRole('combobox').first().click()
    await expect.poll(async () => page.getByRole('option').count(), { timeout: 10_000 }).toBeGreaterThan(0)
    await page.getByRole('option').first().click()

    // descricao (opcional)
    await modal.getByPlaceholder(/Texto curto/i).fill('Template criado via E2E')

    // corpo (obrigatorio)
    await modal.locator('textarea').fill('Corpo de contrato gerado por teste E2E.')

    await modal.getByRole('button', { name: /^Salvar$/ }).first().click()
    await expect(modal).toBeHidden({ timeout: 15_000 })

    // 3) confirma na tabela
    const pageSizeSelect = page.locator('select').filter({ hasText: /5|10|20|50/ }).first()
    if (await pageSizeSelect.count()) await pageSizeSelect.selectOption('50').catch(() => {})

    const row = page.locator('[data-row="true"]', { hasText: name }).first()
    await expect(row).toBeVisible({ timeout: 15_000 })

    // 4) seleciona, abre Editar
    await row.click()
    await expect(row).toHaveAttribute('data-state', 'selected', { timeout: 5_000 })
    await page.getByRole('button', { name: /^Editar$/ }).first().click()
    const editModal = page.getByRole('dialog').filter({ hasText: /Editar template de documento/i })
    await expect(editModal).toBeVisible({ timeout: 10_000 })

    const nameInput = editModal.getByPlaceholder(/Contrato padrão de creator/i)
    await nameInput.fill(renamed)
    await editModal.getByRole('button', { name: /^Salvar$/ }).first().click()
    await expect(editModal).toBeHidden({ timeout: 15_000 })

    const renamedRow = page.locator('[data-row="true"]', { hasText: renamed }).first()
    await expect(renamedRow).toBeVisible({ timeout: 10_000 })

    // 5) exclui (com confirm)
    page.on('dialog', (d) => { void d.accept() })
    await renamedRow.click()
    await expect(renamedRow).toHaveAttribute('data-state', 'selected', { timeout: 5_000 })
    await page.getByRole('button', { name: /^Excluir$/ }).click()
    await expect(page.locator('[data-row="true"]', { hasText: renamed })).toHaveCount(0, { timeout: 15_000 })

    expectNoApiFailures()
  })
})
