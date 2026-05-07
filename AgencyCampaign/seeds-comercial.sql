-- =====================================================================
-- Seeds para o módulo Comercial do Kanvas/AgencyCampaign
-- =====================================================================
-- Como executar:
--   psql "<connection-string-do-tenant>" -f seeds-comercial.sql
--
-- Pré-requisitos:
--   - Banco com migrations aplicadas até Migration_202605070003
--   - Estágios padrão do pipeline já populados (Lead, Qualificada,
--     Proposta, Negociação, Ganha, Perdida) — vêm na migration 202604260001
--
-- Este script ASSUME que as tabelas abaixo estão vazias. Se já houver
-- registros, pode haver duplicação.
--
-- Cobre: brand, creator, commercialresponsible, opportunity,
-- opportunitystagehistory, opportunitycomment, opportunityfollowup,
-- opportunitynegotiation, opportunityapprovalrequest, proposal,
-- proposalitem, proposalversion, proposalsharelink, proposalblock,
-- proposaltemplate, proposaltemplateitem.
-- =====================================================================

BEGIN;

-- =====================================================================
-- 1. MARCAS
-- =====================================================================
INSERT INTO brand (name, tradename, document, contactname, contactemail, notes, isactive, createdat, updatedat) VALUES
  ('Adidas Brasil', 'Adidas', '90.123.456/0001-78', 'Bruno Martins', 'bruno.martins@adidas.com.br', 'Esportivo, parceria recorrente', true, NOW() - INTERVAL '60 days', NOW()),
  ('Natura Cosméticos', 'Natura', '33.456.789/0001-01', 'Eduarda Lopes', 'eduarda.lopes@natura.net', 'Beleza e sustentabilidade', true, NOW() - INTERVAL '50 days', NOW()),
  ('Magazine Luiza', 'Magalu', '78.901.234/0001-56', 'Ricardo Gomes', 'ricardo.gomes@magalu.com', 'Varejo, foco em performance', true, NOW() - INTERVAL '45 days', NOW()),
  ('iFood', 'iFood', '11.222.333/0001-44', 'Camila Rocha', 'camila.rocha@ifood.com.br', 'Delivery, campanhas regionais', true, NOW() - INTERVAL '30 days', NOW()),
  ('Starbucks Brasil', 'Starbucks', '22.333.444/0001-55', 'Pedro Almeida', 'pedro.almeida@starbucks.com.br', 'Lifestyle e gastronomia', true, NOW() - INTERVAL '20 days', NOW());

-- =====================================================================
-- 2. CREATORS
-- =====================================================================
INSERT INTO creator (name, stagename, email, phone, document, primaryniche, city, state, defaultagencyfeepercent, isactive, createdat, updatedat) VALUES
  ('Mariana Costa', 'mari.costa', 'mariana@creator.com', '(11) 99111-2222', '111.222.333-44', 'Lifestyle', 'São Paulo', 'SP', 15.00, true, NOW() - INTERVAL '90 days', NOW()),
  ('Lucas Oliveira', 'lucasoli', 'lucas@creator.com', '(11) 99222-3333', '222.333.444-55', 'Tecnologia', 'São Paulo', 'SP', 20.00, true, NOW() - INTERVAL '80 days', NOW()),
  ('Beatriz Souza', 'beasouza', 'beatriz@creator.com', '(21) 99333-4444', '333.444.555-66', 'Moda', 'Rio de Janeiro', 'RJ', 18.00, true, NOW() - INTERVAL '70 days', NOW()),
  ('Rafael Santos', 'rafasantos', 'rafael@creator.com', '(31) 99444-5555', '444.555.666-77', 'Fitness', 'Belo Horizonte', 'MG', 15.00, true, NOW() - INTERVAL '60 days', NOW()),
  ('Camila Ferreira', 'camifer', 'camila@creator.com', '(11) 99555-6666', '555.666.777-88', 'Beleza', 'São Paulo', 'SP', 22.00, true, NOW() - INTERVAL '40 days', NOW()),
  ('Pedro Henrique', 'phlima', 'pedro@creator.com', '(41) 99666-7777', '666.777.888-99', 'Gastronomia', 'Curitiba', 'PR', 17.00, true, NOW() - INTERVAL '20 days', NOW());

-- =====================================================================
-- 3. RESPONSÁVEIS COMERCIAIS
-- =====================================================================
INSERT INTO commercialresponsible (name, email, phone, notes, isactive, createdat, updatedat) VALUES
  ('Ana Silva', 'ana.silva@kanvas.com', '(11) 98765-4321', 'Sênior, foco em contas estratégicas', true, NOW() - INTERVAL '100 days', NOW()),
  ('Bruno Costa', 'bruno.costa@kanvas.com', '(11) 97654-3210', 'Pleno, especialista em tech e startups', true, NOW() - INTERVAL '90 days', NOW()),
  ('Carla Mendes', 'carla.mendes@kanvas.com', '(11) 96543-2109', 'Pleno, food e bebidas', true, NOW() - INTERVAL '80 days', NOW()),
  ('Diego Oliveira', 'diego.oliveira@kanvas.com', '(11) 95432-1098', 'Júnior, varejo e moda', true, NOW() - INTERVAL '60 days', NOW());

