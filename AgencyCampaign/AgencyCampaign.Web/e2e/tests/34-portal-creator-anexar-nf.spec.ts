import { test, expect } from '../fixtures/test'
import { crud, rowWithText, campaign, portal } from '../fixtures/helpers'

// Spec 34: Portal do Creator - upload de NF (caminho fim a fim)
// 1) cria creator + campanha + vincula creator
// 2) cria pagamento ao creator em /financeiro/repasses-creators
// 3) gera token de portal pro creator
// 4) abre portal em contexto sem auth, vai para /pagamentos
// 5) clica "Anexar NF", preenche numero/data/url e salva
// 6) confirma badge "NF anexada"

test.describe('Portal do Creator - upload de NF', () => {
  test('cria pagamento e creator anexa NF pelo portal', async ({ page, browser }) => {
    const stamp = Date.now()
    const creatorName = `E2E NFCreator ${stamp}`
    const campaignName = `E2E NFCamp ${stamp}`

    // 1) cria creator
    await page.goto('/creators')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})
    await crud.add(page).click()
    const createCreatorModal = page.getByRole('dialog').filter({ hasText: /Novo influenciador|Novo creator/i })
    await expect(createCreatorModal).toBeVisible({ timeout: 10_000 })
    await createCreatorModal.locator('input[type="text"], input:not([type])').first().fill(creatorName)
    await createCreatorModal.getByRole('button', { name: /^Salvar$/ }).first().click()
    await expect(createCreatorModal).toBeHidden({ timeout: 15_000 })

    // 2) cria campanha
    await page.goto('/campanhas')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})
    await crud.add(page).click()
    const campModal = page.getByRole('dialog').filter({ hasText: /Nova campanha/i })
    await expect(campModal).toBeVisible({ timeout: 10_000 })
    // marca
    await campModal.locator(':text("Marca")').locator('..').locator('button, [role="combobox"]').first().click()
    await expect.poll(async () => page.getByRole('option').count(), { timeout: 10_000 }).toBeGreaterThan(0)
    await page.getByRole('option').first().click()
    // nome / budget / data
    await campModal.locator('input[type="text"], input:not([type])').first().fill(campaignName)
    await campModal.locator('input[type="number"]').first().fill('10000')
    await campModal.locator('input[type="date"]').first().fill('2026-09-01')
    await campModal.getByRole('button', { name: /^Salvar$/ }).first().click()
    await expect(campModal).toBeHidden({ timeout: 15_000 })

    // 3) abre detalhe da campanha e adiciona o creator recem criado
    const pageSizeSelect = page.locator('select').filter({ hasText: /5|10|20|50/ }).first()
    if (await pageSizeSelect.count()) await pageSizeSelect.selectOption('50').catch(() => {})

    const campRow = rowWithText(page, campaignName).first()
    await expect(campRow).toBeVisible({ timeout: 15_000 })
    await campRow.scrollIntoViewIfNeeded()
    await campRow.locator('button').last().click()
    await page.waitForURL(/\/campanhas\/\d+$/, { timeout: 10_000 })

    await campaign.addCreatorButton(page).click()
    const addCreatorModal = page.getByRole('dialog').filter({ hasText: /Adicionar creator|Adicionar influenciador/i })
    await expect(addCreatorModal).toBeVisible({ timeout: 10_000 })

    const fc = (label: string) =>
      addCreatorModal.locator(`div.space-y-2:has(> label:text-is("${label}"))`).first()
    await addCreatorModal.getByTestId('form-field-creator').locator('button, [role="combobox"]').first().click()
    // busca pelo creator recem criado
    const creatorSearch = page.locator('input[placeholder="Buscar"]').first()
    await creatorSearch.click()
    await creatorSearch.pressSequentially(creatorName, { delay: 30 })
    await page.getByText('Buscando...').waitFor({ state: 'detached', timeout: 30_000 }).catch(() => {})
    const creatorOpt34 = page.getByRole('option', { name: new RegExp(creatorName, 'i') }).first()
    await creatorOpt34.waitFor({ state: 'visible', timeout: 30_000 })
    await creatorOpt34.click()
    await fc('Valor combinado').locator('input').first().fill('5000')
    await addCreatorModal.getByRole('button', { name: /^Salvar$|Adicionar/i }).first().click()
    await expect(addCreatorModal).toBeHidden({ timeout: 15_000 })

    // 4) cria pagamento em /financeiro/repasses-creators
    await page.goto('/financeiro/repasses-creators')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})

    // primeiro filtro de campanha
    const filtroCampanhaCombo = page.getByRole('combobox').nth(1)
    await filtroCampanhaCombo.click()
    const campSearch34 = page.locator('input[placeholder="Buscar"]').first()
    await campSearch34.click()
    await campSearch34.pressSequentially(campaignName, { delay: 30 })
    await page.getByText('Buscando...').waitFor({ state: 'detached', timeout: 30_000 }).catch(() => {})
    const campOpt34 = page.getByRole('option', { name: new RegExp(campaignName, 'i') }).first()
    await campOpt34.waitFor({ state: 'visible', timeout: 30_000 })
    await campOpt34.click()
    await page.waitForLoadState('networkidle', { timeout: 10_000 }).catch(() => {})

    // abre Novo
    await page.getByRole('button', { name: /^Novo$/ }).click()
    const payModal = page.getByRole('dialog').filter({ hasText: /Novo pagamento ao (creator|influenciador)/i })
    await expect(payModal).toBeVisible({ timeout: 10_000 })

    // escolhe o creator vinculado
    const payField = (label: string) =>
      payModal.locator(`div.space-y-2:has(> label:text-is("${label}"))`).first()
    const creatorTrigger = payField('Creator vinculado à campanha').locator('button, [role="combobox"]').first()
    await creatorTrigger.click()
    await page.getByRole('option').first().click()

    // valor bruto
    await payField('Valor bruto (R$)').locator('input').first().fill('1500')

    await payModal.getByRole('button', { name: /^Salvar$/ }).first().click()
    await expect(payModal).toBeHidden({ timeout: 15_000 })

    // 5) gera token de portal pro creator (volta pra /creators)
    await page.goto('/creators')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})
    if (await pageSizeSelect.count()) await pageSizeSelect.selectOption('50').catch(() => {})
    const creatorRow = rowWithText(page, creatorName).first()
    await expect(creatorRow).toBeVisible({ timeout: 15_000 })
    await creatorRow.click()
    await page.getByRole('button', { name: /Links do portal/i }).first().click()
    const tokenModal = page.getByRole('dialog').filter({ hasText: /Links de acesso ao portal/i })
    await expect(tokenModal).toBeVisible({ timeout: 10_000 })

    const issuePromise = page.waitForResponse(
      (resp) => /\/api\/CreatorAccessTokens\/Issue/i.test(resp.url()) && resp.status() < 400,
      { timeout: 15_000 },
    )
    await tokenModal.getByRole('button', { name: /Gerar link/i }).first().click()
    const issueResp = await issuePromise
    const issueBody = await issueResp.json()
    const token: string | undefined = issueBody?.data?.token ?? issueBody?.token
    expect(token).toBeTruthy()

    // 6) abre portal e vai pra /pagamentos
    const origin = new URL(page.url()).origin
    const portalCtx = await browser.newContext()
    const portalPage = await portalCtx.newPage()
    await portalPage.goto(`${origin}/portal/${token}/pagamentos`)
    await portalPage.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})

    // valida que o portal publico carregou
    await expect(portal.page(portalPage)).toBeVisible({ timeout: 20_000 })

    // valida que o pagamento aparece
    await expect(portalPage.getByText(/Seus repasses/i)).toBeVisible({ timeout: 15_000 })
    await expect(portalPage.getByText(/R\$ 1\.?500/).first()).toBeVisible({ timeout: 10_000 })

    // 7) clica Anexar NF
    await portalPage.getByRole('button', { name: /Anexar NF/i }).first().click()

    // 8) preenche e submete
    await portalPage.getByPlaceholder(/Número da NF/i).fill('E2E-001')
    await portalPage.getByPlaceholder(/URL do PDF da NF/i).fill('https://example.com/nf-e2e.pdf')

    const uploadResp = portalPage.waitForResponse(
      (resp) => /\/api\/CreatorPortal\/.+\/invoice/i.test(resp.url()) && resp.status() < 400,
      { timeout: 15_000 },
    )
    await portalPage.getByRole('button', { name: /^Anexar$/ }).click()
    await uploadResp

    // 9) badge "NF anexada" aparece
    await expect(portalPage.getByText(/NF anexada/i).first()).toBeVisible({ timeout: 15_000 })

    await portalCtx.close()
  })
})
