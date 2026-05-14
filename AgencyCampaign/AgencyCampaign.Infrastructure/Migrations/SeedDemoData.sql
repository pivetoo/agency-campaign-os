-- ============================================================================
-- SeedDemoData.sql
-- Dados-demo complementares ao SeedData.sql para popular telas de prints.
--
-- Pré-requisitos:
--   1. SeedData.sql já rodou (brands 1-18, creators 1-15, campaigns 1-18,
--      opportunities 1-8, proposals 1-5, deliverables 1-7).
--   2. Migrations default já criaram: platform (1=IG, 2=TikTok, 3=YT, 4=Kwai,
--      5=X, 6=Other), deliverablekind (1=Reel, 2=Story, ...), campaigncreatorstatus
--      (1=Convidado, 2=Pendente, 3=Confirmado, 4=Em execução, 5=Entregue, 6=Cancelado),
--      financialaccount id=1 ("Caixa").
--
-- Como rodar:
--   psql -h <host> -U <user> -d <database-do-tenant> -f SeedDemoData.sql
--
-- Aviso: NÃO É IDEMPOTENTE. Rodar em banco de demo/staging, não em produção.
-- ============================================================================


-- ── CREATOR SOCIAL HANDLES (Instagram + TikTok para cada creator) ───────────
INSERT INTO creatorsocialhandle (creatorid, platformid, handle, profileurl, followers, engagementrate, isprimary, isactive, createdat, updatedat) VALUES
(1,  1, '@helprado',         'https://instagram.com/helprado',         284000, 3.85, true,  true, NOW(), NOW()),
(1,  2, '@helprado',          'https://tiktok.com/@helprado',          512000, 5.20, false, true, NOW(), NOW()),
(2,  1, '@arthurplay',       'https://instagram.com/arthurplay',       198000, 4.12, true,  true, NOW(), NOW()),
(2,  3, '@arthurplay',        'https://youtube.com/@arthurplay',       420000, 3.10, false, true, NOW(), NOW()),
(3,  1, '@biamoreira',       'https://instagram.com/biamoreira',       365000, 4.50, true,  true, NOW(), NOW()),
(3,  2, '@bia.moreira',       'https://tiktok.com/@bia.moreira',        88000, 6.80, false, true, NOW(), NOW()),
(4,  1, '@migstavares',      'https://instagram.com/migstavares',      142000, 5.05, true,  true, NOW(), NOW()),
(4,  2, '@migstavares',       'https://tiktok.com/@migstavares',       267000, 7.40, false, true, NOW(), NOW()),
(5,  1, '@clara.az',         'https://instagram.com/clara.az',         215000, 4.65, true,  true, NOW(), NOW()),
(5,  2, '@clara.az',          'https://tiktok.com/@clara.az',          340000, 6.10, false, true, NOW(), NOW()),
(6,  1, '@rafagoulart',      'https://instagram.com/rafagoulart',      178000, 3.95, true,  true, NOW(), NOW()),
(6,  3, '@rafagoulart',       'https://youtube.com/@rafagoulart',      305000, 4.20, false, true, NOW(), NOW()),
(7,  1, '@sofipeixoto',      'https://instagram.com/sofipeixoto',      256000, 4.85, true,  true, NOW(), NOW()),
(7,  2, '@sofipeixoto',       'https://tiktok.com/@sofipeixoto',       189000, 5.95, false, true, NOW(), NOW()),
(8,  1, '@lufarias',         'https://instagram.com/lufarias',         412000, 5.50, true,  true, NOW(), NOW()),
(8,  2, '@lufarias',          'https://tiktok.com/@lufarias',          780000, 8.20, false, true, NOW(), NOW()),
(9,  1, '@manucoelho',       'https://instagram.com/manucoelho',       168000, 4.30, true,  true, NOW(), NOW()),
(9,  2, '@manu.coelho',       'https://tiktok.com/@manu.coelho',       125000, 5.40, false, true, NOW(), NOW()),
(10, 1, '@gabenascimento',   'https://instagram.com/gabenascimento',   320000, 4.10, true,  true, NOW(), NOW()),
(10, 2, '@gabenascimento',    'https://tiktok.com/@gabenascimento',    485000, 6.30, false, true, NOW(), NOW()),
(11, 1, '@lauramendes',      'https://instagram.com/lauramendes',      245000, 4.95, true,  true, NOW(), NOW()),
(11, 2, '@lauramendes',       'https://tiktok.com/@lauramendes',       155000, 5.85, false, true, NOW(), NOW()),
(12, 1, '@pedroq',           'https://instagram.com/pedroq',           132000, 4.20, true,  true, NOW(), NOW()),
(12, 3, '@pedroqcanal',       'https://youtube.com/@pedroqcanal',      218000, 3.80, false, true, NOW(), NOW()),
(13, 1, '@isaviana',         'https://instagram.com/isaviana',         188000, 4.55, true,  true, NOW(), NOW()),
(13, 2, '@isaviana',          'https://tiktok.com/@isaviana',          234000, 6.05, false, true, NOW(), NOW()),
(14, 1, '@mathcardoso',      'https://instagram.com/mathcardoso',      295000, 4.75, true,  true, NOW(), NOW()),
(14, 2, '@mathcardoso',       'https://tiktok.com/@mathcardoso',       368000, 5.95, false, true, NOW(), NOW()),
(15, 1, '@anabe',            'https://instagram.com/anabe',            205000, 5.10, true,  true, NOW(), NOW()),
(15, 2, '@anabe',             'https://tiktok.com/@anabe',             162000, 6.50, false, true, NOW(), NOW());


