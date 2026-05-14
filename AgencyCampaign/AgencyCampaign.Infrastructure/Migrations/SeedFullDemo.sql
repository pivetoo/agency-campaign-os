-- ============================================================================
-- SeedFullDemo.sql
-- Seed completo para tenant DEMO. Limpa e repopula tudo.
-- Usa CTEs (WITH ... RETURNING) para encadear FKs sem assumir IDs.
--
-- Rodar em banco demo apenas:
--   psql -h <host> -U appdatabase -d agencycampaign -f SeedFullDemo.sql
-- ============================================================================

BEGIN;

-- ── LIMPEZA TOTAL ───────────────────────────────────────────────────────────
TRUNCATE TABLE
    automationexecutionlog,
    automation,
    creatorpayment,
    deliverableapproval,
    deliverablesharelink,
    financialentry,
    financialaccount,
    campaigndocument,
    campaigncreatorstatushistory,
    campaigndeliverable,
    campaigncreator,
    campaignstatushistory,
    campaign,
    proposalsharelink,
    proposalstatushistory,
    proposalversion,
    proposalitem,
    proposal,
    opportunitycomment,
    opportunityfollowup,
    opportunitynegotiation,
    opportunitystagehistory,
    opportunitytagassignment,
    opportunity,
    creatoraccesstoken,
    creatorsocialhandle,
    creator,
    brand
RESTART IDENTITY CASCADE;

-- Defaults que normalmente vêm de migrations (idempotente: só insere se vazio)
INSERT INTO campaigncreatorstatus (name, description, displayorder, color, isinitial, isfinal, category, isactive, createdat, updatedat)
SELECT * FROM (VALUES
    ('Convidado',            'Creator foi convidado para a campanha',     1, '#6b7280', true,  false, 0, true, NOW(), NOW()),
    ('Pendente de aprovação','Aguardando aprovação interna ou do cliente', 2, '#0ea5e9', false, false, 0, true, NOW(), NOW()),
    ('Confirmado',           'Creator aceitou participar',                 3, '#10b981', false, false, 0, true, NOW(), NOW()),
    ('Em execução',          'Creator produzindo o conteúdo',              4, '#f59e0b', false, false, 0, true, NOW(), NOW()),
    ('Entregue',             'Conteúdo entregue e publicado',              5, '#8b5cf6', false, true,  1, true, NOW(), NOW()),
    ('Cancelado',            'Creator removido da campanha',               6, '#ef4444', false, true,  2, true, NOW(), NOW())
) AS v(name, description, displayorder, color, isinitial, isfinal, category, isactive, createdat, updatedat)
WHERE NOT EXISTS (SELECT 1 FROM campaigncreatorstatus);

INSERT INTO deliverablekind (name, isactive, displayorder, createdat)
SELECT * FROM (VALUES
    ('Reel',      true, 1, NOW()),
    ('Story',     true, 2, NOW()),
    ('Feed Post', true, 3, NOW()),
    ('Video',     true, 4, NOW()),
    ('Live',      true, 5, NOW()),
    ('Combo',     true, 6, NOW()),
    ('Other',     true, 7, NOW())
) AS v(name, isactive, displayorder, createdat)
WHERE NOT EXISTS (SELECT 1 FROM deliverablekind);

-- Garante a conta padrão "Caixa"
INSERT INTO financialaccount (name, type, initialbalance, color, isactive, createdat, updatedat)
VALUES ('Caixa', 2, 0, '#6366f1', true, NOW(), NOW());


-- ── BRANDS ──────────────────────────────────────────────────────────────────
INSERT INTO brand (name, tradename, document, contactname, contactemail, notes, isactive, createdat, updatedat) VALUES
('Skala Cosméticos S.A.',  'Skala',    '11.111.111/0001-11', 'Mariana Lopes',  'mariana@skala.com.br',      'Cliente prioritário, alto LTV',     true, NOW(), NOW()),
('Natura Cosméticos S.A.', 'Natura',   '22.222.222/0001-22', 'Eduarda Lopes',  'eduarda.lopes@natura.net',  'Marca sustentável, foco institucional', true, NOW(), NOW()),
('Avon Industrial Ltda',   'Avon',     '33.333.333/0001-33', 'Patrícia Reis',  'patricia.reis@avon.com',    'Time de marketing recém-renovado',  true, NOW(), NOW()),
('Lapinha SPA',            'Lapinha',  '44.444.444/0001-44', 'Roberto Alves',  'roberto@lapinha.com.br',    'Lifestyle, wellness',                true, NOW(), NOW()),
('Granado',                'Granado',  '55.555.555/0001-55', 'Helena Garcia',  'helena@granado.com.br',     'Marca tradicional, ticket médio alto', true, NOW(), NOW()),
('O Boticário',            'Boticário','66.666.666/0001-66', 'Ana Paula',      'ana.paula@oboticario.com.br','Sazonal forte: Dia das Mães, Namorados', true, NOW(), NOW()),
('Itaú Unibanco',          'Itaú',     '77.777.777/0001-77', 'Carlos Eduardo', 'carlos.eduardo@itau.com.br','Banco com forte presença digital',  true, NOW(), NOW()),
('Magazine Luiza',         'Magalu',   '88.888.888/0001-88', 'Ricardo Gomes',  'ricardo@magazineluiza.com.br','Varejo omnichannel',               true, NOW(), NOW());


