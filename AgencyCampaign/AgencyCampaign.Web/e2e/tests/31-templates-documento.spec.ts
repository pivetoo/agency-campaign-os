import { test, expect } from '../fixtures/test'
import { crud, rowWithText, confirmDelete, expectPageTitle } from '../fixtures/helpers'

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
    await expectPageTitle(page, /Templates? de documentos?|Modelos de contrato/i, 15_000)

    // 2) cria template
    await crud.add(page).click()
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

    const row = rowWithText(page, name).first()
    await expect(row).toBeVisible({ timeout: 15_000 })

    // 4) seleciona, abre Editar
    await row.click()
    await expect(row).toHaveAttribute('data-state', 'selected', { timeout: 5_000 })
    await crud.edit(page).click()
    const editModal = page.getByRole('dialog').filter({ hasText: /Editar template de documento/i })
    await expect(editModal).toBeVisible({ timeout: 10_000 })

    const nameInput = editModal.getByPlaceholder(/Contrato padrão de creator/i)
    await nameInput.fill(renamed)
    await editModal.getByRole('button', { name: /^Salvar$/ }).first().click()
    await expect(editModal).toBeHidden({ timeout: 15_000 })

    const renamedRow = rowWithText(page, renamed).first()
    await expect(renamedRow).toBeVisible({ timeout: 10_000 })

    // 5) exclui (com ConfirmModal). Para template novo nunca usado, o backend remove
    //    do banco; quando ja vinculado a documentos, marca como Inativo.
    //    Aceita as duas situacoes.
    await renamedRow.click()
    const excluirBtn = crud.delete(page)
    await expect(excluirBtn).toBeEnabled({ timeout: 5_000 })

    const deleteResp = page.waitForResponse(
      (resp) => /\/api\/CampaignDocumentTemplates\/Delete\/\d+/i.test(resp.url()),
      { timeout: 15_000 },
    )
    await excluirBtn.click()
    await confirmDelete(page)
    const resp = await deleteResp
    expect(resp.status(), `delete deveria retornar sucesso, recebeu ${resp.status()}`).toBeLessThan(400)

    // Apos delete, o backend pode remover do banco (template nao usado) OU marcar como Inativo.
    // Aguarda a lista refletir uma das duas situacoes (refresh pode levar mais que 15s).
    const finalRow = rowWithText(page, renamed)
    await expect
      .poll(async () => {
        if ((await finalRow.count()) === 0) return 'removed'
        const text = (await finalRow.first().innerText()).toLowerCase()
        return text.includes('inativo') ? 'inactive' : 'still-active'
      }, { timeout: 30_000 })
      .not.toBe('still-active')

    expectNoApiFailures()
  })
})