-- =====================================================================
-- 4. BLOCOS REUTILIZÁVEIS DE PROPOSTA
-- =====================================================================
INSERT INTO proposalblock (name, body, category, isactive, createdat, updatedat) VALUES
  ('Cláusula de exclusividade 30 dias',
   'O criador se compromete a não publicar conteúdo patrocinado para marcas concorrentes no período de 30 (trinta) dias corridos a partir da data de publicação do conteúdo desta proposta.',
   'Cláusula', true, NOW() - INTERVAL '30 days', NOW()),
  ('Direitos de imagem 6 meses',
   'A marca tem direito a utilizar o conteúdo produzido em suas redes sociais, e-mail marketing e website pelo período de 6 (seis) meses a contar da data de publicação, sem custos adicionais.',
   'Cláusula', true, NOW() - INTERVAL '30 days', NOW()),
  ('Pagamento 50% antecipado / 50% pós-publicação',
   '50% do valor total da proposta deve ser pago em até 5 (cinco) dias úteis após a assinatura do contrato. Os 50% restantes serão pagos em até 10 (dez) dias úteis após a publicação do último entregável.',
   'Pagamento', true, NOW() - INTERVAL '25 days', NOW()),
  ('Aprovação prévia obrigatória',
   'Todo conteúdo deverá ser submetido à marca para aprovação prévia, com pelo menos 48 (quarenta e oito) horas de antecedência da data de publicação. A marca terá até 24h para aprovar ou solicitar ajustes.',
   'Condição comercial', true, NOW() - INTERVAL '20 days', NOW()),
  ('Cancelamento até 7 dias antes',
   'O cancelamento desta proposta sem ônus poderá ser feito por qualquer das partes com no mínimo 7 (sete) dias de antecedência da primeira publicação programada. Após esse prazo, será devida multa de 50% do valor total.',
   'Cancelamento', true, NOW() - INTERVAL '15 days', NOW()),
  ('Descrição padrão Reels patrocinado',
   'Reels patrocinado de 30 a 60 segundos no Instagram, com menção verbal e visual da marca, hashtag oficial e tag no perfil oficial. Roteiro alinhado previamente com o briefing.',
   'Descrição padrão', true, NOW() - INTERVAL '10 days', NOW());

-- =====================================================================
-- 5. TEMPLATES DE PROPOSTA
-- =====================================================================
WITH t1 AS (
  INSERT INTO proposaltemplate (name, description, isactive, createdat, updatedat)
  VALUES ('Pacote Lançamento Reels', 'Modelo padrão para campanhas de lançamento com 3 reels e 2 stories', true, NOW() - INTERVAL '20 days', NOW())
  RETURNING id
)
INSERT INTO proposaltemplateitem (proposaltemplateid, description, defaultquantity, defaultunitprice, defaultdeliverydays, observations, displayorder, createdat, updatedat)
SELECT id, 'Reels patrocinado 30-60s no Instagram', 3, 8000.00, 14, 'Roteiro previamente aprovado', 0, NOW(), NOW() FROM t1
UNION ALL
SELECT id, 'Stories com link/sticker', 2, 1500.00, 7, NULL, 1, NOW(), NOW() FROM t1
UNION ALL
SELECT id, 'Repostagem em feed (carrossel)', 1, 3500.00, 21, 'Carrossel de 5 imagens', 2, NOW(), NOW() FROM t1;

WITH t2 AS (
  INSERT INTO proposaltemplate (name, description, isactive, createdat, updatedat)
  VALUES ('Cobertura de evento - 1 dia', 'Cobertura completa de evento presencial com creator', true, NOW() - INTERVAL '15 days', NOW())
  RETURNING id
)
INSERT INTO proposaltemplateitem (proposaltemplateid, description, defaultquantity, defaultunitprice, defaultdeliverydays, observations, displayorder, createdat, updatedat)
SELECT id, 'Presença no evento (4 horas)', 1, 12000.00, 1, 'Inclui transporte e refeição', 0, NOW(), NOW() FROM t2
UNION ALL
SELECT id, 'Stories ao vivo durante o evento', 8, 800.00, 1, NULL, 1, NOW(), NOW() FROM t2
UNION ALL
SELECT id, 'Reels recap pós-evento', 1, 6500.00, 5, 'Edição em até 5 dias úteis', 2, NOW(), NOW() FROM t2;

WITH t3 AS (
  INSERT INTO proposaltemplate (name, description, isactive, createdat, updatedat)
  VALUES ('Review de produto - 2 plataformas', 'Review honesto do produto em Instagram + TikTok', true, NOW() - INTERVAL '10 days', NOW())
  RETURNING id
)
INSERT INTO proposaltemplateitem (proposaltemplateid, description, defaultquantity, defaultunitprice, defaultdeliverydays, observations, displayorder, createdat, updatedat)
SELECT id, 'Reels review no Instagram (60-90s)', 1, 5500.00, 10, NULL, 0, NOW(), NOW() FROM t3
UNION ALL
SELECT id, 'TikTok review no formato vertical (60s)', 1, 4500.00, 10, NULL, 1, NOW(), NOW() FROM t3
UNION ALL
SELECT id, 'Stories teaser (2 sequências)', 2, 1200.00, 5, 'Antes da publicação principal', 2, NOW(), NOW() FROM t3;

-- =====================================================================
-- 6. OPORTUNIDADES (cobrindo todos os estágios + diferentes responsáveis)
-- =====================================================================
-- Lead inicial — Adidas Tênis 2026 (Ana Silva)
INSERT INTO opportunity (brandid, commercialpipelinestageid, name, description, estimatedvalue, probability, probabilityismanual, expectedcloseat, commercialresponsibleid, contactname, contactemail, notes, createdat, updatedat)
VALUES (
  (SELECT id FROM brand WHERE name = 'Adidas Brasil'),
  (SELECT id FROM commercialpipelinestage WHERE name = 'Lead'),
  'Adidas Tênis Performance — campanha 2026',
  'Lançamento da nova linha de tênis de corrida para Maio/2026',
  85000.00, 10.00, false, NOW() + INTERVAL '60 days',
  (SELECT id FROM commercialresponsible WHERE name = 'Ana Silva'),
  'Bruno Martins', 'bruno.martins@adidas.com.br',
  'Indicação via parceria anterior. Aguardando briefing oficial.',
  NOW() - INTERVAL '5 days', NOW() - INTERVAL '5 days'
);

