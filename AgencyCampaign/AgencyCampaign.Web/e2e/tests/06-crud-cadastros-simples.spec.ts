import { test, expect } from '../fixtures/test'
import { crud, clickSaveInDialog } from '../fixtures/helpers'

interface CrudSpec {
  path: string
  modalTitle: RegExp
  newButton?: RegExp
}

const specs: CrudSpec[] = [
  { path: '/configuracao/origens-oportunidade', modalTitle: /Nova origem|Nova origem de oportunidade/i },
  { path: '/configuracao/tags-oportunidade', modalTitle: /Nova tag|Nova tag de oportunidade/i },
  { path: '/configuracao/tipos-entrega', modalTitle: /tipo de entrega|Novo tipo/i },
  { path: '/configuracao/plataformas', modalTitle: /plataforma|Nova plataforma|Nova rede social|rede social/i },
  { path: '/configuracao/status-creators', modalTitle: /status|Novo status/i },
  { path: '/configuracao/pipeline-comercial', modalTitle: /estágio|Novo estágio|estagio/i },
  { path: '/configuracao/contas-financeiras', modalTitle: /conta|Nova conta|bancária|bancaria/i },
  { path: '/configuracao/subcategorias-financeiras', modalTitle: /subcategoria|Nova subcategoria/i },
]

test.describe('CRUD - cadastros simples (cria registro novo)', () => {
  for (const spec of specs) {
    test(`cria registro em ${spec.path}`, async ({ page, expectNoApiFailures }) => {
      const stamp = Date.now()
      const recordName = `E2E ${stamp}`

      await page.goto(spec.path)
      await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})

      // botao do PageLayout no padrao Archon: "Incluir"
      const newButton = crud.add(page)
      await expect(newButton).toBeVisible({ timeout: 10_000 })
      await newButton.click()

      const modal = page.getByRole('dialog').filter({ hasText: spec.modalTitle })
      await expect(modal).toBeVisible({ timeout: 10_000 })

      // campo Nome — pegar o primeiro input visivel do tipo texto dentro do modal
      const nameInput = modal.locator('input[type="text"], input:not([type])').first()
      await nameInput.fill(recordName)

      // Salvar
      await clickSaveInDialog(modal)

      // modal fecha
      await expect(modal).toBeHidden({ timeout: 15_000 })

      // aumentar page size pra garantir que o registro recem criado esteja visivel
      const pageSizeSelect = page.locator('select').filter({ hasText: /5|10|20|50/ }).first()
      if (await pageSizeSelect.count()) {
        await pageSizeSelect.selectOption('50').catch(() => {})
      }

      // registro aparece (na listagem ou em qualquer parte da pagina)
      await expect(page.getByText(recordName).first()).toBeVisible({ timeout: 15_000 })

      expectNoApiFailures()
    })
  }
})
