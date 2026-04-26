INSERT INTO commercial_responsible (name, email, phone, notes, isactive, createdat, updatedat) VALUES
('Ana Silva', 'ana.silva@agency.com', '(11) 98765-4321', 'Responsável pelo setor de moda e lifestyle', true, NOW(), NOW()),
('Bruno Costa', 'bruno.costa@agency.com', '(11) 97654-3210', 'Foco em tecnologia e startups', true, NOW(), NOW()),
('Carla Mendes', 'carla.mendes@agency.com', '(11) 96543-2109', 'Especialista em food e bebidas', true, NOW(), NOW()),
('Diego Oliveira', 'diego.oliveira@agency.com', '(11) 95432-1098', 'Atua com grandes varejistas', true, NOW(), NOW()),
('Elisa Rocha', 'elisa.rocha@agency.com', '(11) 94321-0987', 'Responsável por cosméticos e beleza', true, NOW(), NOW()),
('Fernando Lima', 'fernando.lima@agency.com', '(11) 93210-9876', 'Foco em campanhas de performance digital', true, NOW(), NOW()),
('Gabriela Souza', 'gabriela.souza@agency.com', '(11) 92109-8765', 'Atuação em influenciadores e creators', true, NOW(), NOW()),
('Henrique Alves', 'henrique.alves@agency.com', '(11) 91098-7654', 'Especialista em mídia paga e tráfego', true, NOW(), NOW()),
('Isabela Martins', 'isabela.martins@agency.com', '(11) 99876-5432', 'Responsável por contas enterprise', true, NOW(), NOW()),
('João Pereira', 'joao.pereira@agency.com', '(11) 98765-1234', 'Atuação em campanhas regionais', true, NOW(), NOW()),
('Karina Duarte', 'karina.duarte@agency.com', '(11) 97654-2345', 'Especialista em branding e posicionamento', true, NOW(), NOW()),
('Lucas Fernandes', 'lucas.fernandes@agency.com', '(11) 96543-3456', 'Foco em estratégias omnichannel', true, NOW(), NOW()),
('Mariana Teixeira', 'mariana.teixeira@agency.com', '(11) 95432-4567', 'Responsável por campanhas de alto impacto', true, NOW(), NOW()),
('Rafael Nogueira', 'rafael.nogueira@agency.com', '(11) 94321-5678', 'Especialista em análise de dados e BI', true, NOW(), NOW()),
('Tatiane Carvalho', 'tatiane.carvalho@agency.com', '(11) 93210-6789', 'Atuação em campanhas sazonais e eventos', true, NOW(), NOW());