-- Qualificada — Natura SP Lifestyle (Carla Mendes)
INSERT INTO opportunity (brandid, commercialpipelinestageid, name, description, estimatedvalue, probability, probabilityismanual, expectedcloseat, commercialresponsibleid, contactname, contactemail, notes, createdat, updatedat)
VALUES (
  (SELECT id FROM brand WHERE name = 'Natura Cosméticos'),
  (SELECT id FROM commercialpipelinestage WHERE name = 'Qualificada'),
  'Natura — Linha Tukum Verão',
  'Lançamento da linha Tukum focado em creators de SP/RJ',
  120000.00, 30.00, false, NOW() + INTERVAL '45 days',
  (SELECT id FROM commercialresponsible WHERE name = 'Carla Mendes'),
  'Eduarda Lopes', 'eduarda.lopes@natura.net',
  'Briefing recebido, alinhamento com 6 creators em validação.',
  NOW() - INTERVAL '12 days', NOW() - INTERVAL '2 days'
);

-- Proposta — Magalu Black Friday (Diego Oliveira)
INSERT INTO opportunity (brandid, commercialpipelinestageid, name, description, estimatedvalue, probability, probabilityismanual, expectedcloseat, commercialresponsibleid, contactname, contactemail, notes, createdat, updatedat)
VALUES (
  (SELECT id FROM brand WHERE name = 'Magazine Luiza'),
  (SELECT id FROM commercialpipelinestage WHERE name = 'Proposta'),
  'Magalu — Esquenta Black Friday',
  'Campanha 2 semanas antes da Black Friday, foco em performance',
  240000.00, 60.00, false, NOW() + INTERVAL '30 days',
  (SELECT id FROM commercialresponsible WHERE name = 'Diego Oliveira'),
  'Ricardo Gomes', 'ricardo.gomes@magalu.com',
  'Proposta enviada, aguardando aprovação interna do cliente.',
  NOW() - INTERVAL '20 days', NOW() - INTERVAL '3 days'
);

-- Negociação com probabilidade manual — iFood Regional (Bruno Costa)
INSERT INTO opportunity (brandid, commercialpipelinestageid, name, description, estimatedvalue, probability, probabilityismanual, expectedcloseat, commercialresponsibleid, contactname, contactemail, notes, createdat, updatedat)
VALUES (
  (SELECT id FROM brand WHERE name = 'iFood'),
  (SELECT id FROM commercialpipelinestage WHERE name = 'Negociação'),
  'iFood Regional Sul — campanha aniversário',
  'Reativação de clientes, foco PR/SC/RS',
  95000.00, 90.00, true, NOW() + INTERVAL '15 days',
  (SELECT id FROM commercialresponsible WHERE name = 'Bruno Costa'),
  'Camila Rocha', 'camila.rocha@ifood.com.br',
  'Negociação avançada. Cliente solicitou ajustes em 2 entregáveis.',
  NOW() - INTERVAL '35 days', NOW() - INTERVAL '1 day'
);

-- Ganha — Starbucks Inverno (Ana Silva, fechada)
INSERT INTO opportunity (brandid, commercialpipelinestageid, name, description, estimatedvalue, probability, probabilityismanual, expectedcloseat, commercialresponsibleid, contactname, contactemail, notes, closedat, wonnotes, createdat, updatedat)
VALUES (
  (SELECT id FROM brand WHERE name = 'Starbucks Brasil'),
  (SELECT id FROM commercialpipelinestage WHERE name = 'Ganha'),
  'Starbucks — Bebidas de Inverno',
  'Cardápio sazonal junho-agosto',
  68000.00, 100.00, false, NOW() - INTERVAL '5 days',
  (SELECT id FROM commercialresponsible WHERE name = 'Ana Silva'),
  'Pedro Almeida', 'pedro.almeida@starbucks.com.br',
  'Fechado dentro do escopo previsto.',
  NOW() - INTERVAL '5 days',
  'Cliente assinou no escopo padrão, sem desconto.',
  NOW() - INTERVAL '40 days', NOW() - INTERVAL '5 days'
);

-- Perdida — Adidas Lifestyle (Diego Oliveira)
INSERT INTO opportunity (brandid, commercialpipelinestageid, name, description, estimatedvalue, probability, probabilityismanual, expectedcloseat, commercialresponsibleid, contactname, contactemail, notes, closedat, lossreason, createdat, updatedat)
VALUES (
  (SELECT id FROM brand WHERE name = 'Adidas Brasil'),
  (SELECT id FROM commercialpipelinestage WHERE name = 'Perdida'),
  'Adidas — Linha Lifestyle Casual',
  'Campanha de mid-season, casual wear',
  72000.00, 0.00, false, NOW() - INTERVAL '10 days',
  (SELECT id FROM commercialresponsible WHERE name = 'Diego Oliveira'),
  'Bruno Martins', 'bruno.martins@adidas.com.br',
  'Cliente optou por concorrente.',
  NOW() - INTERVAL '10 days',
  'Preço maior que concorrente direto.',
  NOW() - INTERVAL '50 days', NOW() - INTERVAL '10 days'
);

-- =====================================================================
-- 7. STAGE HISTORY (entrada inicial + transições para timeline rica)
-- =====================================================================
-- Adidas Tênis 2026 — só "criada" (em Lead desde o início)
INSERT INTO opportunitystagehistory (opportunityid, fromstageid, tostageid, changedat, changedbyusername, reason, createdat, updatedat)
SELECT o.id, NULL, (SELECT id FROM commercialpipelinestage WHERE name = 'Lead'),
  o.createdat, 'Ana Silva', 'Oportunidade criada', o.createdat, o.createdat