-- ── CREATORS ────────────────────────────────────────────────────────────────
INSERT INTO creator (name, stagename, email, phone, document, pixkey, primaryniche, city, state, notes, defaultagencyfeepercent, isactive, createdat, updatedat) VALUES
('Helena Prado',       'Hel Prado',       'helena.prado@creator.com',       '(11) 91123-0001', '111.222.333-01', 'helena.prado@creator.com',       'Moda e lifestyle',       'São Paulo',     'SP', 'Foco em moda casual e tendências urbanas',          12.50, true, NOW(), NOW()),
('Arthur Pires',       'Arthur Play',     'arthur.pires@creator.com',       '(21) 92234-0002', '222.333.444-02', 'arthur.pires@creator.com',       'Games e tecnologia',     'Rio de Janeiro','RJ', 'Reviews de gadgets e lives patrocinadas',           15.00, true, NOW(), NOW()),
('Beatriz Moreira',    'Bia Moreira',     'beatriz.moreira@creator.com',    '(31) 93345-0003', '333.444.555-03', 'beatriz.moreira@creator.com',    'Beleza e skincare',      'Belo Horizonte','MG', 'Tutoriais de beleza e autocuidado',                 13.75, true, NOW(), NOW()),
('Miguel Tavares',     'Migs Tavares',    'miguel.tavares@creator.com',     '(41) 94456-0004', '444.555.666-04', 'miguel.tavares@creator.com',     'Fitness e saúde',        'Curitiba',      'PR', 'Treinos rápidos e suplementação',                   10.00, true, NOW(), NOW()),
('Clara Azevedo',      'Clara Az',        'clara.azevedo@creator.com',      '(51) 95567-0005', '555.666.777-05', 'clara.azevedo@creator.com',      'Food e receitas',        'Porto Alegre',  'RS', 'Receitas práticas e gastronomia',                   11.50, true, NOW(), NOW()),
('Rafael Goulart',     'Rafa Goulart',    'rafael.goulart@creator.com',     '(61) 96678-0006', '666.777.888-06', 'rafael.goulart@creator.com',     'Finanças pessoais',      'Brasília',      'DF', 'Educação financeira e investimentos',               16.00, true, NOW(), NOW()),
('Sofia Peixoto',      'Sofi Peixoto',    'sofia.peixoto@creator.com',      '(71) 97789-0007', '777.888.999-07', 'sofia.peixoto@creator.com',      'Viagem e turismo',       'Salvador',      'BA', 'Roteiros nacionais e experiências culturais',       14.25, true, NOW(), NOW()),
('Lucas Farias',       'Lu Farias',       'lucas.farias@creator.com',       '(81) 98890-0008', '888.999.111-08', 'lucas.farias@creator.com',       'Humor e entretenimento', 'Recife',        'PE', 'Vídeos curtos de humor e trends',                   12.00, true, NOW(), NOW()),
('Manuela Coelho',     'Manu Coelho',     'manuela.coelho@creator.com',     '(91) 99901-0009', '999.111.222-09', 'manuela.coelho@creator.com',     'Casa e decoração',       'Belém',         'PA', 'Decoração acessível e lifestyle doméstico',         10.50, true, NOW(), NOW()),
('Gabriel Nascimento', 'Gabe Nascimento', 'gabriel.nascimento@creator.com', '(11) 90012-0010', '101.303.505-10', 'gabriel.nascimento@creator.com', 'Música e cultura pop',   'São Paulo',     'SP', 'Música, lançamentos e cultura pop',                 15.50, true, NOW(), NOW()),
('Laura Mendonça',     'Laura Mendes',    'laura.mendonca@creator.com',     '(21) 91123-0011', '202.404.606-11', 'laura.mendonca@creator.com',     'Maternidade e família',  'Niterói',       'RJ', 'Rotina familiar e maternidade leve',                 9.75, true, NOW(), NOW()),
('Pedro Queiroz',      'Pedro Q',         'pedro.queiroz@creator.com',      '(31) 92234-0012', '303.505.707-12', 'pedro.queiroz@creator.com',      'Automotivo',             'Contagem',      'MG', 'Carros, manutenção e reviews',                      13.00, true, NOW(), NOW()),
('Isadora Viana',      'Isa Viana',       'isadora.viana@creator.com',      '(41) 93345-0013', '404.606.808-13', 'isadora.viana@creator.com',      'Educação e carreira',    'Londrina',      'PR', 'Estudos, produtividade e carreira',                 11.25, true, NOW(), NOW()),
('Matheus Cardoso',    'Math Cardoso',    'matheus.cardoso@creator.com',    '(51) 94456-0014', '505.707.909-14', 'matheus.cardoso@creator.com',    'Esportes',               'Caxias do Sul', 'RS', 'Esportes e bastidores de eventos',                  12.75, true, NOW(), NOW()),
('Ana Bezerra',        'Ana Bê',          'ana.bezerra@creator.com',        '(61) 95567-0015', '606.808.101-15', 'ana.bezerra@creator.com',        'Pets',                   'Goiânia',       'GO', 'Pets e produtos para animais',                      10.00, true, NOW(), NOW());