INSERT INTO brand (name, tradename, document, contactname, contactemail, notes, isactive, createdat, updatedat) VALUES
('Nike Brasil', 'Nike', '12.345.678/0001-90', 'João Silva', 'joao.silva@nike.com.br', 'Marca global de artigos esportivos', true, NOW(), NOW()),
('Coca-Cola', 'Coca-Cola', '23.456.789/0001-01', 'Maria Santos', 'maria.santos@coca-cola.com', 'Atuação forte em bebidas e marketing massivo', true, NOW(), NOW()),
('Samsung', 'Samsung', '34.567.890/0001-12', 'Pedro Lima', 'pedro.lima@samsung.com', 'Tecnologia e eletrônicos de consumo', true, NOW(), NOW()),
('O Boticário', 'Boticário', '45.678.901/0001-23', 'Ana Paula', 'ana.paula@oboticario.com.br', 'Segmento de beleza e cosméticos', true, NOW(), NOW()),
('Itaú', 'Itaú', '56.789.012/0001-34', 'Carlos Eduardo', 'carlos.eduardo@itau.com.br', 'Banco com forte presença digital', true, NOW(), NOW()),
('Ambev', 'Ambev', '67.890.123/0001-45', 'Fernanda Torres', 'fernanda.torres@ambev.com.br', 'Bebidas e campanhas de grande escala', true, NOW(), NOW()),
('Magazine Luiza', 'Magalu', '78.901.234/0001-56', 'Ricardo Gomes', 'ricardo.gomes@magazineluiza.com.br', 'Varejo omnichannel', true, NOW(), NOW()),
('Netshoes', 'Netshoes', '89.012.345/0001-67', 'Juliana Prado', 'juliana.prado@netshoes.com.br', 'E-commerce esportivo', true, NOW(), NOW()),
('Adidas Brasil', 'Adidas', '90.123.456/0001-78', 'Bruno Martins', 'bruno.martins@adidas.com.br', 'Concorrente direto da Nike no segmento esportivo', true, NOW(), NOW()),
('PepsiCo', 'Pepsi', '11.234.567/0001-89', 'Camila Ferreira', 'camila.ferreira@pepsico.com', 'Bebidas e snacks', true, NOW(), NOW()),
('LG Electronics', 'LG', '22.345.678/0001-90', 'Daniel Ribeiro', 'daniel.ribeiro@lg.com', 'Eletrônicos e eletrodomésticos', true, NOW(), NOW()),
('Natura', 'Natura', '33.456.789/0001-01', 'Eduarda Lopes', 'eduarda.lopes@natura.net', 'Cosméticos sustentáveis', true, NOW(), NOW()),
('Bradesco', 'Bradesco', '44.567.890/0001-12', 'Felipe Carvalho', 'felipe.carvalho@bradesco.com.br', 'Banco com forte atuação nacional', true, NOW(), NOW()),
('Heineken Brasil', 'Heineken', '55.678.901/0001-23', 'Gabriel Alves', 'gabriel.alves@heineken.com.br', 'Cervejaria premium', true, NOW(), NOW()),
('Shopee Brasil', 'Shopee', '66.789.012/0001-34', 'Heloisa Costa', 'heloisa.costa@shopee.com', 'Marketplace com forte presença mobile', true, NOW(), NOW()),
('Mercado Livre', 'Mercado Livre', '77.890.123/0001-45', 'Igor Mendes', 'igor.mendes@mercadolivre.com.br', 'Marketplace líder na América Latina', true, NOW(), NOW()),
('Riachuelo', 'Riachuelo', '88.901.234/0001-56', 'Julio Cesar', 'julio.cesar@riachuelo.com.br', 'Moda e varejo nacional', true, NOW(), NOW()),
('XP Investimentos', 'XP', '99.012.345/0001-67', 'Larissa Rocha', 'larissa.rocha@xpi.com.br', 'Investimentos e mercado financeiro', true, NOW(), NOW());