FROM opportunity o WHERE o.name = 'Adidas Tênis Performance — campanha 2026';

-- Natura — Lead → Qualificada
INSERT INTO opportunitystagehistory (opportunityid, fromstageid, tostageid, changedat, changedbyusername, reason, createdat, updatedat)
SELECT o.id, NULL, (SELECT id FROM commercialpipelinestage WHERE name = 'Lead'),
  o.createdat, 'Carla Mendes', 'Oportunidade criada', o.createdat, o.createdat
FROM opportunity o WHERE o.name = 'Natura — Linha Tukum Verão';

INSERT INTO opportunitystagehistory (opportunityid, fromstageid, tostageid, changedat, changedbyusername, reason, createdat, updatedat)
SELECT
  (SELECT id FROM opportunity WHERE name = 'Natura — Linha Tukum Verão'),
  (SELECT id FROM commercialpipelinestage WHERE name = 'Lead'),
  (SELECT id FROM commercialpipelinestage WHERE name = 'Qualificada'),
  NOW() - INTERVAL '6 days', 'Carla Mendes', 'Briefing recebido',
  NOW() - INTERVAL '6 days', NOW() - INTERVAL '6 days';

-- Magalu Black Friday — Lead → Qualificada → Proposta
INSERT INTO opportunitystagehistory (opportunityid, fromstageid, tostageid, changedat, changedbyusername, reason, createdat, updatedat)
SELECT o.id, NULL, (SELECT id FROM commercialpipelinestage WHERE name = 'Lead'),
  o.createdat, 'Diego Oliveira', 'Oportunidade criada', o.createdat, o.createdat
FROM opportunity o WHERE o.name = 'Magalu — Esquenta Black Friday';

INSERT INTO opportunitystagehistory (opportunityid, fromstageid, tostageid, changedat, changedbyusername, reason, createdat, updatedat)
SELECT
  (SELECT id FROM opportunity WHERE name = 'Magalu — Esquenta Black Friday'),
  (SELECT id FROM commercialpipelinestage WHERE name = 'Lead'),
  (SELECT id FROM commercialpipelinestage WHERE name = 'Qualificada'),
  NOW() - INTERVAL '15 days', 'Diego Oliveira', 'Cliente confirmou interesse e budget',
  NOW() - INTERVAL '15 days', NOW() - INTERVAL '15 days';

INSERT INTO opportunitystagehistory (opportunityid, fromstageid, tostageid, changedat, changedbyusername, reason, createdat, updatedat)
SELECT
  (SELECT id FROM opportunity WHERE name = 'Magalu — Esquenta Black Friday'),
  (SELECT id FROM commercialpipelinestage WHERE name = 'Qualificada'),
  (SELECT id FROM commercialpipelinestage WHERE name = 'Proposta'),
  NOW() - INTERVAL '8 days', 'Diego Oliveira', 'Briefing fechado, montando proposta',
  NOW() - INTERVAL '8 days', NOW() - INTERVAL '8 days';

-- iFood Regional — Lead → Qualificada → Proposta → Negociação
INSERT INTO opportunitystagehistory (opportunityid, fromstageid, tostageid, changedat, changedbyusername, reason, createdat, updatedat)
SELECT o.id, NULL, (SELECT id FROM commercialpipelinestage WHERE name = 'Lead'),
  o.createdat, 'Bruno Costa', 'Oportunidade criada', o.createdat, o.createdat
FROM opportunity o WHERE o.name = 'iFood Regional Sul — campanha aniversário';

INSERT INTO opportunitystagehistory (opportunityid, fromstageid, tostageid, changedat, changedbyusername, reason, createdat, updatedat)
SELECT
  (SELECT id FROM opportunity WHERE name = 'iFood Regional Sul — campanha aniversário'),
  (SELECT id FROM commercialpipelinestage WHERE name = 'Lead'),
  (SELECT id FROM commercialpipelinestage WHERE name = 'Qualificada'),
  NOW() - INTERVAL '28 days', 'Bruno Costa', 'Lead qualificado',
  NOW() - INTERVAL '28 days', NOW() - INTERVAL '28 days';

INSERT INTO opportunitystagehistory (opportunityid, fromstageid, tostageid, changedat, changedbyusername, reason, createdat, updatedat)
SELECT
  (SELECT id FROM opportunity WHERE name = 'iFood Regional Sul — campanha aniversário'),
  (SELECT id FROM commercialpipelinestage WHERE name = 'Qualificada'),
  (SELECT id FROM commercialpipelinestage WHERE name = 'Proposta'),
  NOW() - INTERVAL '20 days', 'Bruno Costa', 'Proposta inicial enviada',
  NOW() - INTERVAL '20 days', NOW() - INTERVAL '20 days';

INSERT INTO opportunitystagehistory (opportunityid, fromstageid, tostageid, changedat, changedbyusername, reason, createdat, updatedat)
SELECT
  (SELECT id FROM opportunity WHERE name = 'iFood Regional Sul — campanha aniversário'),
  (SELECT id FROM commercialpipelinestage WHERE name = 'Proposta'),
  (SELECT id FROM commercialpipelinestage WHERE name = 'Negociação'),
  NOW() - INTERVAL '7 days', 'Bruno Costa', 'Cliente solicitou ajuste de escopo',
  NOW() - INTERVAL '7 days', NOW() - INTERVAL '7 days';