-- ── CREATOR SOCIAL HANDLES (IG + TikTok/YouTube por creator) ────────────────
INSERT INTO creatorsocialhandle (creatorid, platformid, handle, profileurl, followers, engagementrate, isprimary, isactive, createdat, updatedat) VALUES
( 1, 1, '@helprado',         'https://instagram.com/helprado',         284000, 3.85, true,  true, NOW(), NOW()),
( 1, 2, '@helprado',          'https://tiktok.com/@helprado',          512000, 5.20, false, true, NOW(), NOW()),
( 2, 1, '@arthurplay',        'https://instagram.com/arthurplay',      198000, 4.12, true,  true, NOW(), NOW()),
( 2, 3, '@arthurplay',        'https://youtube.com/@arthurplay',       420000, 3.10, false, true, NOW(), NOW()),
( 3, 1, '@biamoreira',        'https://instagram.com/biamoreira',      365000, 4.50, true,  true, NOW(), NOW()),
( 3, 2, '@bia.moreira',       'https://tiktok.com/@bia.moreira',        88000, 6.80, false, true, NOW(), NOW()),
( 4, 1, '@migstavares',       'https://instagram.com/migstavares',     142000, 5.05, true,  true, NOW(), NOW()),
( 4, 2, '@migstavares',       'https://tiktok.com/@migstavares',       267000, 7.40, false, true, NOW(), NOW()),
( 5, 1, '@clara.az',          'https://instagram.com/clara.az',        215000, 4.65, true,  true, NOW(), NOW()),
( 5, 2, '@clara.az',          'https://tiktok.com/@clara.az',          340000, 6.10, false, true, NOW(), NOW()),
( 6, 1, '@rafagoulart',       'https://instagram.com/rafagoulart',     178000, 3.95, true,  true, NOW(), NOW()),
( 6, 3, '@rafagoulart',       'https://youtube.com/@rafagoulart',      305000, 4.20, false, true, NOW(), NOW()),
( 7, 1, '@sofipeixoto',       'https://instagram.com/sofipeixoto',     256000, 4.85, true,  true, NOW(), NOW()),
( 7, 2, '@sofipeixoto',       'https://tiktok.com/@sofipeixoto',       189000, 5.95, false, true, NOW(), NOW()),
( 8, 1, '@lufarias',          'https://instagram.com/lufarias',        412000, 5.50, true,  true, NOW(), NOW()),
( 8, 2, '@lufarias',          'https://tiktok.com/@lufarias',          780000, 8.20, false, true, NOW(), NOW()),
( 9, 1, '@manucoelho',        'https://instagram.com/manucoelho',      168000, 4.30, true,  true, NOW(), NOW()),
( 9, 2, '@manu.coelho',       'https://tiktok.com/@manu.coelho',       125000, 5.40, false, true, NOW(), NOW()),
(10, 1, '@gabenascimento',    'https://instagram.com/gabenascimento',  320000, 4.10, true,  true, NOW(), NOW()),
(10, 2, '@gabenascimento',    'https://tiktok.com/@gabenascimento',    485000, 6.30, false, true, NOW(), NOW()),
(11, 1, '@lauramendes',       'https://instagram.com/lauramendes',     245000, 4.95, true,  true, NOW(), NOW()),
(11, 2, '@lauramendes',       'https://tiktok.com/@lauramendes',       155000, 5.85, false, true, NOW(), NOW()),
(12, 1, '@pedroq',            'https://instagram.com/pedroq',          132000, 4.20, true,  true, NOW(), NOW()),
(12, 3, '@pedroqcanal',       'https://youtube.com/@pedroqcanal',      218000, 3.80, false, true, NOW(), NOW()),
(13, 1, '@isaviana',          'https://instagram.com/isaviana',        188000, 4.55, true,  true, NOW(), NOW()),
(13, 2, '@isaviana',          'https://tiktok.com/@isaviana',          234000, 6.05, false, true, NOW(), NOW()),
(14, 1, '@mathcardoso',       'https://instagram.com/mathcardoso',     295000, 4.75, true,  true, NOW(), NOW()),
(14, 2, '@mathcardoso',       'https://tiktok.com/@mathcardoso',       368000, 5.95, false, true, NOW(), NOW()),
(15, 1, '@anabe',             'https://instagram.com/anabe',           205000, 5.10, true,  true, NOW(), NOW()),
(15, 2, '@anabe',             'https://tiktok.com/@anabe',             162000, 6.50, false, true, NOW(), NOW());