INSERT INTO creator (
  name, email, phone, document, pixkey, isactive, createdat, updatedat,
  stagename, primaryniche, city, state, notes, defaultagencyfeepercent
) VALUES
('Helena Prado', 'helena.prado@creator.com', '(11) 91123-0001', '111.222.333-01', 'helena.prado@creator.com', true, NOW(), NOW(), 'Hel Prado', 'Moda e lifestyle', 'São Paulo', 'SP', 'Creator fictícia focada em conteúdo de moda casual e tendências urbanas.', 12.50),
('Arthur Pires', 'arthur.pires@creator.com', '(21) 92234-0002', '222.333.444-02', 'arthur.pires@creator.com', true, NOW(), NOW(), 'Arthur Play', 'Games e tecnologia', 'Rio de Janeiro', 'RJ', 'Creator fictício com foco em games, reviews de gadgets e lives patrocinadas.', 15.00),
('Beatriz Moreira', 'beatriz.moreira@creator.com', '(31) 93345-0003', '333.444.555-03', 'beatriz.moreira@creator.com', true, NOW(), NOW(), 'Bia Moreira', 'Beleza e skincare', 'Belo Horizonte', 'MG', 'Perfil fictício voltado para tutoriais de beleza, autocuidado e produtos de skincare.', 13.75),
('Miguel Tavares', 'miguel.tavares@creator.com', '(41) 94456-0004', '444.555.666-04', 'miguel.tavares@creator.com', true, NOW(), NOW(), 'Migs Tavares', 'Fitness e saúde', 'Curitiba', 'PR', 'Creator fictício especializado em treinos rápidos, rotina saudável e suplementação.', 10.00),
('Clara Azevedo', 'clara.azevedo@creator.com', '(51) 95567-0005', '555.666.777-05', 'clara.azevedo@creator.com', true, NOW(), NOW(), 'Clara Az', 'Food e receitas', 'Porto Alegre', 'RS', 'Creator fictícia com foco em receitas práticas, restaurantes e experiências gastronômicas.', 11.50),
('Rafael Goulart', 'rafael.goulart@creator.com', '(61) 96678-0006', '666.777.888-06', 'rafael.goulart@creator.com', true, NOW(), NOW(), 'Rafa Goulart', 'Finanças pessoais', 'Brasília', 'DF', 'Creator fictício focado em educação financeira, investimentos básicos e organização pessoal.', 16.00),
('Sofia Peixoto', 'sofia.peixoto@creator.com', '(71) 97789-0007', '777.888.999-07', 'sofia.peixoto@creator.com', true, NOW(), NOW(), 'Sofi Peixoto', 'Viagem e turismo', 'Salvador', 'BA', 'Creator fictícia voltada para roteiros nacionais, hospedagens e experiências culturais.', 14.25),
('Lucas Farias', 'lucas.farias@creator.com', '(81) 98890-0008', '888.999.111-08', 'lucas.farias@creator.com', true, NOW(), NOW(), 'Lu Farias', 'Humor e entretenimento', 'Recife', 'PE', 'Creator fictício com foco em vídeos curtos de humor, trends e campanhas descontraídas.', 12.00),
('Manuela Coelho', 'manuela.coelho@creator.com', '(91) 99901-0009', '999.111.222-09', 'manuela.coelho@creator.com', true, NOW(), NOW(), 'Manu Coelho', 'Casa e decoração', 'Belém', 'PA', 'Creator fictícia especializada em decoração acessível, organização e lifestyle doméstico.', 10.50),
('Gabriel Nascimento', 'gabriel.nascimento@creator.com', '(11) 90012-0010', '101.303.505-10', 'gabriel.nascimento@creator.com', true, NOW(), NOW(), 'Gabe Nascimento', 'Música e cultura pop', 'São Paulo', 'SP', 'Creator fictício com foco em música, lançamentos, cultura pop e eventos.', 15.50),
('Laura Mendonça', 'laura.mendonca@creator.com', '(21) 91123-0011', '202.404.606-11', 'laura.mendonca@creator.com', true, NOW(), NOW(), 'Laura Mendes', 'Maternidade e família', 'Niterói', 'RJ', 'Creator fictícia voltada para rotina familiar, maternidade leve e consumo consciente.', 9.75),
('Pedro Queiroz', 'pedro.queiroz@creator.com', '(31) 92234-0012', '303.505.707-12', 'pedro.queiroz@creator.com', true, NOW(), NOW(), 'Pedro Q', 'Automotivo', 'Contagem', 'MG', 'Creator fictício com foco em carros, manutenção básica, reviews e experiências automotivas.', 13.00),
('Isadora Viana', 'isadora.viana@creator.com', '(41) 93345-0013', '404.606.808-13', 'isadora.viana@creator.com', true, NOW(), NOW(), 'Isa Viana', 'Educação e carreira', 'Londrina', 'PR', 'Creator fictícia focada em estudos, produtividade, carreira e rotina profissional.', 11.25),
('Matheus Cardoso', 'matheus.cardoso@creator.com', '(51) 94456-0014', '505.707.909-14', 'matheus.cardoso@creator.com', true, NOW(), NOW(), 'Math Cardoso', 'Esportes', 'Caxias do Sul', 'RS', 'Creator fictício com foco em esportes, bastidores de eventos e conteúdo motivacional.', 12.75),
('Ana Bezerra', 'ana.bezerra@creator.com', '(61) 95567-0015', '606.808.101-15', 'ana.bezerra@creator.com', true, NOW(), NOW(), 'Ana Bê', 'Pets', 'Goiânia', 'GO', 'Creator fictícia especializada em pets, cuidados diários e produtos para animais.', 10.00);