-- Starbucks Inverno — fluxo até Ganha
INSERT INTO opportunitystagehistory (opportunityid, fromstageid, tostageid, changedat, changedbyusername, reason, createdat, updatedat)
SELECT o.id, NULL, (SELECT id FROM commercialpipelinestage WHERE name = 'Lead'),
  o.createdat, 'Ana Silva', 'Oportunidade criada', o.createdat, o.createdat
FROM opportunity o WHERE o.name = 'Starbucks — Bebidas de Inverno';

INSERT INTO opportunitystagehistory (opportunityid, fromstageid, tostageid, changedat, changedbyusername, reason, createdat, updatedat)
SELECT
  (SELECT id FROM opportunity WHERE name = 'Starbucks — Bebidas de Inverno'),
  (SELECT id FROM commercialpipelinestage WHERE name = 'Lead'),
  (SELECT id FROM commercialpipelinestage WHERE name = 'Proposta'),
  NOW() - INTERVAL '30 days', 'Ana Silva', 'Cliente já tinha budget aprovado',
  NOW() - INTERVAL '30 days', NOW() - INTERVAL '30 days';

INSERT INTO opportunitystagehistory (opportunityid, fromstageid, tostageid, changedat, changedbyusername, reason, createdat, updatedat)
SELECT
  (SELECT id FROM opportunity WHERE name = 'Starbucks — Bebidas de Inverno'),
  (SELECT id FROM commercialpipelinestage WHERE name = 'Proposta'),
  (SELECT id FROM commercialpipelinestage WHERE name = 'Ganha'),
  NOW() - INTERVAL '5 days', 'Ana Silva', 'Cliente assinou no escopo padrão',
  NOW() - INTERVAL '5 days', NOW() - INTERVAL '5 days';

-- Adidas Lifestyle — fluxo até Perdida
INSERT INTO opportunitystagehistory (opportunityid, fromstageid, tostageid, changedat, changedbyusername, reason, createdat, updatedat)
SELECT o.id, NULL, (SELECT id FROM commercialpipelinestage WHERE name = 'Lead'),
  o.createdat, 'Diego Oliveira', 'Oportunidade criada', o.createdat, o.createdat
FROM opportunity o WHERE o.name = 'Adidas — Linha Lifestyle Casual';

INSERT INTO opportunitystagehistory (opportunityid, fromstageid, tostageid, changedat, changedbyusername, reason, createdat, updatedat)
SELECT
  (SELECT id FROM opportunity WHERE name = 'Adidas — Linha Lifestyle Casual'),
  (SELECT id FROM commercialpipelinestage WHERE name = 'Lead'),
  (SELECT id FROM commercialpipelinestage WHERE name = 'Negociação'),
  NOW() - INTERVAL '30 days', 'Diego Oliveira', 'Avançou rápido, cliente urgente',
  NOW() - INTERVAL '30 days', NOW() - INTERVAL '30 days';

INSERT INTO opportunitystagehistory (opportunityid, fromstageid, tostageid, changedat, changedbyusername, reason, createdat, updatedat)
SELECT
  (SELECT id FROM opportunity WHERE name = 'Adidas — Linha Lifestyle Casual'),
  (SELECT id FROM commercialpipelinestage WHERE name = 'Negociação'),
  (SELECT id FROM commercialpipelinestage WHERE name = 'Perdida'),
  NOW() - INTERVAL '10 days', 'Diego Oliveira', 'Preço maior que concorrente direto',
  NOW() - INTERVAL '10 days', NOW() - INTERVAL '10 days';

-- =====================================================================
-- 8. COMENTÁRIOS NAS OPORTUNIDADES (timeline de atividades)
-- =====================================================================
INSERT INTO opportunitycomment (opportunityid, authorname, body, createdat, updatedat)
SELECT
  (SELECT id FROM opportunity WHERE name = 'Magalu — Esquenta Black Friday'),
  'Diego Oliveira',
  'Cliente pediu para incluir 2 creators do Sul. Avaliando disponibilidade.',
  NOW() - INTERVAL '7 days', NOW() - INTERVAL '7 days';

INSERT INTO opportunitycomment (opportunityid, authorname, body, createdat, updatedat)
SELECT
  (SELECT id FROM opportunity WHERE name = 'Magalu — Esquenta Black Friday'),
  'Ana Silva',
  'Mariana Costa e Lucas Oliveira topam. Já fiz alinhamento de cachê.',
  NOW() - INTERVAL '4 days', NOW() - INTERVAL '4 days';

INSERT INTO opportunitycomment (opportunityid, authorname, body, createdat, updatedat)
SELECT
  (SELECT id FROM opportunity WHERE name = 'iFood Regional Sul — campanha aniversário'),
  'Bruno Costa',
  'Cliente quer trocar reels por carrossel em 2 entregas. Vou recalcular.',
  NOW() - INTERVAL '6 days', NOW() - INTERVAL '6 days';

INSERT INTO opportunitycomment (opportunityid, authorname, body, createdat, updatedat)
SELECT
  (SELECT id FROM opportunity WHERE name = 'iFood Regional Sul — campanha aniversário'),
  'Bruno Costa',
  'Recalculei. Diferença de R$ 8.000 a menos. Cliente aprovou no WhatsApp.',
  NOW() - INTERVAL '2 days', NOW() - INTERVAL '2 days';

INSERT INTO opportunitycomment (opportunityid, authorname, body, createdat, updatedat)
SELECT
  (SELECT id FROM opportunity WHERE name = 'Natura — Linha Tukum Verão'),
  'Carla Mendes',
  'Recebi briefing. Os creators alvo são todos de cosméticos/beleza. Avaliando perfis.',
  NOW() - INTERVAL '8 days', NOW() - INTERVAL '8 days';