-- ── OPPORTUNITIES (em estágios diferentes do pipeline) ──────────────────────
-- Resolve IDs dos estágios do pipeline (criados via migration default)
DO $$
DECLARE
    v_lead        BIGINT;
    v_qualified   BIGINT;
    v_proposal    BIGINT;
    v_negotiation BIGINT;
    v_won         BIGINT;
    v_lost        BIGINT;
BEGIN
    SELECT id INTO v_lead        FROM commercialpipelinestage WHERE name ILIKE 'Lead'         LIMIT 1;
    SELECT id INTO v_qualified   FROM commercialpipelinestage WHERE name ILIKE 'Qualificad%'  LIMIT 1;
    SELECT id INTO v_proposal    FROM commercialpipelinestage WHERE name ILIKE 'Proposta%'    LIMIT 1;
    SELECT id INTO v_negotiation FROM commercialpipelinestage WHERE name ILIKE 'Negociaç%'    LIMIT 1;
    SELECT id INTO v_won         FROM commercialpipelinestage WHERE name ILIKE 'Ganh%'        LIMIT 1;
    SELECT id INTO v_lost        FROM commercialpipelinestage WHERE name ILIKE 'Perdid%'      LIMIT 1;

    INSERT INTO opportunity (brandid, name, description, commercialpipelinestageid, estimatedvalue, probability, probabilityismanual, contactname, contactemail, notes, createdat, updatedat) VALUES
    (1, 'Skala - Linha Profissional Q2',        'Campanha de cabelo com 6 creators de beleza',         v_lead,        85000.00,  10.00, false, 'Mariana Lopes',  'mariana@skala.com.br',       'Primeiro contato em evento',                NOW() - INTERVAL '2 days',  NOW()),
    (2, 'Natura Beleza Consciente',             'Campanha institucional com creators sustentáveis',     v_qualified,  190000.00,  35.00, false, 'Eduarda Lopes',  'eduarda.lopes@natura.net',    'Briefing aprovado internamente',            NOW() - INTERVAL '5 days',  NOW()),
    (3, 'Avon Brilho Mãe',                       'Campanha emocional Dia das Mães com 5 creators',       v_proposal,    72000.00,  55.00, false, 'Patrícia Reis',  'patricia.reis@avon.com',     'Proposta enviada aguardando feedback',      NOW() - INTERVAL '8 days',  NOW()),
    (4, 'Lapinha Wellness Retreat',             'Conteúdo em retiro com creators de lifestyle',         v_negotiation, 145000.00,  70.00, false, 'Roberto Alves',  'roberto@lapinha.com.br',     'Negociando valor de 2 creators top',        NOW() - INTERVAL '11 days', NOW()),
    (5, 'Itaú Pontos Turbo',                    'Programa de recompensas com creators financeiros',    v_won,         200000.00, 100.00, false, 'Carlos Eduardo', 'carlos.eduardo@itau.com.br', 'Contrato assinado, campanha em execução',   NOW() - INTERVAL '40 days', NOW()),
    (6, 'O Boticário Dia das Mães',             'Campanha emocional com creators mães',                 v_won,         150000.00, 100.00, false, 'Ana Paula',      'ana.paula@oboticario.com.br','Contrato assinado, campanha planejada',     NOW() - INTERVAL '25 days', NOW()),
    (7, 'Magalu Semana do Consumidor',          'Campanha promocional de varejo',                       v_lost,         95000.00,   0.00, false, 'Ricardo Gomes',  'ricardo@magazineluiza.com.br','Cliente optou por agência concorrente',     NOW() - INTERVAL '60 days', NOW()),
    (8, 'Granado Coleção Inverno',              'Lançamento de sabonetes com creators de skincare',     v_qualified,   110000.00,  35.00, false, 'Helena Garcia',  'helena@granado.com.br',      'Reunião de alinhamento realizada',          NOW() - INTERVAL '3 days',  NOW());
END $$;


-- ── PROPOSALS (apenas para opportunities em estágio Proposta/Negociação/Ganha) ──
INSERT INTO proposal (opportunityid, name, internalownerid, description, validityuntil, totalvalue, status, notes, createdat, updatedat) VALUES
(3, 'Avon Brilho Mãe - v1',         1, 'Pacote com 5 creators do nicho beleza/maternidade', NOW() + INTERVAL '15 days',  72000.00, 2, 'Proposta enviada, aguardando feedback', NOW() - INTERVAL '6 days',  NOW()),
(4, 'Lapinha Wellness Retreat - v2', 1, 'Pacote premium com 4 creators e logística inclusa', NOW() + INTERVAL '20 days', 145000.00, 3, 'Cliente visualizou, em negociação',     NOW() - INTERVAL '9 days',  NOW()),
(5, 'Itaú Pontos Turbo - final',    1, 'Programa educativo com 4 creators financeiros',    NOW() - INTERVAL '25 days', 200000.00, 6, 'Convertida em campanha',                NOW() - INTERVAL '38 days', NOW()),
(6, 'Boticário Dia das Mães - final',1,'Pacote emocional com 6 creators mães',              NOW() - INTERVAL '15 days', 150000.00, 6, 'Convertida em campanha',                NOW() - INTERVAL '23 days', NOW());