INSERT INTO campaign (
  brandid, name, description, budget, startsat, endsat,
  isactive, createdat, updatedat,
  objective, briefing, status, internalownername, notes
) VALUES
(22, 'Nike Air Max Day 2025', 'Campanha de lançamento da nova linha Air Max', 250000.00, '2025-03-01', '2025-03-31', true, NOW(), NOW(),
 'Gerar awareness e vendas da nova linha Air Max',
 'Selecionar creators fitness e lifestyle para conteúdos de unboxing, treino e lifestyle urbano. Foco em Instagram e TikTok.',
 2, 'Ana Silva', 'Campanha prioritária Q1'),

(23, 'Coca-Cola Verão', 'Campanha de verão com creators de lifestyle', 180000.00, '2025-01-15', '2025-02-28', true, NOW(), NOW(),
 'Aumentar brand awareness durante o verão',
 'Conteúdos em praias, festas e momentos de lazer. Foco em reels e stories com apelo emocional e jovem.',
 2, 'Bruno Costa', 'Alta exposição em regiões litorâneas'),

(24, 'Samsung Galaxy S25', 'Lançamento do novo smartphone', 320000.00, '2025-02-01', '2025-04-30', true, NOW(), NOW(),
 'Posicionar o S25 como referência em tecnologia',
 'Creators de tecnologia com reviews detalhadas, comparativos e unboxings. Conteúdo para YouTube e Instagram.',
 3, 'Carla Mendes', 'Campanha com creators técnicos'),

(25, 'Boticário Dia das Mães', 'Campanha especial para o Dia das Mães', 150000.00, '2025-04-20', '2025-05-11', true, NOW(), NOW(),
 'Gerar conexão emocional e vendas sazonais',
 'Creators mães com storytelling emocional, reels e conteúdos de presente. Foco em conversão.',
 2, 'Diego Oliveira', 'Campanha sazonal importante'),

(26, 'Itaú Pontos Turbo', 'Campanha de recompensas do cartão', 200000.00, '2025-01-01', '2025-06-30', true, NOW(), NOW(),
 'Aumentar uso do programa de pontos',
 'Creators de finanças explicando benefícios, uso do cartão e vantagens. Conteúdo educativo.',
 3, 'Elisa Rocha', 'Campanha longa duração'),

(27, 'Ambev Festival Experience', 'Campanha para ativação de marca em eventos', 280000.00, '2025-05-01', '2025-07-31', true, NOW(), NOW(),
 'Fortalecer presença em eventos e festivais',
 'Creators de música e lifestyle em festivais. Conteúdo em tempo real e experiências de marca.',
 1, 'Fernando Lima', 'Dependente de calendário de eventos'),

(28, 'Magalu Semana do Consumidor', 'Campanha promocional para varejo', 220000.00, '2025-03-10', '2025-03-20', true, NOW(), NOW(),
 'Aumentar conversão durante período promocional',
 'Creators diversos com foco em ofertas, cupons e links afiliados.',
 2, 'Gabriela Souza', 'Campanha focada em performance'),

(29, 'Netshoes Run Challenge', 'Campanha com creators de esporte', 130000.00, '2025-04-01', '2025-05-15', true, NOW(), NOW(),
 'Engajar público esportivo e corrida',
 'Creators runners com desafios, treinos e reviews de produtos.',
 2, 'Henrique Alves', 'Campanha com desafios semanais'),