INSERT INTO opportunitycomment (opportunityid, authorname, body, createdat, updatedat)
SELECT
  (SELECT id FROM opportunity WHERE name = 'Adidas Tênis Performance — campanha 2026'),
  'Ana Silva',
  'Primeiro contato amanhã às 10h, vou levar deck institucional.',
  NOW() - INTERVAL '3 days', NOW() - INTERVAL '3 days';

-- =====================================================================
-- 9. FOLLOW-UPS
-- =====================================================================
INSERT INTO opportunityfollowup (opportunityid, subject, dueat, notes, iscompleted, createdat, updatedat)
SELECT
  (SELECT id FROM opportunity WHERE name = 'Adidas Tênis Performance — campanha 2026'),
  'Ligar para Bruno Martins após apresentação',
  NOW() + INTERVAL '2 days',
  'Confirmar interesse e pedir referência de campanhas anteriores',
  false,
  NOW() - INTERVAL '3 days', NOW() - INTERVAL '3 days';

INSERT INTO opportunityfollowup (opportunityid, subject, dueat, notes, iscompleted, createdat, updatedat)
SELECT
  (SELECT id FROM opportunity WHERE name = 'Magalu — Esquenta Black Friday'),
  'Enviar proposta revisada',
  NOW() - INTERVAL '1 day',
  'Cliente pediu prazo de 7 dias. Proposta vence em duas semanas.',
  false,
  NOW() - INTERVAL '5 days', NOW() - INTERVAL '5 days';

INSERT INTO opportunityfollowup (opportunityid, subject, dueat, notes, iscompleted, completedat, createdat, updatedat)
SELECT
  (SELECT id FROM opportunity WHERE name = 'iFood Regional Sul — campanha aniversário'),
  'Recalcular escopo com 2 carrosséis',
  NOW() - INTERVAL '3 days',
  'Substituir 2 reels por 2 carrosséis',
  true, NOW() - INTERVAL '2 days',
  NOW() - INTERVAL '6 days', NOW() - INTERVAL '2 days';

-- =====================================================================
-- 10. NEGOCIAÇÕES + APROVAÇÕES
-- =====================================================================
WITH neg AS (
  INSERT INTO opportunitynegotiation (opportunityid, title, amount, status, negotiatedat, notes, createdat, updatedat)
  VALUES (
    (SELECT id FROM opportunity WHERE name = 'iFood Regional Sul — campanha aniversário'),
    'Negociação ajuste de escopo',
    87000.00, 2, NOW() - INTERVAL '5 days',
    'Reduzir escopo em 8k mantendo 80% das entregas',
    NOW() - INTERVAL '5 days', NOW() - INTERVAL '5 days'
  )
  RETURNING id
)
INSERT INTO opportunityapprovalrequest (opportunitynegotiationid, approvaltype, status, reason, requestedbyusername, requestedat, createdat, updatedat)
SELECT id, 1, 1, 'Solicitando aprovação para desconto de 8% no valor estimado', 'Bruno Costa', NOW() - INTERVAL '5 days', NOW() - INTERVAL '5 days', NOW() - INTERVAL '5 days'
FROM neg;

-- =====================================================================
-- 11. PROPOSTAS
-- =====================================================================
-- Proposta em RASCUNHO para Magalu (Diego)
WITH p1 AS (
  INSERT INTO proposal (opportunityid, name, description, status, validityuntil, totalvalue, internalownerid, internalownername, notes, createdat, updatedat)
  VALUES (
    (SELECT id FROM opportunity WHERE name = 'Magalu — Esquenta Black Friday'),
    'Esquenta Black Friday — Pacote Performance v1',
    'Pacote inicial com foco em conversão pré-Black Friday',
    1, NOW() + INTERVAL '14 days', 0,
    (SELECT id FROM commercialresponsible WHERE name = 'Diego Oliveira'),
    'Diego Oliveira',
    'Versão inicial, ainda não enviada ao cliente',
    NOW() - INTERVAL '7 days', NOW() - INTERVAL '7 days'
  )
  RETURNING id
)
INSERT INTO proposalitem (proposalid, description, quantity, unitprice, deliverydeadline, status, observations, creatorid, createdat, updatedat)
SELECT id, 'Reels patrocinado 30-60s', 3, 8000.00, NOW() + INTERVAL '14 days', 1, 'Roteiro com aprovação prévia', (SELECT id FROM creator WHERE name = 'Mariana Costa'), NOW() - INTERVAL '7 days', NOW() - INTERVAL '7 days' FROM p1
UNION ALL
SELECT id, 'Stories com link/sticker', 4, 1500.00, NOW() + INTERVAL '7 days', 1, NULL, (SELECT id FROM creator WHERE name = 'Lucas Oliveira'), NOW() - INTERVAL '7 days', NOW() - INTERVAL '7 days' FROM p1
UNION ALL
SELECT id, 'Carrossel feed (5 imagens)', 2, 3500.00, NOW() + INTERVAL '21 days', 1, 'Direção de arte alinhada com manual', (SELECT id FROM creator WHERE name = 'Camila Ferreira'), NOW() - INTERVAL '7 days', NOW() - INTERVAL '7 days' FROM p1;

UPDATE proposal SET totalvalue = (SELECT COALESCE(SUM(quantity * unitprice), 0) FROM proposalitem WHERE proposalid = proposal.id)
WHERE name = 'Esquenta Black Friday — Pacote Performance v1';