-- ── CAMPAIGNS (oriundas das propostas convertidas + algumas em andamento) ───
INSERT INTO campaign (brandid, name, description, budget, startsat, endsat, status, objective, briefing, internalownername, notes, isactive, createdat, updatedat) VALUES
(5, 'Itaú Pontos Turbo',         'Programa de recompensas com creators financeiros', 200000.00, NOW() - INTERVAL '20 days', NOW() + INTERVAL '40 days', 3, 'Aumentar uso do programa de pontos',     'Creators de finanças explicando benefícios',           'Carla Mendes',  'Campanha de longa duração', true, NOW() - INTERVAL '38 days', NOW()),
(6, 'Boticário Dia das Mães',    'Campanha emocional com creators mães',              150000.00, NOW() + INTERVAL '5 days',  NOW() + INTERVAL '25 days', 2, 'Gerar conexão emocional e vendas',       'Storytelling emocional e reels de presente',           'Diego Oliveira','Sazonal importante',         true, NOW() - INTERVAL '23 days', NOW()),
(2, 'Natura Beleza Consciente',  'Campanha institucional com creators sustentáveis',  190000.00, NOW() + INTERVAL '15 days', NOW() + INTERVAL '55 days', 2, 'Promover sustentabilidade e beleza natural','Creators com foco em ingredientes naturais',         'Lucas Fernandes','Campanha institucional',     true, NOW() - INTERVAL '4 days',  NOW()),
(1, 'Skala Linha Profissional',  'Campanha com creators de beleza',                    85000.00, NOW() + INTERVAL '30 days', NOW() + INTERVAL '50 days', 1, 'Lançar nova linha profissional',         'Tutoriais e reviews',                                  'Ana Silva',     'Em planejamento',            true, NOW() - INTERVAL '1 day',   NOW());


-- ── CAMPAIGN CREATORS ───────────────────────────────────────────────────────
-- status id: 1=Convidado, 2=Pendente, 3=Confirmado, 4=Em execução, 5=Entregue
INSERT INTO campaigncreator (campaignid, creatorid, campaigncreatorstatusid, agreedamount, agencyfeepercent, agencyfeeamount, confirmedat, notes, createdat, updatedat) VALUES
-- Campanha 1: Itaú (concluída em parte)
(1,  6, 5, 50000.00, 22.00, 11000.00, NOW() - INTERVAL '35 days', 'Vídeo educativo publicado', NOW() - INTERVAL '36 days', NOW()),
(1, 13, 5, 35000.00, 20.00,  7000.00, NOW() - INTERVAL '34 days', 'Stories explicando programa', NOW() - INTERVAL '35 days', NOW()),
(1,  2, 4, 40000.00, 18.00,  7200.00, NOW() - INTERVAL '32 days', 'Em produção do vídeo', NOW() - INTERVAL '33 days', NOW()),
(1, 12, 3, 30000.00, 20.00,  6000.00, NOW() - INTERVAL '30 days', 'Confirmado, briefing recebido', NOW() - INTERVAL '31 days', NOW()),
-- Campanha 2: Boticário (em planejamento)
(2,  3, 3, 28000.00, 18.00,  5040.00, NOW() - INTERVAL '8 days',  'Confirmou, briefing alinhado',  NOW() - INTERVAL '10 days', NOW()),
(2, 11, 3, 30000.00, 18.00,  5400.00, NOW() - INTERVAL '6 days',  'Confirmou após reunião',        NOW() - INTERVAL '8 days',  NOW()),
(2, 15, 2, 22000.00, 20.00,  4400.00, NULL,                       'Aguardando aprovação',          NOW() - INTERVAL '3 days',  NOW()),
-- Campanha 3: Natura
(3,  5, 3, 32000.00, 18.00,  5760.00, NOW() - INTERVAL '1 day',   'Confirmou, foco em sustentabilidade', NOW() - INTERVAL '2 days',  NOW()),
(3,  9, 3, 28000.00, 18.00,  5040.00, NOW() - INTERVAL '1 day',   'Confirmou, decoração consciente',     NOW() - INTERVAL '2 days',  NOW()),
(3,  7, 2, 25000.00, 20.00,  5000.00, NULL,                       'Aguardando agenda de viagem',          NOW() - INTERVAL '1 day',  NOW()),
-- Campanha 4: Skala (planejamento)
(4,  1, 1, 18000.00, 20.00,  3600.00, NULL, 'Convidada via e-mail', NOW(), NOW()),
(4,  3, 1, 22000.00, 18.00,  3960.00, NULL, 'Convidada via e-mail', NOW(), NOW());