(30, 'Adidas Move Forward', 'Campanha de lifestyle esportivo', 260000.00, '2025-06-01', '2025-07-15', true, NOW(), NOW(),
 'Reforçar posicionamento da marca',
 'Creators fitness e lifestyle com foco em superação e performance.',
 1, 'Isabela Martins', 'Concorrência direta com Nike'),

(31, 'Pepsi Music Drops', 'Campanha focada em música e cultura pop', 175000.00, '2025-02-15', '2025-04-15', true, NOW(), NOW(),
 'Engajar público jovem através da música',
 'Creators de música, DJs e influenciadores culturais.',
 2, 'João Pereira', 'Campanha focada em trends'),

(32, 'LG Casa Inteligente', 'Campanha de eletrônicos conectados', 210000.00, '2025-08-01', '2025-09-30', true, NOW(), NOW(),
 'Promover linha de casa inteligente',
 'Creators tech e família mostrando uso real dos produtos.',
 1, 'Karina Duarte', 'Foco em demonstração prática'),

(33, 'Natura Beleza Consciente', 'Campanha de cosméticos sustentáveis', 190000.00, '2025-05-05', '2025-06-20', true, NOW(), NOW(),
 'Promover sustentabilidade e beleza natural',
 'Creators de beleza com foco em ingredientes naturais.',
 2, 'Lucas Fernandes', 'Campanha institucional'),

(34, 'Bradesco Conta Digital', 'Campanha de produtos digitais', 240000.00, '2025-07-01', '2025-08-31', true, NOW(), NOW(),
 'Aumentar abertura de contas digitais',
 'Creators explicando facilidade e benefícios.',
 1, 'Mariana Teixeira', 'Foco em aquisição'),

(35, 'Heineken Green Nights', 'Campanha premium de eventos', 300000.00, '2025-09-01', '2025-10-31', true, NOW(), NOW(),
 'Fortalecer branding premium',
 'Creators em eventos exclusivos e experiências premium.',
 1, 'Rafael Nogueira', 'Campanha premium'),

(36, 'Shopee Ofertas Relâmpago', 'Campanha de marketplace', 230000.00, '2025-11-01', '2025-11-30', true, NOW(), NOW(),
 'Aumentar vendas mobile',
 'Creators com foco em conversão e cupons.',
 2, 'Tatiane Carvalho', 'Campanha altamente promocional'),

(37, 'Mercado Livre Full Experience', 'Campanha de marketplace', 350000.00, '2025-10-01', '2025-12-15', true, NOW(), NOW(),
 'Reforçar confiança e logística',
 'Creators mostrando entrega rápida e variedade.',
 1, 'Ana Silva', 'Campanha institucional + performance'),

(38, 'Riachuelo Coleção Outono', 'Campanha de moda', 160000.00, '2025-03-15', '2025-04-30', true, NOW(), NOW(),
 'Divulgar nova coleção',
 'Creators fashion mostrando looks e tendências.',
 2, 'Bruno Costa', 'Campanha sazonal'),

(39, 'XP Invista no Futuro', 'Campanha educativa de investimentos', 270000.00, '2025-06-15', '2025-08-15', true, NOW(), NOW(),
 'Educar e converter novos investidores',
 'Creators de finanças com conteúdo educativo.',
 1, 'Carla Mendes', 'Campanha educacional');

DO $$
DECLARE
    v_lead_id BIGINT;
    v_qualified_id BIGINT;
    v_proposal_id BIGINT;
    v_negotiation_id BIGINT;
    v_won_id BIGINT;
    v_lost_id BIGINT;