-- ── CAMPAIGN CREATORS (creators vinculados às campanhas) ────────────────────
-- Status id: 3=Confirmado, 4=Em execução, 5=Entregue
INSERT INTO campaigncreator (campaignid, creatorid, campaigncreatorstatusid, agreedamount, agencyfeepercentage, agencyfeeamount, confirmedat, notes, createdat, updatedat) VALUES
(1,  4, 5, 18000.00, 20.00, 3600.00, NOW() - INTERVAL '12 days', 'Creator fitness, entregou Reel no prazo', NOW(), NOW()),
(1,  2, 5, 28000.00, 20.00, 5600.00, NOW() - INTERVAL '10 days', 'Review completo no YouTube', NOW(), NOW()),
(1, 10, 4, 22000.00, 18.00, 3960.00, NOW() - INTERVAL '8 days',  'Em produção, entrega 19/05', NOW(), NOW()),
(2,  5, 5, 15000.00, 20.00, 3000.00, NOW() - INTERVAL '15 days', 'Conteúdo de praia, alta engagement', NOW(), NOW()),
(2,  8, 5, 20000.00, 18.00, 3600.00, NOW() - INTERVAL '14 days', 'Trend de verão viralizou', NOW(), NOW()),
(2,  7, 4, 17000.00, 20.00, 3400.00, NOW() - INTERVAL '6 days',  'Aguardando aprovação da marca', NOW(), NOW()),
(3,  6, 4, 32000.00, 22.00, 7040.00, NOW() - INTERVAL '5 days',  'Review técnico do S25', NOW(), NOW()),
(3, 12, 3, 25000.00, 20.00, 5000.00, NOW() - INTERVAL '3 days',  'Comparativo com iPhone', NOW(), NOW()),
(4,  3, 4, 14000.00, 18.00, 2520.00, NOW() - INTERVAL '4 days',  'Mãe creator, storytelling emocional', NOW(), NOW()),
(4, 11, 3, 16000.00, 18.00, 2880.00, NOW() - INTERVAL '2 days',  'Confirmada após reunião de briefing', NOW(), NOW()),
(5,  6, 5, 24000.00, 22.00, 5280.00, NOW() - INTERVAL '20 days', 'Vídeo educativo já publicado', NOW(), NOW()),
(5, 13, 5, 18000.00, 20.00, 3600.00, NOW() - INTERVAL '18 days', 'Stories explicando pontos', NOW(), NOW()),
(7,  8, 3, 12000.00, 18.00, 2160.00, NOW() - INTERVAL '1 day',   'Aguardando início', NOW(), NOW()),
(7, 14, 3, 15000.00, 20.00, 3000.00, NOW() - INTERVAL '1 day',   'Confirmou pela manhã', NOW(), NOW());


-- ── FINANCIAL ACCOUNTS (contas bancárias extras) ────────────────────────────
INSERT INTO financialaccount (name, type, bank, agency, number, initialbalance, color, isactive, createdat, updatedat) VALUES
('Conta Corrente Itaú',     1, 'Itaú',     '0341', '12345-6',  85000.00, '#1F8A5B', true, NOW(), NOW()),
('Bradesco Empresarial',    1, 'Bradesco', '0237', '67890-1',  42000.00, '#E07A3D', true, NOW(), NOW()),
('Conta Pix Asaas',         1, 'Asaas',    '0001', '99887-7',  18000.00, '#00AEC7', true, NOW(), NOW());