-- ── CAMPAIGN DELIVERABLES ──────────────────────────────────────────────────
-- status: 1=Pending, 2=InReview, 3=Approved, 4=Published, 5=Cancelled
-- platform: 1=Instagram, 2=TikTok, 3=YouTube
-- deliverablekind: 1=Reel, 2=Story, 3=Feed Post, 4=Video, 5=Live
INSERT INTO campaigndeliverable (campaignid, campaigncreatorid, deliverablekindid, platformid, title, description, dueat, publishedat, status, grossamount, creatoramount, agencyfeeamount, publishedurl, createdat, updatedat) VALUES
-- Itaú deliverables
(1, 1, 4, 3, 'Como acumular pontos no Itaú',         'Vídeo educativo de 6 minutos',          NOW() - INTERVAL '25 days', NOW() - INTERVAL '24 days', 4, 50000.00, 39000.00, 11000.00, 'https://youtube.com/watch?v=demo-itau-1', NOW() - INTERVAL '36 days', NOW()),
(1, 2, 2, 1, 'Stories Itaú Pontos',                  'Sequência de 5 stories',                NOW() - INTERVAL '23 days', NOW() - INTERVAL '22 days', 4, 35000.00, 28000.00,  7000.00, 'https://instagram.com/stories/demo-itau', NOW() - INTERVAL '35 days', NOW()),
(1, 3, 1, 1, 'Reel - benefícios do programa',         'Reel curto explicando vantagens',       NOW() - INTERVAL '5 days',  NULL,                       2, 40000.00, 32800.00,  7200.00, NULL,                                       NOW() - INTERVAL '33 days', NOW()),
(1, 4, 1, 1, 'Reel - depoimento de uso',              'Reel autêntico de uso real',            NOW() + INTERVAL '3 days',  NULL,                       1, 30000.00, 24000.00,  6000.00, NULL,                                       NOW() - INTERVAL '31 days', NOW()),
-- Boticário deliverables
(2, 5, 1, 1, 'Reel emocional Dia das Mães',           'Storytelling com produto',              NOW() + INTERVAL '15 days', NULL,                       1, 28000.00, 22960.00,  5040.00, NULL,                                       NOW() - INTERVAL '10 days', NOW()),
(2, 6, 3, 1, 'Feed post coleção das mães',            'Post fotográfico com produto',          NOW() + INTERVAL '18 days', NULL,                       1, 30000.00, 24600.00,  5400.00, NULL,                                       NOW() - INTERVAL '8 days',  NOW()),
-- Natura deliverables
(3, 8, 1, 1, 'Reel - ingredientes naturais',          'Demonstração de produto',               NOW() + INTERVAL '25 days', NULL,                       1, 32000.00, 26240.00,  5760.00, NULL,                                       NOW() - INTERVAL '2 days',  NOW()),
(3, 9, 3, 1, 'Feed post decoração consciente',        'Post lifestyle com produto',            NOW() + INTERVAL '28 days', NULL,                       1, 28000.00, 22960.00,  5040.00, NULL,                                       NOW() - INTERVAL '2 days',  NOW());


-- ── FINANCIAL ACCOUNTS (extras) ─────────────────────────────────────────────
INSERT INTO financialaccount (name, type, bank, agency, number, initialbalance, color, isactive, createdat, updatedat) VALUES
('Conta Corrente Itaú',  1, 'Itaú',     '0341', '12345-6',  85000.00, '#1F8A5B', true, NOW(), NOW()),
('Bradesco Empresarial', 1, 'Bradesco', '0237', '67890-1',  42000.00, '#E07A3D', true, NOW(), NOW()),
('Conta Pix Asaas',      1, 'Asaas',    '0001', '99887-7',  18000.00, '#00AEC7', true, NOW(), NOW());


