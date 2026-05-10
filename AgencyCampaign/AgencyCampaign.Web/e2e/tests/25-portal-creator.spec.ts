import { test, expect } from '../fixtures/test'

// Spec 25: Portal do Creator
// 1) cria creator novo via /creators
// 2) abre modal "Links do portal" e gera token
// 3) abre /portal/:token em contexto SEM auth
// 4) valida layout (header, bottom nav)
// 5) percorre as 4 abas (Campanhas, Contratos, Pagamentos, Perfil)
// 6) atualiza dados de PIX no Perfil
// 7) garante que token invalido cai na tela "Acesso nao autorizado"

test.describe('Portal do Creator - link de acesso publico', () => {
  test('gera token, acessa portal sem auth e atualiza PIX', async ({ page, browser, expectNoApiFailures }) => {
    const stamp = Date.now()
    const creatorName = `E2E PortalCreator ${stamp}`

    // 1) cria creator
    await page.goto('/creators')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})

    await page.getByRole('button', { name: /^Incluir$|^Novo$|^Nova$/i }).first().click()

    const formModal = page.getByRole('dialog').filter({ hasText: /Novo influenciador|Novo creator/i })
    await expect(formModal).toBeVisible({ timeout: 10_000 })
    await formModal.locator('input[type="text"], input:not([type])').first().fill(creatorName)
    await formModal.getByRole('button', { name: /^Salvar$/i }).first().click()
    await expect(formModal).toBeHidden({ timeout: 15_000 })

    // 2) seleciona a linha do creator recem criado
    const pageSizeSelect = page.locator('select').filter({ hasText: /5|10|20|50/ }).first()
    if (await pageSizeSelect.count()) await pageSizeSelect.selectOption('50').catch(() => {})

    const row = page.locator('[data-row="true"]', { hasText: creatorName }).first()
    await expect(row).toBeVisible({ timeout: 15_000 })
    await row.click()

    // 3) abre modal de links do portal
    await page.getByRole('button', { name: /Links do portal/i }).first().click()
    const tokenModal = page.getByRole('dialog').filter({ hasText: /Links de acesso ao portal/i })
    await expect(tokenModal).toBeVisible({ timeout: 10_000 })

    // 4) gera novo token e captura via response
    const issuePromise = page.waitForResponse(
      (resp) => /\/api\/CreatorAccessTokens\/Issue/i.test(resp.url()) && resp.status() < 400,
      { timeout: 15_000 },
    )
    await tokenModal.getByRole('button', { name: /Gerar link/i }).first().click()
    const issueResponse = await issuePromise
    const issueBody = await issueResponse.json()
    const token: string | undefined = issueBody?.data?.token ?? issueBody?.token
    expect(token, `token nao veio na response: ${JSON.stringify(issueBody)}`).toBeTruthy()

    // 5) deriva URL publica do portal
    const origin = new URL(page.url()).origin
    const portalUrl = `${origin}/portal/${token}`

    // 6) abre o portal em contexto NOVO (sem cookies/auth)
    const publicContext = await browser.newContext()
    const publicPage = await publicContext.newPage()

    // captura erros de API tambem na sessao publica
    const portalApiFailures: { url: string; status: number }[] = []
    publicPage.on('response', (resp) => {
      const status = resp.status()
      const url = resp.url()
      if (status >= 400 && /\/api\/CreatorPortal\//.test(url)) {
        portalApiFailures.push({ url, status })
      }
    })

    await publicPage.goto(portalUrl)
    await publicPage.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})

    // 7) layout: saudacao + 5 nav items
    await expect(publicPage.getByRole('heading', { name: new RegExp(`Olá, ${creatorName}`, 'i') })).toBeVisible({
      timeout: 15_000,
    })
    await expect(publicPage.getByText('Portal do creator', { exact: false }).first()).toBeVisible()
    for (const label of ['Início', 'Campanhas', 'Contratos', 'Pagamentos', 'Perfil']) {
      await expect(publicPage.getByRole('link', { name: label }).first()).toBeVisible()
    }

    // 8) dashboard: 3 SummaryCards e alerta de PIX faltando
    await expect(publicPage.getByText('Cadastre sua chave PIX').first()).toBeVisible({ timeout: 10_000 })
    for (const label of ['Campanhas', 'Contratos pendentes', 'Repasses pendentes']) {
      await expect(publicPage.getByText(label, { exact: false }).first()).toBeVisible()
    }

    // 9) Campanhas — empty state
    await publicPage.getByRole('link', { name: 'Campanhas' }).first().click()
    await publicPage.waitForURL(/\/portal\/.+\/campanhas/, { timeout: 10_000 })
    await expect(publicPage.getByText('Nenhuma campanha ainda.')).toBeVisible({ timeout: 10_000 })

    // 10) Contratos — empty state
    await publicPage.getByRole('link', { name: 'Contratos' }).first().click()
    await publicPage.waitForURL(/\/portal\/.+\/contratos/, { timeout: 10_000 })
    await expect(publicPage.getByText('Nenhum contrato disponível.')).toBeVisible({ timeout: 10_000 })

    // 11) Pagamentos — empty state
    await publicPage.getByRole('link', { name: 'Pagamentos' }).first().click()
    await publicPage.waitForURL(/\/portal\/.+\/pagamentos/, { timeout: 10_000 })
    await expect(publicPage.getByText('Nenhum pagamento ainda.')).toBeVisible({ timeout: 10_000 })

    // 12) Perfil — atualiza PIX
    await publicPage.getByRole('link', { name: 'Perfil' }).first().click()
    await publicPage.waitForURL(/\/portal\/.+\/perfil/, { timeout: 10_000 })
    await expect(publicPage.getByRole('heading', { name: /Dados de pagamento/i })).toBeVisible({ timeout: 10_000 })

    // tipo da chave PIX = CPF
    await publicPage.getByRole('combobox').first().click()
    await publicPage.getByRole('option', { name: /^CPF$/ }).first().click()

    // chave PIX
    await publicPage.getByPlaceholder(/Digite sua chave/i).fill('12345678900')

    // CPF/CNPJ titular
    await publicPage.getByPlaceholder(/Apenas n[uú]meros/i).fill('12345678900')

    const updateBankPromise = publicPage.waitForResponse(
      (resp) => /\/api\/CreatorPortal\/.+\/bank-info/i.test(resp.url()) && resp.status() < 400,
      { timeout: 15_000 },
    )
    await publicPage.getByRole('button', { name: /^Salvar$/ }).click()
    const updateBankResp = await updateBankPromise
    expect(updateBankResp.status()).toBeLessThan(400)

    expect(portalApiFailures, `falhas em /api/CreatorPortal: ${JSON.stringify(portalApiFailures)}`).toHaveLength(0)

    await publicContext.close()

    // 13) token invalido cai em "Acesso nao autorizado"
    const invalidContext = await browser.newContext()
    const invalidPage = await invalidContext.newPage()
    await invalidPage.goto(`${origin}/portal/invalid-token-${stamp}`)
    await expect(invalidPage.getByText(/Acesso n[ãa]o autorizado/i)).toBeVisible({ timeout: 15_000 })
    await invalidContext.close()

    expectNoApiFailures()
  })
})