-- Proposta ENVIADA para iFood (Bruno) — vai gerar ProposalVersion v1
WITH p2 AS (
  INSERT INTO proposal (opportunityid, name, description, status, validityuntil, totalvalue, internalownerid, internalownername, notes, createdat, updatedat)
  VALUES (
    (SELECT id FROM opportunity WHERE name = 'iFood Regional Sul — campanha aniversário'),
    'iFood Regional Sul — Pacote Aniversário',
    'Reativação de clientes na região Sul, 4 creators locais',
    2, NOW() + INTERVAL '20 days', 0,
    (SELECT id FROM commercialresponsible WHERE name = 'Bruno Costa'),
    'Bruno Costa',
    'Enviada após ajuste de escopo aprovado pelo cliente',
    NOW() - INTERVAL '4 days', NOW() - INTERVAL '2 days'
  )
  RETURNING id
)
INSERT INTO proposalitem (proposalid, description, quantity, unitprice, deliverydeadline, status, creatorid, createdat, updatedat)
SELECT id, 'Reels patrocinado Curitiba', 1, 12000.00, NOW() + INTERVAL '14 days', 1, (SELECT id FROM creator WHERE name = 'Pedro Henrique'), NOW() - INTERVAL '4 days', NOW() - INTERVAL '4 days' FROM p2
UNION ALL
SELECT id, 'Stories live durante o evento', 6, 1200.00, NOW() + INTERVAL '7 days', 1, (SELECT id FROM creator WHERE name = 'Pedro Henrique'), NOW() - INTERVAL '4 days', NOW() - INTERVAL '4 days' FROM p2
UNION ALL
SELECT id, 'Carrossel review (2 creators)', 2, 4000.00, NOW() + INTERVAL '14 days', 1, (SELECT id FROM creator WHERE name = 'Rafael Santos'), NOW() - INTERVAL '4 days', NOW() - INTERVAL '4 days' FROM p2
UNION ALL
SELECT id, 'TikTok review formato vertical', 2, 6500.00, NOW() + INTERVAL '21 days', 1, (SELECT id FROM creator WHERE name = 'Beatriz Souza'), NOW() - INTERVAL '4 days', NOW() - INTERVAL '4 days' FROM p2;

UPDATE proposal SET totalvalue = (SELECT COALESCE(SUM(quantity * unitprice), 0) FROM proposalitem WHERE proposalid = proposal.id)
WHERE name = 'iFood Regional Sul — Pacote Aniversário';

-- ProposalVersion v1 da proposta iFood
INSERT INTO proposalversion (proposalid, versionnumber, name, description, totalvalue, validityuntil, snapshotjson, sentat, sentbyusername, createdat, updatedat)
SELECT
  p.id, 1, p.name, p.description, p.totalvalue, p.validityuntil,
  json_build_object(
    'proposalId', p.id,
    'name', p.name,
    'description', p.description,
    'totalValue', p.totalvalue,
    'validityUntil', p.validityuntil,
    'notes', p.notes,
    'items', (
      SELECT json_agg(json_build_object(
        'id', pi.id, 'description', pi.description,
        'quantity', pi.quantity, 'unitPrice', pi.unitprice,
        'total', pi.quantity * pi.unitprice,
        'deliveryDeadline', pi.deliverydeadline,
        'status', pi.status
      ))
      FROM proposalitem pi WHERE pi.proposalid = p.id
    )
  )::text,
  NOW() - INTERVAL '2 days', 'Bruno Costa',
  NOW() - INTERVAL '2 days', NOW() - INTERVAL '2 days'
FROM proposal p WHERE p.name = 'iFood Regional Sul — Pacote Aniversário';

-- Histórico de status da proposta iFood
INSERT INTO proposalstatushistory (proposalid, fromstatus, tostatus, changedat, changedbyusername, reason, createdat, updatedat)
SELECT id, NULL, 1, createdat, 'Bruno Costa', 'Proposta criada', createdat, createdat
FROM proposal WHERE name = 'iFood Regional Sul — Pacote Aniversário';

INSERT INTO proposalstatushistory (proposalid, fromstatus, tostatus, changedat, changedbyusername, reason, createdat, updatedat)
SELECT id, 1, 2, NOW() - INTERVAL '2 days', 'Bruno Costa', 'Enviada ao cliente', NOW() - INTERVAL '2 days', NOW() - INTERVAL '2 days'
FROM proposal WHERE name = 'iFood Regional Sul — Pacote Aniversário';

-- ShareLink ativo para a proposta iFood
INSERT INTO proposalsharelink (proposalid, token, expiresat, createdbyusername, viewcount, createdat, updatedat)
SELECT id, 'demo-token-ifood-' || md5(random()::text)::text, NOW() + INTERVAL '30 days', 'Bruno Costa', 0, NOW() - INTERVAL '2 days', NOW() - INTERVAL '2 days'
FROM proposal WHERE name = 'iFood Regional Sul — Pacote Aniversário';

-- Proposta APROVADA para Starbucks (já fechou) — total 68k
WITH p3 AS (
  INSERT INTO proposal (opportunityid, name, description, status, validityuntil, totalvalue, internalownerid, internalownername, notes, createdat, updatedat)
  VALUES (
    (SELECT id FROM opportunity WHERE name = 'Starbucks — Bebidas de Inverno'),
    'Starbucks Inverno 2026 — Pack 1',
    'Cardápio sazonal com 3 creators de food',
    4, NOW() + INTERVAL '5 days', 0,
    (SELECT id FROM commercialresponsible WHERE name = 'Ana Silva'),
    'Ana Silva',
    'Aprovada e fechada como ganha',
    NOW() - INTERVAL '15 days', NOW() - INTERVAL '5 days'
  )
  RETURNING id
)
INSERT INTO proposalitem (proposalid, description, quantity, unitprice, deliverydeadline, status, creatorid, createdat, updatedat)
SELECT id, 'Review de bebida no Reels', 3, 7500.00, NOW() + INTERVAL '14 days', 2, (SELECT id FROM creator WHERE name = 'Mariana Costa'), NOW() - INTERVAL '15 days', NOW() - INTERVAL '15 days' FROM p3
UNION ALL
SELECT id, 'Carrossel "experiência" no feed', 2, 4500.00, NOW() + INTERVAL '21 days', 2, (SELECT id FROM creator WHERE name = 'Camila Ferreira'), NOW() - INTERVAL '15 days', NOW() - INTERVAL '15 days' FROM p3
UNION ALL
SELECT id, 'Stories degustação', 4, 1200.00, NOW() + INTERVAL '7 days', 2, (SELECT id FROM creator WHERE name = 'Pedro Henrique'), NOW() - INTERVAL '15 days', NOW() - INTERVAL '15 days' FROM p3
UNION ALL
SELECT id, 'TikTok recap mensal', 1, 4000.00, NOW() + INTERVAL '28 days', 2, (SELECT id FROM creator WHERE name = 'Beatriz Souza'), NOW() - INTERVAL '15 days', NOW() - INTERVAL '15 days' FROM p3;