-- ── FINANCIAL ENTRIES ──────────────────────────────────────────────────────
-- type: 1=Receivable, 2=Payable | status: 1=Pending, 2=Paid, 3=Overdue
-- category: 1=BrandReceivable, 2=CreatorPayout, 3=AgencyFee, 4=Operational
-- accountid: 1=Caixa, 2=Itaú, 3=Bradesco, 4=Asaas
INSERT INTO financialentry (accountid, type, category, description, amount, dueat, occurredat, status, paidat, paymentmethod, referencecode, counterpartyname, notes, campaignid, campaigndeliverableid, createdat, updatedat) VALUES
-- Recebíveis pagos
(2, 1, 1, 'Itaú Pontos Turbo - parcela 1/2', 100000.00, NOW() - INTERVAL '25 days', NOW() - INTERVAL '30 days', 2, NOW() - INTERVAL '22 days', 'PIX',    'PIX-ITAU-001',  'Itaú',          'Recebido via PIX',           1, NULL, NOW(), NOW()),
(2, 1, 1, 'Itaú Pontos Turbo - parcela 2/2', 100000.00, NOW() - INTERVAL '8 days',  NOW() - INTERVAL '15 days', 2, NOW() - INTERVAL '5 days',  'PIX',    'PIX-ITAU-002',  'Itaú',          'Saldo final',                1, NULL, NOW(), NOW()),
-- Recebíveis pendentes
(2, 1, 1, 'Boticário Dia das Mães - sinal 30%',  45000.00, NOW() + INTERVAL '8 days',  NOW() - INTERVAL '2 days',  1, NULL, 'Boleto', 'BOL-BOT-001',  'O Boticário', 'Sinal da campanha',          2, NULL, NOW(), NOW()),
(2, 1, 1, 'Boticário Dia das Mães - saldo 70%', 105000.00, NOW() + INTERVAL '35 days', NOW() - INTERVAL '2 days',  1, NULL, 'Boleto', 'BOL-BOT-002',  'O Boticário', 'Saldo após campanha',        2, NULL, NOW(), NOW()),
(2, 1, 1, 'Natura - sinal 40%',                  76000.00, NOW() + INTERVAL '12 days', NOW() - INTERVAL '1 day',   1, NULL, 'PIX',    'PIX-NAT-001',  'Natura',      'Sinal a receber',            3, NULL, NOW(), NOW()),
(2, 1, 1, 'Natura - saldo 60%',                 114000.00, NOW() + INTERVAL '50 days', NOW() - INTERVAL '1 day',   1, NULL, 'PIX',    'PIX-NAT-002',  'Natura',      'Saldo final',                3, NULL, NOW(), NOW()),
-- Recebível vencido
(2, 1, 1, 'Skala - retroativo',                  25000.00, NOW() - INTERVAL '10 days', NOW() - INTERVAL '40 days', 3, NULL, 'Boleto', 'BOL-SKA-001',  'Skala',       'Vencido — em cobrança',      4, NULL, NOW(), NOW()),
-- Pagáveis a creators (pagos)
(4, 2, 2, 'Pagamento Rafael Goulart - Itaú',      39000.00, NOW() - INTERVAL '20 days', NOW() - INTERVAL '24 days', 2, NOW() - INTERVAL '19 days', 'PIX', 'PIX-CR-01', 'Rafael Goulart',  'Pago após vídeo aprovado',   1, 1, NOW(), NOW()),
(4, 2, 2, 'Pagamento Isadora Viana - Itaú',       28000.00, NOW() - INTERVAL '18 days', NOW() - INTERVAL '22 days', 2, NOW() - INTERVAL '17 days', 'PIX', 'PIX-CR-02', 'Isadora Viana',   'Pago após stories',          1, 2, NOW(), NOW()),
-- Pagáveis pendentes
(4, 2, 2, 'Pagamento Arthur Pires - Itaú',        32800.00, NOW() + INTERVAL '3 days',  NOW() - INTERVAL '5 days',  1, NULL, 'PIX', NULL, 'Arthur Pires',     'Aguardando aprovação',       1, 3, NOW(), NOW()),
(4, 2, 2, 'Pagamento Pedro Queiroz - Itaú',       24000.00, NOW() + INTERVAL '10 days', NOW() - INTERVAL '3 days',  1, NULL, 'PIX', NULL, 'Pedro Queiroz',    'Aguardando entrega',         1, 4, NOW(), NOW()),
(4, 2, 2, 'Pagamento Beatriz Moreira - Boticário',22960.00, NOW() + INTERVAL '20 days', NOW() - INTERVAL '2 days',  1, NULL, 'PIX', NULL, 'Beatriz Moreira',  'Aguardando entrega',         2, 5, NOW(), NOW()),
(4, 2, 2, 'Pagamento Laura Mendonça - Boticário', 24600.00, NOW() + INTERVAL '22 days', NOW() - INTERVAL '2 days',  1, NULL, 'PIX', NULL, 'Laura Mendonça',   'Aguardando entrega',         2, 6, NOW(), NOW()),
(4, 2, 2, 'Pagamento Clara Azevedo - Natura',     26240.00, NOW() + INTERVAL '30 days', NOW() - INTERVAL '1 day',   1, NULL, 'PIX', NULL, 'Clara Azevedo',    'Aguardando entrega',         3, 7, NOW(), NOW()),
(4, 2, 2, 'Pagamento Manuela Coelho - Natura',    22960.00, NOW() + INTERVAL '32 days', NOW() - INTERVAL '1 day',   1, NULL, 'PIX', NULL, 'Manuela Coelho',   'Aguardando entrega',         3, 8, NOW(), NOW()),
-- Custos operacionais
(1, 2, 4, 'Licença Adobe Creative Cloud',           299.00, NOW() + INTERVAL '8 days',  NOW() - INTERVAL '5 days',  1, NULL, 'Cartão', NULL, 'Adobe Brasil', 'Mensal recorrente',         NULL, NULL, NOW(), NOW()),
(1, 2, 4, 'Anúncio Meta Ads - boost Itaú',         1500.00, NOW() - INTERVAL '2 days',  NOW() - INTERVAL '10 days', 2, NOW() - INTERVAL '2 days', 'Cartão', 'META-CC-001', 'Meta', 'Boost de posts',                  1, NULL, NOW(), NOW()),
-- Fee de agência (recebido)
(2, 1, 3, 'Agency Fee - Itaú Pontos Turbo',       40000.00, NOW() - INTERVAL '6 days',  NOW() - INTERVAL '20 days', 2, NOW() - INTERVAL '5 days',  'PIX',    'PIX-FEE-001',  'Itaú',         'Fee 20% sobre 200k',         1, NULL, NOW(), NOW());