-- ── FINANCIAL ENTRIES (recebíveis + pagáveis para os prints do Financeiro) ──
-- type: 1=Receivable, 2=Payable
-- status: 1=Pending, 2=Paid, 3=Overdue, 4=Cancelled
-- category: 1=BrandReceivable, 2=CreatorPayout, 3=AgencyFee, 4=Operational, 5=Bonus
-- accountid: 1=Caixa (default), 2=Itaú, 3=Bradesco, 4=Asaas

INSERT INTO financialentry (accountid, type, category, description, amount, dueat, occurredat, status, paidat, paymentmethod, referencecode, counterpartyname, notes, campaignid, campaigndeliverableid, createdat, updatedat) VALUES
-- Recebíveis pagos (campanhas já fechadas)
(2, 1, 1, 'Itaú Pontos Turbo - parcela 1/2',          100000.00, NOW() - INTERVAL '25 days', NOW() - INTERVAL '30 days', 2, NOW() - INTERVAL '22 days', 'PIX',     'PIX-ITAU-001',  'Itaú',         'Recebido via PIX',                          5,    NULL, NOW(), NOW()),
(2, 1, 1, 'Itaú Pontos Turbo - parcela 2/2',          100000.00, NOW() - INTERVAL '8 days',  NOW() - INTERVAL '15 days', 2, NOW() - INTERVAL '5 days',  'PIX',     'PIX-ITAU-002',  'Itaú',         'Saldo final da campanha',                   5,    NULL, NOW(), NOW()),
(2, 1, 1, 'Coca-Cola Verão - sinal 50%',                90000.00, NOW() - INTERVAL '40 days', NOW() - INTERVAL '45 days', 2, NOW() - INTERVAL '38 days', 'Boleto',  'BOL-CC-2025-A', 'Coca-Cola',    'Sinal de 50% pago',                          2,    NULL, NOW(), NOW()),

-- Recebíveis pendentes (a receber)
(2, 1, 1, 'Coca-Cola Verão - saldo 50%',                90000.00, NOW() + INTERVAL '12 days', NOW() - INTERVAL '5 days',  1, NULL,                       'Boleto',  'BOL-CC-2025-B', 'Coca-Cola',    'Saldo da campanha de verão',                 2,    NULL, NOW(), NOW()),
(2, 1, 1, 'Nike Air Max Day - sinal 40%',              100000.00, NOW() + INTERVAL '5 days',  NOW() - INTERVAL '8 days',  1, NULL,                       'PIX',     'PIX-NIKE-S',    'Nike Brasil',  'Sinal a receber',                            1,    NULL, NOW(), NOW()),
(2, 1, 1, 'Nike Air Max Day - saldo 60%',              150000.00, NOW() + INTERVAL '35 days', NOW() - INTERVAL '8 days',  1, NULL,                       'PIX',     'PIX-NIKE-F',    'Nike Brasil',  'Saldo após fim da campanha',                 1,    NULL, NOW(), NOW()),
(3, 1, 1, 'Samsung Galaxy S25 - parcela única',        320000.00, NOW() + INTERVAL '20 days', NOW() - INTERVAL '3 days',  1, NULL,                       'TED',     'TED-SAMS-001',  'Samsung',      'Pagamento à vista no fim',                   3,    NULL, NOW(), NOW()),
(2, 1, 1, 'Boticário Dia das Mães - sinal 30%',         45000.00, NOW() + INTERVAL '8 days',  NOW() - INTERVAL '2 days',  1, NULL,                       'Boleto',  'BOL-BOT-001',   'O Boticário',  'Sinal da campanha sazonal',                  4,    NULL, NOW(), NOW()),

-- Recebível vencido (overdue)
(2, 1, 1, 'Magalu Semana do Consumidor - retroativo',   60000.00, NOW() - INTERVAL '10 days', NOW() - INTERVAL '40 days', 3, NULL,                       'Boleto',  'BOL-MAGA-001',  'Magazine Luiza','Vencido — em cobrança jurídica',             7,    NULL, NOW(), NOW()),

