import { test, expect } from '../fixtures/test'

interface CrudSpec {
  path: string
  modalTitleNew: RegExp
  modalTitleEdit: RegExp
  supportsDelete: boolean
}

const specs: CrudSpec[] = [
  { path: '/configuracao/origens-oportunidade', modalTitleNew: /Nova origem/i, modalTitleEdit: /Editar origem/i, supportsDelete: true },
  { path: '/configuracao/tags-oportunidade', modalTitleNew: /Nova tag/i, modalTitleEdit: /Editar tag/i, supportsDelete: true },
  { path: '/configuracao/tipos-entrega', modalTitleNew: /tipo de entrega|Novo tipo/i, modalTitleEdit: /Editar tipo/i, supportsDelete: false },
  { path: '/configuracao/plataformas', modalTitleNew: /Nova plataforma|Nova rede social|rede social/i, modalTitleEdit: /Editar plataforma|Editar rede/i, supportsDelete: false },
  { path: '/configuracao/status-creators', modalTitleNew: /status|Novo status/i, modalTitleEdit: /Editar status/i, supportsDelete: false },
  { path: '/configuracao/pipeline-comercial', modalTitleNew: /estágio|Novo estágio|estagio/i, modalTitleEdit: /Editar estágio|Editar estagio/i, supportsDelete: false },
  { path: '/configuracao/contas-financeiras', modalTitleNew: /conta|Nova conta|bancária|bancaria/i, modalTitleEdit: /Editar conta/i, supportsDelete: true },
  { path: '/configuracao/subcategorias-financeiras', modalTitleNew: /subcategoria|Nova subcategoria/i, modalTitleEdit: /Editar subcategoria/i, supportsDelete: true },
]

test.describe('CRUD - edit + delete (cadastros simples)', () => {
  for (const spec of specs) {
    const suffix = spec.supportsDelete ? 'cria, edita e exclui' : 'cria e edita (sem delete suportado)'
    test(`${suffix} em ${spec.path}`, async ({ page, expectNoApiFailures }) => {
      const stamp = Date.now()
      const original = `E2E Edit ${stamp}`
      const renamed = `${original} (renomeado)`

      await page.goto(spec.path)
      await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})

      // ============ CREATE ============
      await page.getByRole('button', { name: /^Incluir$|^Novo$|^Nova$/i }).first().click()

      const newModal = page.getByRole('dialog').filter({ hasText: spec.modalTitleNew })
      await expect(newModal).toBeVisible({ timeout: 10_000 })

      const nameInputCreate = newModal.locator('input[type="text"], input:not([type])').first()
      await nameInputCreate.fill(original)
      await newModal.getByRole('button', { name: /^Salvar$/i }).first().click()
      await expect(newModal).toBeHidden({ timeout: 15_000 })

      // aumentar page size para 50 pra garantir que o registro recem criado esta visivel
      const pageSizeSelect = page.locator('select').filter({ hasText: /5|10|20|50/ }).first()
      if (await pageSizeSelect.count()) {
        await pageSizeSelect.selectOption('50')
      }

      // se tabela tem muitas paginas, ir pra ultima onde o registro novo esta
      const lastPageBtn = page.locator('button[aria-label*="última" i], button[title*="última" i]').first()
      if (await lastPageBtn.count() && await lastPageBtn.isEnabled()) {
        await lastPageBtn.click().catch(() => {})
        await page.waitForTimeout(300)
      }

      const row = page.locator('[data-row="true"]', { hasText: original }).first()
      await expect(row).toBeVisible({ timeout: 15_000 })

      // ============ EDIT ============
      await row.click()
      await expect(row).toHaveAttribute('data-state', 'selected', { timeout: 5_000 })

      await page.getByRole('button', { name: /^Editar$/i }).first().click()

      const editModal = page.getByRole('dialog').filter({ hasText: spec.modalTitleEdit })
      await expect(editModal).toBeVisible({ timeout: 10_000 })

      const nameInputEdit = editModal.locator('input[type="text"], input:not([type])').first()
      await nameInputEdit.fill(renamed)
      await editModal.getByRole('button', { name: /^Salvar$/i }).first().click()
      await expect(editModal).toBeHidden({ timeout: 15_000 })

      const renamedRow = page.locator('[data-row="true"]', { hasText: renamed }).first()
      await expect(renamedRow).toBeVisible({ timeout: 15_000 })

      // ============ DELETE (somente para entidades que suportam) ============
      if (spec.supportsDelete) {
        page.on('dialog', (dialog) => {
          void dialog.accept()
        })

        await renamedRow.click()
        await expect(renamedRow).toHaveAttribute('data-state', 'selected', { timeout: 5_000 })

        await page.getByRole('button', { name: /^Excluir$/i }).first().click()

        await expect(page.locator('[data-row="true"]', { hasText: renamed })).toHaveCount(0, { timeout: 15_000 })
      }

      expectNoApiFailures()
    })
  }
})