-- ── AUTOMATIONS ────────────────────────────────────────────────────────────
INSERT INTO automation (name, trigger, triggercondition, connectorid, pipelineid, variablemappingjson, isactive, createdat, updatedat) VALUES
('Notificar time quando proposta é aprovada',     'proposal_approved',          NULL,             1, 1, '{"channel":"#comercial","brand":"{{Brand.Name}}","value":"{{Proposal.TotalValue}}"}', true,  NOW() - INTERVAL '30 days', NOW()),
('Enviar contrato Zapsign após proposta aprovada','proposal_approved',          NULL,             1, 2, '{"document_template":"contrato-campanha","brand_email":"{{Brand.ContactEmail}}"}',     true,  NOW() - INTERVAL '25 days', NOW()),
('Alerta de follow-up atrasado',                  'follow_up_overdue',          NULL,             1, 3, '{"to":"{{Owner.Email}}","opportunity":"{{Opportunity.Name}}"}',                         true,  NOW() - INTERVAL '20 days', NOW()),
('Notificar creator quando entrega aprovada',     'deliverable_brand_approved', NULL,             1, 4, '{"to":"{{Creator.Phone}}","campaign":"{{Campaign.Name}}"}',                              true,  NOW() - INTERVAL '15 days', NOW()),
('Cobrar marca quando recebível vence',           'financial_overdue',          'amount > 10000', 1, 5, '{"to":"{{Brand.ContactEmail}}","amount":"{{Entry.Amount}}","reference":"{{Entry.ReferenceCode}}"}', true, NOW() - INTERVAL '10 days', NOW()),
('Avisar finance quando recebível é criado',      'financial_receivable_created', NULL,           1, 6, '{"channel":"#financeiro","amount":"{{Entry.Amount}}","brand":"{{Counterparty.Name}}"}', false, NOW() - INTERVAL '5 days',  NOW());


-- ── AUTOMATION EXECUTION LOGS ──────────────────────────────────────────────
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
   '{"to":"bruno.costa@agency.com","opportunity":"Natura Beleza Consciente"}',
   NULL, NOW() - INTERVAL '6 days'),
(3, 'Alerta de follow-up atrasado',                    'follow_up_overdue',          true,
   '{"to":"carla.mendes@agency.com","opportunity":"Avon Brilho Mãe"}',
   NULL, NOW() - INTERVAL '3 days'),
(4, 'Notificar creator quando entrega aprovada',       'deliverable_brand_approved', true,
   '{"to":"+5561966780006","campaign":"Itaú Pontos Turbo"}',
   NULL, NOW() - INTERVAL '12 days'),
(4, 'Notificar creator quando entrega aprovada',       'deliverable_brand_approved', false,
   '{"to":"+5511911230011","campaign":"Itaú Pontos Turbo"}',
   'Conector WhatsApp não configurado para esta agência',
   NOW() - INTERVAL '10 days'),
(5, 'Cobrar marca quando recebível vence',             'financial_overdue',          true,
   '{"to":"mariana@skala.com.br","amount":"25000.00","reference":"BOL-SKA-001"}',
   NULL, NOW() - INTERVAL '2 days'),
(5, 'Cobrar marca quando recebível vence',             'financial_overdue',          false,
   '{"to":"mariana@skala.com.br","amount":"25000.00","reference":"BOL-SKA-001"}',
   'Template de e-mail não encontrado no Zapsign',
   NOW() - INTERVAL '1 day');


COMMIT;


-- ── Verificação ────────────────────────────────────────────────────────────
SELECT 'brand'                  AS tabela, COUNT(*) FROM brand
UNION ALL SELECT 'creator',                COUNT(*) FROM creator
UNION ALL SELECT 'creatorsocialhandle',    COUNT(*) FROM creatorsocialhandle
UNION ALL SELECT 'opportunity',            COUNT(*) FROM opportunity
UNION ALL SELECT 'proposal',               COUNT(*) FROM proposal
UNION ALL SELECT 'campaign',               COUNT(*) FROM campaign
UNION ALL SELECT 'campaigncreator',        COUNT(*) FROM campaigncreator
UNION ALL SELECT 'campaigndeliverable',    COUNT(*) FROM campaigndeliverable
UNION ALL SELECT 'financialaccount',       COUNT(*) FROM financialaccount
UNION ALL SELECT 'financialentry',         COUNT(*) FROM financialentry
UNION ALL SELECT 'automation',             COUNT(*) FROM automation
UNION ALL SELECT 'automationexecutionlog', COUNT(*) FROM automationexecutionlog;