-- Pagáveis aos creators (alguns pagos, alguns pendentes)
(4, 2, 2, 'Pagamento Helena Prado - Nike Air Max',      14400.00, NOW() - INTERVAL '5 days',  NOW() - INTERVAL '12 days', 2, NOW() - INTERVAL '3 days',  'PIX',     'PIX-CREATOR-01','Helena Prado', 'Pagamento via PIX após entrega aprovada',    1,    1,    NOW(), NOW()),
(4, 2, 2, 'Pagamento Arthur Pires - Nike Air Max',      22400.00, NOW() - INTERVAL '3 days',  NOW() - INTERVAL '10 days', 2, NOW() - INTERVAL '1 day',   'PIX',     'PIX-CREATOR-02','Arthur Pires', 'Pago após review no YouTube',                 1,    2,    NOW(), NOW()),
(4, 2, 2, 'Pagamento Miguel Tavares - Coca-Cola',       12000.00, NOW() - INTERVAL '20 days', NOW() - INTERVAL '25 days', 2, NOW() - INTERVAL '18 days', 'PIX',     'PIX-CREATOR-03','Miguel Tavares','Pagamento liberado após publicação',         2,    3,    NOW(), NOW()),
(4, 2, 2, 'Pagamento Clara Azevedo - Coca-Cola',         6400.00, NOW() + INTERVAL '3 days',  NOW() - INTERVAL '5 days',  1, NULL,                       'PIX',     NULL,             'Clara Azevedo','Aguardando aprovação da marca',              2,    4,    NOW(), NOW()),
(4, 2, 2, 'Pagamento Rafael Goulart - S25',             19200.00, NOW() - INTERVAL '1 day',   NOW() - INTERVAL '8 days',  2, NOW() - INTERVAL '1 day',   'PIX',     'PIX-CREATOR-04','Rafael Goulart','Pago após publicação no YouTube',            3,    5,    NOW(), NOW()),
(4, 2, 2, 'Pagamento Beatriz Moreira - Boticário',       9600.00, NOW() + INTERVAL '18 days', NOW() - INTERVAL '2 days',  1, NULL,                       'PIX',     NULL,             'Beatriz Moreira','Aguardando entrega',                       4,    6,    NOW(), NOW()),
(4, 2, 2, 'Pagamento Rafael Goulart - Itaú',            16000.00, NOW() - INTERVAL '15 days', NOW() - INTERVAL '20 days', 2, NOW() - INTERVAL '14 days', 'PIX',     'PIX-CREATOR-05','Rafael Goulart','Pago após Itaú Pontos',                       5,    7,    NOW(), NOW()),

-- Custos operacionais
(1, 2, 4, 'Licença Adobe Creative Cloud',                  299.00, NOW() + INTERVAL '8 days',  NOW() - INTERVAL '5 days',  1, NULL,                       'Cartão',  NULL,             'Adobe Brasil', 'Mensal recorrente',                          NULL, NULL, NOW(), NOW()),
(1, 2, 4, 'Anúncio Meta Ads - boost campanha Nike',       1500.00, NOW() - INTERVAL '2 days',  NOW() - INTERVAL '10 days', 2, NOW() - INTERVAL '2 days',  'Cartão',  'META-CC-001',   'Meta',         'Boost de posts',                              1,    NULL, NOW(), NOW()),

-- Fee de agência (recebido)
(2, 1, 3, 'Agency Fee - Itaú Pontos Turbo',              40000.00, NOW() - INTERVAL '6 days',  NOW() - INTERVAL '20 days', 2, NOW() - INTERVAL '5 days',  'PIX',     'PIX-FEE-001',   'Itaú',         'Fee 20% sobre 200k',                          5,    NULL, NOW(), NOW());


-- ── AUTOMATIONS (5 automações usando triggers reais do sistema) ─────────────
-- ATENÇÃO: connectorid e pipelineid abaixo apontam para o IntegrationPlataform.
-- Se não houver conectores cadastrados, as automações disparam mas falham na execução
-- (o que é desejado para popular AutomationExecutionLog com falhas também).
INSERT INTO automation (name, trigger, triggercondition, connectorid, pipelineid, variablemappingjson, isactive, createdat, updatedat) VALUES
('Notificar time quando proposta é aprovada',          'proposal_approved',          NULL,                            1, 1, '{"channel":"#comercial","brand":"{{Brand.Name}}","value":"{{Proposal.TotalValue}}"}', true,  NOW() - INTERVAL '30 days', NOW()),
('Enviar contrato Zapsign após proposta aprovada',     'proposal_approved',          NULL,                            1, 2, '{"document_template":"contrato-campanha","brand_email":"{{Brand.ContactEmail}}"}',     true,  NOW() - INTERVAL '25 days', NOW()),
('Alerta de follow-up atrasado',                        'follow_up_overdue',          NULL,                            1, 3, '{"to":"{{Owner.Email}}","opportunity":"{{Opportunity.Name}}"}',                         true,  NOW() - INTERVAL '20 days', NOW()),
('Notificar creator quando entrega aprovada',          'deliverable_brand_approved', NULL,                            1, 4, '{"to":"{{Creator.Phone}}","campaign":"{{Campaign.Name}}"}',                              true,  NOW() - INTERVAL '15 days', NOW()),
('Cobrar marca quando recebível vence',                'financial_overdue',          'amount > 10000',                1, 5, '{"to":"{{Brand.ContactEmail}}","amount":"{{Entry.Amount}}","reference":"{{Entry.ReferenceCode}}"}', true, NOW() - INTERVAL '10 days', NOW()),
('Avisar finance quando recebível é criado',           'financial_receivable_created', NULL,                          1, 6, '{"channel":"#financeiro","amount":"{{Entry.Amount}}","brand":"{{Counterparty.Name}}"}', false, NOW() - INTERVAL '5 days',  NOW());