BEGIN
    SELECT id INTO v_lead_id FROM commercialpipelinestage WHERE name = 'Lead' LIMIT 1;
    SELECT id INTO v_qualified_id FROM commercialpipelinestage WHERE name = 'Qualificada' LIMIT 1;
    SELECT id INTO v_proposal_id FROM commercialpipelinestage WHERE name = 'Proposta' LIMIT 1;
    SELECT id INTO v_negotiation_id FROM commercialpipelinestage WHERE name = 'Negociação' LIMIT 1;
    SELECT id INTO v_won_id FROM commercialpipelinestage WHERE name = 'Ganha' LIMIT 1;
    SELECT id INTO v_lost_id FROM commercialpipelinestage WHERE name = 'Perdida' LIMIT 1;

    INSERT INTO opportunity (brandid, name, description, commercialpipelinestageid, estimatedvalue, expectedcloseat, commercialresponsibleid, contactname, contactemail, notes, createdat, updatedat) VALUES
    (1, 'Nike - Lançamento Air Max 2025', 'Cliente demonstra interesse em lançamento com 8 creators de fitness', v_lead_id, 250000.00, '2025-03-15', 1, 'João Silva', 'joao.silva@nike.com.br', 'Primeiro contato realizado em evento', NOW(), NOW()),
    (2, 'Coca-Cola - Campanha Verão', 'Campanha de verão com foco em jovens de 18-25 anos', v_qualified_id, 180000.00, '2025-02-20', 2, 'Maria Santos', 'maria.santos@coca-cola.com', 'Briefing aprovado internamente', NOW(), NOW()),
    (3, 'Samsung - Galaxy S25', 'Lançamento nacional com creators de tech', v_proposal_id, 320000.00, '2025-04-10', 3, 'Pedro Lima', 'pedro.lima@samsung.com', 'Proposta enviada aguardando feedback', NOW(), NOW()),
    (4, 'Boticário - Dia das Mães', 'Campanha emocional com creators mães', v_negotiation_id, 150000.00, '2025-04-25', 4, 'Ana Paula', 'ana.paula@oboticario.com.br', 'Negociando valores de 3 creators top', NOW(), NOW()),
    (5, 'Itaú - Pontos Turbo', 'Programa de recompensas com creators financeiros', v_won_id, 200000.00, '2025-01-30', 5, 'Carlos Eduardo', 'carlos.eduardo@itau.com.br', 'Contrato assinado, campanha em execução', NOW(), NOW()),
    (6, 'Ambev - Carnaval 2025', 'Campanha de carnaval com creators de música', v_lost_id, 120000.00, '2025-02-10', 1, 'Fernanda Torres', 'fernanda.torres@ambev.com.br', 'Cliente optou por agência concorrente', NOW(), NOW()),
    (7, 'Magazine Luiza - Black Friday', 'Campanha de Black Friday com creators diversos', v_lead_id, 400000.00, '2025-11-20', 2, 'Ricardo Gomes', 'ricardo.gomes@magazineluiza.com.br', 'Aguardando definição de budget', NOW(), NOW()),
    (8, 'Netshoes - Copa 2026', 'Campanha pré-copa com creators esportivos', v_qualified_id, 280000.00, '2025-06-15', 3, 'Juliana Prado', 'juliana.prado@netshoes.com.br', 'Reunião de alinhamento realizada', NOW(), NOW());
END $$;

INSERT INTO opportunitynegotiation (opportunityid, title, amount, status, negotiatedat, notes, createdat, updatedat) VALUES
(3, 'Negociação inicial Samsung', 280000.00, 1, NOW(), 'Primeira proposta enviada', NOW(), NOW()),
(3, 'Counter-proposal Samsung', 310000.00, 5, NOW(), 'Cliente pediu ajuste em 2 creators', NOW(), NOW()),
(4, 'Negociação Boticário fase 1', 130000.00, 3, NOW(), 'Valores aprovados pela marca', NOW(), NOW()),
(4, 'Negociação Boticário fase 2', 150000.00, 6, NOW(), 'Ajuste final incluindo 1 creator a mais', NOW(), NOW()),
(7, 'Exploratory Magazine Luiza', 350000.00, 1, NOW(), 'Discussão inicial de escopo', NOW(), NOW()),
(8, 'Netshoes proposal round 1', 260000.00, 2, NOW(), 'Aguardando aprovação do diretor', NOW(), NOW());