UPDATE proposal SET totalvalue = (SELECT COALESCE(SUM(quantity * unitprice), 0) FROM proposalitem WHERE proposalid = proposal.id)
WHERE name = 'Starbucks Inverno 2026 — Pack 1';

INSERT INTO proposalstatushistory (proposalid, fromstatus, tostatus, changedat, changedbyusername, reason, createdat, updatedat)
SELECT id, NULL, 1, createdat, 'Ana Silva', 'Proposta criada', createdat, createdat
FROM proposal WHERE name = 'Starbucks Inverno 2026 — Pack 1';

INSERT INTO proposalstatushistory (proposalid, fromstatus, tostatus, changedat, changedbyusername, reason, createdat, updatedat)
SELECT id, 1, 2, NOW() - INTERVAL '12 days', 'Ana Silva', 'Enviada ao cliente', NOW() - INTERVAL '12 days', NOW() - INTERVAL '12 days'
FROM proposal WHERE name = 'Starbucks Inverno 2026 — Pack 1';

INSERT INTO proposalstatushistory (proposalid, fromstatus, tostatus, changedat, changedbyusername, reason, createdat, updatedat)
SELECT id, 2, 4, NOW() - INTERVAL '8 days', NULL, 'Cliente aprovou via email', NOW() - INTERVAL '8 days', NOW() - INTERVAL '8 days'
FROM proposal WHERE name = 'Starbucks Inverno 2026 — Pack 1';

-- ProposalVersion v1 da Starbucks
INSERT INTO proposalversion (proposalid, versionnumber, name, description, totalvalue, validityuntil, snapshotjson, sentat, sentbyusername, createdat, updatedat)
SELECT
  p.id, 1, p.name, p.description, p.totalvalue, p.validityuntil,
  json_build_object(
    'proposalId', p.id, 'name', p.name, 'totalValue', p.totalvalue,
    'items', (SELECT json_agg(json_build_object(
      'id', pi.id, 'description', pi.description,
      'quantity', pi.quantity, 'unitPrice', pi.unitprice,
      'total', pi.quantity * pi.unitprice,
      'deliveryDeadline', pi.deliverydeadline
    )) FROM proposalitem pi WHERE pi.proposalid = p.id)
  )::text,
  NOW() - INTERVAL '12 days', 'Ana Silva',
  NOW() - INTERVAL '12 days', NOW() - INTERVAL '12 days'
FROM proposal p WHERE p.name = 'Starbucks Inverno 2026 — Pack 1';

-- ShareLink revogado de exemplo
INSERT INTO proposalsharelink (proposalid, token, revokedat, createdbyusername, viewcount, lastviewedat, createdat, updatedat)
SELECT id, 'demo-token-revoked-' || md5(random()::text), NOW() - INTERVAL '6 days', 'Ana Silva', 4, NOW() - INTERVAL '7 days', NOW() - INTERVAL '12 days', NOW() - INTERVAL '6 days'
FROM proposal WHERE name = 'Starbucks Inverno 2026 — Pack 1';

COMMIT;

-- Resumo:
SELECT 'brand' AS tabela, COUNT(*) AS registros FROM brand
UNION ALL SELECT 'creator', COUNT(*) FROM creator
UNION ALL SELECT 'commercialresponsible', COUNT(*) FROM commercialresponsible
UNION ALL SELECT 'opportunity', COUNT(*) FROM opportunity
UNION ALL SELECT 'opportunitystagehistory', COUNT(*) FROM opportunitystagehistory
UNION ALL SELECT 'opportunitycomment', COUNT(*) FROM opportunitycomment
UNION ALL SELECT 'opportunityfollowup', COUNT(*) FROM opportunityfollowup
UNION ALL SELECT 'opportunitynegotiation', COUNT(*) FROM opportunitynegotiation
UNION ALL SELECT 'opportunityapprovalrequest', COUNT(*) FROM opportunityapprovalrequest
UNION ALL SELECT 'proposal', COUNT(*) FROM proposal
UNION ALL SELECT 'proposalitem', COUNT(*) FROM proposalitem
UNION ALL SELECT 'proposalversion', COUNT(*) FROM proposalversion
UNION ALL SELECT 'proposalstatushistory', COUNT(*) FROM proposalstatushistory
UNION ALL SELECT 'proposalsharelink', COUNT(*) FROM proposalsharelink
UNION ALL SELECT 'proposalblock', COUNT(*) FROM proposalblock
UNION ALL SELECT 'proposaltemplate', COUNT(*) FROM proposaltemplate
UNION ALL SELECT 'proposaltemplateitem', COUNT(*) FROM proposaltemplateitem
ORDER BY tabela;