-- ── AUTOMATION EXECUTION LOGS (mistura de sucesso e falha) ──────────────────
INSERT INTO automationexecutionlog (automationid, automationname, trigger, succeeded, renderedpayload, errormessage, createdat) VALUES
(1, 'Notificar time quando proposta é aprovada',      'proposal_approved',          true,
   '{"channel":"#comercial","brand":"Itaú","value":"200000.00"}',
   NULL, NOW() - INTERVAL '20 days'),
(1, 'Notificar time quando proposta é aprovada',      'proposal_approved',          true,
   '{"channel":"#comercial","brand":"O Boticário","value":"150000.00"}',
   NULL, NOW() - INTERVAL '4 days'),
(2, 'Enviar contrato Zapsign após proposta aprovada', 'proposal_approved',          true,
   '{"document_template":"contrato-campanha","brand_email":"ana.paula@oboticario.com.br"}',
   NULL, NOW() - INTERVAL '4 days'),
(2, 'Enviar contrato Zapsign após proposta aprovada', 'proposal_approved',          false,
   '{"document_template":"contrato-campanha","brand_email":"carlos.eduardo@itau.com.br"}',
   'Pipeline retornou 401 — token Zapsign expirado',
   NOW() - INTERVAL '20 days'),
(3, 'Alerta de follow-up atrasado',                    'follow_up_overdue',          true,
   '{"to":"bruno.costa@agency.com","opportunity":"Coca-Cola - Campanha Verão"}',
   NULL, NOW() - INTERVAL '6 days'),
(3, 'Alerta de follow-up atrasado',                    'follow_up_overdue',          true,
   '{"to":"carla.mendes@agency.com","opportunity":"Samsung - Galaxy S25"}',
   NULL, NOW() - INTERVAL '3 days'),
(4, 'Notificar creator quando entrega aprovada',       'deliverable_brand_approved', true,
   '{"to":"+5511911230001","campaign":"Nike Air Max Day 2025"}',
   NULL, NOW() - INTERVAL '12 days'),
(4, 'Notificar creator quando entrega aprovada',       'deliverable_brand_approved', false,
   '{"to":"+5521922340002","campaign":"Nike Air Max Day 2025"}',
   'Conector WhatsApp não configurado para esta agência',
   NOW() - INTERVAL '10 days'),
(5, 'Cobrar marca quando recebível vence',             'financial_overdue',          true,
   '{"to":"ricardo.gomes@magazineluiza.com.br","amount":"60000.00","reference":"BOL-MAGA-001"}',
   NULL, NOW() - INTERVAL '2 days'),
(5, 'Cobrar marca quando recebível vence',             'financial_overdue',          false,
   '{"to":"ricardo.gomes@magazineluiza.com.br","amount":"60000.00","reference":"BOL-MAGA-001"}',
   'Template de e-mail não encontrado no Zapsign',
   NOW() - INTERVAL '1 day');


-- ── Verificação rápida ─────────────────────────────────────────────────────
SELECT 'creatorsocialhandle'   AS table_name, COUNT(*) AS count FROM creatorsocialhandle
UNION ALL SELECT 'campaigncreator',          COUNT(*) FROM campaigncreator
UNION ALL SELECT 'financialaccount',         COUNT(*) FROM financialaccount
UNION ALL SELECT 'financialentry',           COUNT(*) FROM financialentry
UNION ALL SELECT 'automation',               COUNT(*) FROM automation
UNION ALL SELECT 'automationexecutionlog',   COUNT(*) FROM automationexecutionlog;