INSERT INTO opportunityapprovalrequest (opportunitynegotiationid, approvaltype, status, reason, requestedbyuserid, requestedbyusername, approvedbyuserid, approvedbyusername, requestedat, decidedat, decisionnotes, createdat, updatedat) VALUES
(3, 1, 2, 'Solicitação de desconto de 10% no pacote de creators para fechar com Boticário', 1, 'Ana Silva', 2, 'Bruno Costa', NOW(), NOW(), 'Desconto aprovado para fechamento rápido', NOW(), NOW()),
(4, 3, 1, 'Solicitação de prazo extendido para entrega dos conteúdos', 3, 'Carla Mendes', NULL, NULL, NOW(), NULL, NULL, NOW(), NOW()),
(6, 2, 3, 'Solicitação de margem reduzida para competir com outra agência', 2, 'Bruno Costa', 1, 'Ana Silva', NOW(), NOW(), 'Margem não aprovada, manter valor original', NOW(), NOW());

INSERT INTO opportunityfollowup (opportunityid, subject, dueat, notes, iscompleted, completedat, createdat, updatedat) VALUES
(1, 'Enviar apresentação inicial', '2025-01-20', 'Preparar deck com cases de fitness', true, '2025-01-19', NOW(), NOW()),
(1, 'Reunião de briefing', '2025-01-25', 'Agendar com time criativo', false, NULL, NOW(), NOW()),
(2, 'Aprovação do conceito criativo', '2025-01-18', 'Aguardar feedback do cliente', true, '2025-01-17', NOW(), NOW()),
(3, 'Follow-up proposta enviada', '2025-01-22', 'Ligar para Pedro Lima', false, NULL, NOW(), NOW()),
(4, 'Reunião de fechamento', '2025-01-15', 'Apresentar creators finalizados', true, '2025-01-14', NOW(), NOW()),
(7, 'Definição de budget', '2025-02-01', 'Aguardar aprovação do Q1', false, NULL, NOW(), NOW()),
(8, 'Envio de amostra de creators', '2025-01-28', 'Selecionar 5 creators esportivos', false, NULL, NOW(), NOW());

INSERT INTO proposal (name, description, status, validityuntil, totalvalue, opportunityid, notes, createdat, updatedat) VALUES
('Proposta Samsung Galaxy S25', 'Pacote completo com 10 creators de tech', 2, '2025-02-15', 320000.00, 3, 'Proposta inicial enviada', NOW(), NOW()),
('Proposta Boticário Dia das Mães', 'Campanha emocional com 6 creators mães', 4, '2025-03-01', 150000.00, 4, 'Aprovada pela marca', NOW(), NOW()),
('Proposta Itaú Pontos Turbo', 'Programa com 4 creators financeiros', 6, '2025-01-31', 200000.00, 5, 'Convertida em campanha', NOW(), NOW()),
('Proposta Netshoes Copa 2026', 'Pacote com 8 creators esportivos', 1, '2025-02-28', 280000.00, 8, 'Em elaboração', NOW(), NOW()),
('Proposta Magazine Luiza BF', 'Campanha com 15 creators diversos', 1, '2025-10-31', 400000.00, 7, 'Aguardando definição de escopo', NOW(), NOW());

