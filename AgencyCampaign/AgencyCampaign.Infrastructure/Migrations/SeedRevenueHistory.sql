-- ============================================================================
-- SeedRevenueHistory.sql
-- Histórico de receita dos últimos 12 meses para alimentar o dashboard.
-- Insere ~36 lançamentos financeiros (receivables pagos) distribuídos
-- pelos 12 meses anteriores, usando as marcas reais do tenant demo.
--
-- Idempotente: detecta pelos referencecode iniciados com 'HIST-' e pula se já existir.
-- ============================================================================

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM financialentry WHERE referencecode LIKE 'HIST-%') THEN
        RAISE NOTICE 'Histórico já populado, abortando.';
        RETURN;
    END IF;

    INSERT INTO financialentry (accountid, type, category, description, amount, dueat, occurredat, status, paidat, paymentmethod, referencecode, counterpartyname, notes, createdat, updatedat) VALUES
    -- ── 12 meses atrás
    (2, 1, 1, 'Skala Verão - parcela única',           120000.00, NOW() - INTERVAL '12 months' + INTERVAL '5 days',  NOW() - INTERVAL '12 months',           2, NOW() - INTERVAL '12 months' + INTERVAL '5 days',  'PIX',    'HIST-M12-01', 'Skala',           'Campanha sazonal verão',         NOW() - INTERVAL '12 months', NOW()),
    (2, 1, 1, 'Magalu Black Friday - sinal',            85000.00, NOW() - INTERVAL '12 months' + INTERVAL '10 days', NOW() - INTERVAL '12 months',           2, NOW() - INTERVAL '12 months' + INTERVAL '10 days', 'Boleto', 'HIST-M12-02', 'Magazine Luiza',  'Sinal de campanha',              NOW() - INTERVAL '12 months', NOW()),

    -- ── 11 meses atrás
    (2, 1, 1, 'Itaú Educação Financeira',                95000.00, NOW() - INTERVAL '11 months' + INTERVAL '3 days',  NOW() - INTERVAL '11 months',           2, NOW() - INTERVAL '11 months' + INTERVAL '3 days',  'PIX',    'HIST-M11-01', 'Itaú',            'Conteúdo educativo',             NOW() - INTERVAL '11 months', NOW()),
    (3, 1, 1, 'Natura Lançamento Tododia',               72000.00, NOW() - INTERVAL '11 months' + INTERVAL '15 days', NOW() - INTERVAL '11 months',           2, NOW() - INTERVAL '11 months' + INTERVAL '15 days', 'PIX',    'HIST-M11-02', 'Natura',          'Lançamento de linha',            NOW() - INTERVAL '11 months', NOW()),
    (2, 1, 1, 'Granado Coleção Especial',                58000.00, NOW() - INTERVAL '11 months' + INTERVAL '22 days', NOW() - INTERVAL '11 months',           2, NOW() - INTERVAL '11 months' + INTERVAL '22 days', 'Boleto', 'HIST-M11-03', 'Granado',         'Coleção limitada',               NOW() - INTERVAL '11 months', NOW()),

    -- ── 10 meses atrás
    (2, 1, 1, 'Magalu Aniversário',                     145000.00, NOW() - INTERVAL '10 months' + INTERVAL '5 days',  NOW() - INTERVAL '10 months',           2, NOW() - INTERVAL '10 months' + INTERVAL '5 days',  'Boleto', 'HIST-M10-01', 'Magazine Luiza',  'Mês de aniversário',             NOW() - INTERVAL '10 months', NOW()),
    (2, 1, 1, 'Avon Beleza Brasileira',                  68000.00, NOW() - INTERVAL '10 months' + INTERVAL '12 days', NOW() - INTERVAL '10 months',           2, NOW() - INTERVAL '10 months' + INTERVAL '12 days', 'PIX',    'HIST-M10-02', 'Avon',            'Campanha institucional',         NOW() - INTERVAL '10 months', NOW()),
    (3, 1, 1, 'Lapinha Detox Janeiro',                   45000.00, NOW() - INTERVAL '10 months' + INTERVAL '20 days', NOW() - INTERVAL '10 months',           2, NOW() - INTERVAL '10 months' + INTERVAL '20 days', 'PIX',    'HIST-M10-03', 'Lapinha',         'Retiro de janeiro',              NOW() - INTERVAL '10 months', NOW()),

    -- ── 9 meses atrás
    (2, 1, 1, 'Itaú Cartões Premium',                   110000.00, NOW() - INTERVAL '9 months'  + INTERVAL '8 days',  NOW() - INTERVAL '9 months',            2, NOW() - INTERVAL '9 months'  + INTERVAL '8 days',  'PIX',    'HIST-M09-01', 'Itaú',            'Campanha de cartões',            NOW() - INTERVAL '9 months',  NOW()),
    (2, 1, 1, 'O Boticário Coleção Outono',              92000.00, NOW() - INTERVAL '9 months'  + INTERVAL '14 days', NOW() - INTERVAL '9 months',            2, NOW() - INTERVAL '9 months'  + INTERVAL '14 days', 'Boleto', 'HIST-M09-02', 'O Boticário',     'Coleção sazonal',                NOW() - INTERVAL '9 months',  NOW()),
    (2, 1, 1, 'Skala Hidratação Profunda',               64000.00, NOW() - INTERVAL '9 months'  + INTERVAL '25 days', NOW() - INTERVAL '9 months',            2, NOW() - INTERVAL '9 months'  + INTERVAL '25 days', 'PIX',    'HIST-M09-03', 'Skala',           'Linha tratamento',               NOW() - INTERVAL '9 months',  NOW()),

    -- ── 8 meses atrás
    (3, 1, 1, 'Natura Sustentabilidade',                 88000.00, NOW() - INTERVAL '8 months'  + INTERVAL '3 days',  NOW() - INTERVAL '8 months',            2, NOW() - INTERVAL '8 months'  + INTERVAL '3 days',  'PIX',    'HIST-M08-01', 'Natura',          'Campanha institucional',         NOW() - INTERVAL '8 months',  NOW()),
    (2, 1, 1, 'Magalu Educação Tech',                    78000.00, NOW() - INTERVAL '8 months'  + INTERVAL '10 days', NOW() - INTERVAL '8 months',            2, NOW() - INTERVAL '8 months'  + INTERVAL '10 days', 'Boleto', 'HIST-M08-02', 'Magazine Luiza',  'Curso digital',                  NOW() - INTERVAL '8 months',  NOW()),
    (2, 1, 1, 'Granado Brasil Original',                 56000.00, NOW() - INTERVAL '8 months'  + INTERVAL '21 days', NOW() - INTERVAL '8 months',            2, NOW() - INTERVAL '8 months'  + INTERVAL '21 days', 'PIX',    'HIST-M08-03', 'Granado',         'Linha tradicional',              NOW() - INTERVAL '8 months',  NOW()),

    -- ── 7 meses atrás
    (2, 1, 1, 'Itaú Pix Empresa',                       125000.00, NOW() - INTERVAL '7 months'  + INTERVAL '7 days',  NOW() - INTERVAL '7 months',            2, NOW() - INTERVAL '7 months'  + INTERVAL '7 days',  'PIX',    'HIST-M07-01', 'Itaú',            'Conteúdo B2B',                   NOW() - INTERVAL '7 months',  NOW()),
    (2, 1, 1, 'O Boticário Coleção Inverno',             82000.00, NOW() - INTERVAL '7 months'  + INTERVAL '15 days', NOW() - INTERVAL '7 months',            2, NOW() - INTERVAL '7 months'  + INTERVAL '15 days', 'PIX',    'HIST-M07-02', 'O Boticário',     'Coleção sazonal',                NOW() - INTERVAL '7 months',  NOW()),
    (3, 1, 1, 'Avon Marca Bonita',                       64000.00, NOW() - INTERVAL '7 months'  + INTERVAL '24 days', NOW() - INTERVAL '7 months',            2, NOW() - INTERVAL '7 months'  + INTERVAL '24 days', 'Boleto', 'HIST-M07-03', 'Avon',            'Reposicionamento de marca',      NOW() - INTERVAL '7 months',  NOW()),

    -- ── 6 meses atrás
    (2, 1, 1, 'Skala Cabelos Cacheados',                 76000.00, NOW() - INTERVAL '6 months'  + INTERVAL '5 days',  NOW() - INTERVAL '6 months',            2, NOW() - INTERVAL '6 months'  + INTERVAL '5 days',  'PIX',    'HIST-M06-01', 'Skala',           'Linha curly hair',               NOW() - INTERVAL '6 months',  NOW()),
    (2, 1, 1, 'Itaú Investimentos',                     105000.00, NOW() - INTERVAL '6 months'  + INTERVAL '12 days', NOW() - INTERVAL '6 months',            2, NOW() - INTERVAL '6 months'  + INTERVAL '12 days', 'PIX',    'HIST-M06-02', 'Itaú',            'Educação financeira',            NOW() - INTERVAL '6 months',  NOW()),
    (3, 1, 1, 'Lapinha Verão Wellness',                  52000.00, NOW() - INTERVAL '6 months'  + INTERVAL '22 days', NOW() - INTERVAL '6 months',            2, NOW() - INTERVAL '6 months'  + INTERVAL '22 days', 'PIX',    'HIST-M06-03', 'Lapinha',         'Retiro de verão',                NOW() - INTERVAL '6 months',  NOW()),

    -- ── 5 meses atrás
    (2, 1, 1, 'Magalu Volta às Aulas',                   98000.00, NOW() - INTERVAL '5 months'  + INTERVAL '4 days',  NOW() - INTERVAL '5 months',            2, NOW() - INTERVAL '5 months'  + INTERVAL '4 days',  'Boleto', 'HIST-M05-01', 'Magazine Luiza',  'Campanha sazonal',               NOW() - INTERVAL '5 months',  NOW()),
    (2, 1, 1, 'Natura Ekos',                             84000.00, NOW() - INTERVAL '5 months'  + INTERVAL '13 days', NOW() - INTERVAL '5 months',            2, NOW() - INTERVAL '5 months'  + INTERVAL '13 days', 'PIX',    'HIST-M05-02', 'Natura',          'Linha Ekos',                     NOW() - INTERVAL '5 months',  NOW()),
    (2, 1, 1, 'Granado Pet',                             48000.00, NOW() - INTERVAL '5 months'  + INTERVAL '23 days', NOW() - INTERVAL '5 months',            2, NOW() - INTERVAL '5 months'  + INTERVAL '23 days', 'PIX',    'HIST-M05-03', 'Granado',         'Linha pet',                      NOW() - INTERVAL '5 months',  NOW()),

    -- ── 4 meses atrás
    (2, 1, 1, 'O Boticário Namorados',                  115000.00, NOW() - INTERVAL '4 months'  + INTERVAL '6 days',  NOW() - INTERVAL '4 months',            2, NOW() - INTERVAL '4 months'  + INTERVAL '6 days',  'PIX',    'HIST-M04-01', 'O Boticário',     'Dia dos Namorados',              NOW() - INTERVAL '4 months',  NOW()),
    (2, 1, 1, 'Itaú Cashback',                           88000.00, NOW() - INTERVAL '4 months'  + INTERVAL '14 days', NOW() - INTERVAL '4 months',            2, NOW() - INTERVAL '4 months'  + INTERVAL '14 days', 'PIX',    'HIST-M04-02', 'Itaú',            'Programa cashback',              NOW() - INTERVAL '4 months',  NOW()),
    (3, 1, 1, 'Avon Skincare',                           71000.00, NOW() - INTERVAL '4 months'  + INTERVAL '22 days', NOW() - INTERVAL '4 months',            2, NOW() - INTERVAL '4 months'  + INTERVAL '22 days', 'Boleto', 'HIST-M04-03', 'Avon',            'Lançamento skincare',            NOW() - INTERVAL '4 months',  NOW()),

    -- ── 3 meses atrás
    (2, 1, 1, 'Skala Reparação Total',                   89000.00, NOW() - INTERVAL '3 months'  + INTERVAL '5 days',  NOW() - INTERVAL '3 months',            2, NOW() - INTERVAL '3 months'  + INTERVAL '5 days',  'PIX',    'HIST-M03-01', 'Skala',           'Linha reparadora',               NOW() - INTERVAL '3 months',  NOW()),
    (2, 1, 1, 'Magalu Tech Week',                       132000.00, NOW() - INTERVAL '3 months'  + INTERVAL '12 days', NOW() - INTERVAL '3 months',            2, NOW() - INTERVAL '3 months'  + INTERVAL '12 days', 'Boleto', 'HIST-M03-02', 'Magazine Luiza',  'Semana de tecnologia',           NOW() - INTERVAL '3 months',  NOW()),
    (2, 1, 1, 'Natura Mamãe & Bebê',                     67000.00, NOW() - INTERVAL '3 months'  + INTERVAL '20 days', NOW() - INTERVAL '3 months',            2, NOW() - INTERVAL '3 months'  + INTERVAL '20 days', 'PIX',    'HIST-M03-03', 'Natura',          'Linha infantil',                 NOW() - INTERVAL '3 months',  NOW()),

    -- ── 2 meses atrás
    (2, 1, 1, 'Itaú Personnalité',                      155000.00, NOW() - INTERVAL '2 months'  + INTERVAL '7 days',  NOW() - INTERVAL '2 months',            2, NOW() - INTERVAL '2 months'  + INTERVAL '7 days',  'PIX',    'HIST-M02-01', 'Itaú',            'Segmento alta renda',            NOW() - INTERVAL '2 months',  NOW()),
    (2, 1, 1, 'O Boticário Coleção Primavera',           94000.00, NOW() - INTERVAL '2 months'  + INTERVAL '15 days', NOW() - INTERVAL '2 months',            2, NOW() - INTERVAL '2 months'  + INTERVAL '15 days', 'PIX',    'HIST-M02-02', 'O Boticário',     'Coleção sazonal',                NOW() - INTERVAL '2 months',  NOW()),
    (3, 1, 1, 'Granado Bebê',                            58000.00, NOW() - INTERVAL '2 months'  + INTERVAL '24 days', NOW() - INTERVAL '2 months',            2, NOW() - INTERVAL '2 months'  + INTERVAL '24 days', 'Boleto', 'HIST-M02-03', 'Granado',         'Linha infantil',                 NOW() - INTERVAL '2 months',  NOW()),

    -- ── 1 mês atrás
    (2, 1, 1, 'Skala Cabelos Lisos',                     72000.00, NOW() - INTERVAL '1 month'   + INTERVAL '6 days',  NOW() - INTERVAL '1 month',             2, NOW() - INTERVAL '1 month'   + INTERVAL '6 days',  'PIX',    'HIST-M01-01', 'Skala',           'Linha alisamento',               NOW() - INTERVAL '1 month',   NOW()),
    (2, 1, 1, 'Avon Empoderamento',                      78000.00, NOW() - INTERVAL '1 month'   + INTERVAL '13 days', NOW() - INTERVAL '1 month',             2, NOW() - INTERVAL '1 month'   + INTERVAL '13 days', 'PIX',    'HIST-M01-02', 'Avon',            'Campanha institucional',         NOW() - INTERVAL '1 month',   NOW()),
    (3, 1, 1, 'Lapinha Outono Wellness',                 48000.00, NOW() - INTERVAL '1 month'   + INTERVAL '22 days', NOW() - INTERVAL '1 month',             2, NOW() - INTERVAL '1 month'   + INTERVAL '22 days', 'PIX',    'HIST-M01-03', 'Lapinha',         'Retiro outono',                  NOW() - INTERVAL '1 month',   NOW());

    RAISE NOTICE 'Histórico de receita populado.';
END $$;

-- Resumo por mês
SELECT
    TO_CHAR(paidat, 'YYYY-MM') AS mes,
    COUNT(*) AS lancamentos,
    SUM(amount) AS receita
FROM financialentry
WHERE type = 1 AND status = 2 AND paidat IS NOT NULL
GROUP BY TO_CHAR(paidat, 'YYYY-MM')
ORDER BY mes;