INSERT INTO proposalitem (proposalid, description, quantity, unitprice, deliverydeadline, status, observations, creatorid) VALUES
(1, 'Reels de unboxing Galaxy S25', 10, 15000.00, '2025-03-10', 1, 'Conteúdo em português, 30-60s', 2),
(1, 'Stories de first impressions', 10, 5000.00, '2025-03-12', 1, 'Mínimo 3 stories por creator', 3),
(2, 'Reels emocionais Dia das Mães', 6, 12000.00, '2025-04-25', 1, 'Foco em histórias reais', 4),
(2, 'Feed posts com produtos', 6, 8000.00, '2025-04-28', 1, 'Fotografia profissional', 5),
(3, 'Vídeos educativos sobre pontos', 4, 25000.00, '2025-02-15', 1, 'Formato 16:9 para YouTube', 6),
(4, 'Reels pré-copa com chuteiras', 8, 18000.00, '2025-06-01', 1, 'Conteúdo em estádios', 7),
(5, 'Live shopping Black Friday', 15, 10000.00, '2025-11-25', 1, 'Lives de 2h cada', 8);

INSERT INTO campaigndeliverable (campaignid, creatorid, title, description, dueat, publishedat, status, grossamount, creatoramount, agencyfeeamount, deliverablekindid, platformid, createdat, updatedat) VALUES
(1, 2, 'Unboxing Air Max 2025', 'Reel mostrando o novo modelo', '2025-03-15', '2025-03-16', 3, 15000.00, 12000.00, 3000.00, 1, 1, NOW(), NOW()),
(1, 3, 'Review completo Air Max', 'Vídeo longo no YouTube', '2025-03-20', NULL, 2, 25000.00, 20000.00, 5000.00, 4, 3, NOW(), NOW()),
(2, 4, 'Coca-Cola no verão', 'Reel de lifestyle na praia', '2025-02-10', '2025-02-11', 3, 18000.00, 14400.00, 3600.00, 1, 1, NOW(), NOW()),
(2, 5, 'Momentos Coca-Cola', 'Stories durante o Carnaval', '2025-02-15', NULL, 1, 8000.00, 6400.00, 1600.00, 2, 1, NOW(), NOW()),
(3, 6, 'Review Galaxy S25', 'Vídeo técnico comparativo', '2025-03-01', '2025-03-02', 3, 30000.00, 24000.00, 6000.00, 4, 3, NOW(), NOW()),
(4, 7, 'Presente Boticário', 'Reel emocional Dia das Mães', '2025-05-05', NULL, 1, 12000.00, 9600.00, 2400.00, 1, 1, NOW(), NOW()),
(5, 8, 'Como acumular pontos Itaú', 'Vídeo educativo', '2025-02-20', '2025-02-21', 3, 20000.00, 16000.00, 4000.00, 4, 3, NOW(), NOW());

UPDATE opportunity SET closedat = '2025-01-30', wonnotes = 'Contrato assinado, campanha em execução' WHERE id = 5;
UPDATE opportunity SET closedat = '2025-02-10', lossreason = 'Cliente optou por agência concorrente' WHERE id = 6;

SELECT 'commercial_responsible' AS table_name, COUNT(*) AS count FROM commercial_responsible
UNION ALL SELECT 'brand', COUNT(*) FROM brand
UNION ALL SELECT 'creator', COUNT(*) FROM creator
UNION ALL SELECT 'commercialpipelinestage', COUNT(*) FROM commercialpipelinestage
UNION ALL SELECT 'opportunity', COUNT(*) FROM opportunity
UNION ALL SELECT 'opportunitynegotiation', COUNT(*) FROM opportunitynegotiation
UNION ALL SELECT 'opportunityapprovalrequest', COUNT(*) FROM opportunityapprovalrequest
UNION ALL SELECT 'opportunityfollowup', COUNT(*) FROM opportunityfollowup
UNION ALL SELECT 'proposal', COUNT(*) FROM proposal
UNION ALL SELECT 'proposalitem', COUNT(*) FROM proposalitem
UNION ALL SELECT 'campaign', COUNT(*) FROM campaign
UNION ALL SELECT 'campaigndeliverable', COUNT(*) FROM campaigndeliverable
UNION ALL SELECT 'platform', COUNT(*) FROM platform
UNION ALL SELECT 'deliverablekind', COUNT(*) FROM deliverablekind;
