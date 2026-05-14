-- ============================================================================
-- SeedDeliverableHistory.sql
-- Popular campanhas + creators + deliverables retroativos para alimentar
-- o gráfico "Receita bruta x Fee da agência" dos últimos 12 meses.
--
-- O dashboard usa CampaignDeliverable.GrossAmount/AgencyFeeAmount agrupados
-- por CreatedAt mensal — então CreatedAt precisa estar espalhado nos meses.
--
-- Idempotente: detecta campaign.name LIKE 'HIST-%' e pula se já populado.
-- ============================================================================

DO $$
DECLARE
    v_month_offset INTEGER;
    v_campaign_id  BIGINT;
    v_campaign_creator_id BIGINT;
    v_month_date   TIMESTAMPTZ;
    v_brand_ids    BIGINT[];
    v_creator_ids  BIGINT[];
    v_brand_id     BIGINT;
    v_creator_id   BIGINT;
    v_campaign_name TEXT;
    v_brand_name   TEXT;
    v_gross_total  NUMERIC;
    v_fee_pct      NUMERIC := 20.00;
    v_kind_reel    BIGINT;
    v_kind_video   BIGINT;
    v_kind_story   BIGINT;
    v_plat_ig      BIGINT;
    v_plat_tk      BIGINT;
    v_plat_yt      BIGINT;
    v_status_done  BIGINT;
BEGIN
    IF EXISTS (SELECT 1 FROM campaign WHERE name LIKE 'HIST-%') THEN
        RAISE NOTICE 'Histórico de deliverables já populado, abortando.';
        RETURN;
    END IF;

    -- Coleta IDs dinâmicos
    SELECT ARRAY(SELECT id FROM brand   ORDER BY id) INTO v_brand_ids;
    SELECT ARRAY(SELECT id FROM creator ORDER BY id) INTO v_creator_ids;
    SELECT id INTO v_kind_reel   FROM deliverablekind WHERE name = 'Reel'  LIMIT 1;
    SELECT id INTO v_kind_video  FROM deliverablekind WHERE name = 'Video' LIMIT 1;
    SELECT id INTO v_kind_story  FROM deliverablekind WHERE name = 'Story' LIMIT 1;
    SELECT id INTO v_plat_ig     FROM platform WHERE name = 'Instagram'    LIMIT 1;
    SELECT id INTO v_plat_tk     FROM platform WHERE name = 'TikTok'       LIMIT 1;
    SELECT id INTO v_plat_yt     FROM platform WHERE name = 'YouTube'      LIMIT 1;
    SELECT id INTO v_status_done FROM campaigncreatorstatus WHERE name ILIKE 'Entregue%' LIMIT 1;

    -- Loop pelos últimos 12 meses (do mais antigo pro mais recente)
    FOR v_month_offset IN REVERSE 12..1 LOOP
        v_month_date := date_trunc('month', NOW() - (v_month_offset || ' months')::INTERVAL) + INTERVAL '5 days';

        -- Rotaciona pelas brands existentes
        v_brand_id := v_brand_ids[((12 - v_month_offset) % array_length(v_brand_ids, 1)) + 1];
        SELECT tradename INTO v_brand_name FROM brand WHERE id = v_brand_id;

        v_campaign_name := format('HIST-%s %s campanha mensal', to_char(v_month_date, 'YYYY-MM'), v_brand_name);

        -- Variação suave de receita: começa em ~180k, oscila +/- 60k, leve crescimento
        v_gross_total := 180000 + ((12 - v_month_offset) * 8000) + ((v_month_offset * 7919) % 60000);

        -- Cria campanha
        INSERT INTO campaign (brandid, name, description, budget, startsat, endsat, status, objective, briefing, internalownername, notes, isactive, createdat, updatedat)
        VALUES (v_brand_id, v_campaign_name, format('Campanha histórica %s', v_brand_name),
                v_gross_total, v_month_date, v_month_date + INTERVAL '20 days', 5,
                'Concluída', 'Conteúdo entregue e publicado', 'Time Comercial', 'Campanha histórica para análise',
                true, v_month_date, v_month_date + INTERVAL '25 days')
        RETURNING id INTO v_campaign_id;

        -- 3 deliverables por campanha (Reel + Story + Video), com 3 creators distintos
        FOR i IN 1..3 LOOP
            v_creator_id := v_creator_ids[(((12 - v_month_offset) * 3 + i) % array_length(v_creator_ids, 1)) + 1];

            DECLARE
                v_deliv_gross   NUMERIC;
                v_deliv_fee     NUMERIC;
                v_deliv_creator NUMERIC;
                v_kind_id       BIGINT;
                v_plat_id       BIGINT;
                v_kind_label    TEXT;
                v_plat_label    TEXT;
            BEGIN
                -- Distribui gross entre 3 deliverables
                v_deliv_gross := CASE i WHEN 1 THEN v_gross_total * 0.45 WHEN 2 THEN v_gross_total * 0.30 ELSE v_gross_total * 0.25 END;
                v_deliv_fee     := round(v_deliv_gross * v_fee_pct / 100, 2);
                v_deliv_creator := v_deliv_gross - v_deliv_fee;

                v_kind_id    := CASE i WHEN 1 THEN v_kind_video WHEN 2 THEN v_kind_reel ELSE v_kind_story END;
                v_kind_label := CASE i WHEN 1 THEN 'Vídeo' WHEN 2 THEN 'Reel' ELSE 'Story' END;
                v_plat_id    := CASE i WHEN 1 THEN v_plat_yt WHEN 2 THEN v_plat_ig ELSE v_plat_tk END;
                v_plat_label := CASE i WHEN 1 THEN 'YouTube' WHEN 2 THEN 'Instagram' ELSE 'TikTok' END;

                -- Cria CampaignCreator
                INSERT INTO campaigncreator (campaignid, creatorid, campaigncreatorstatusid, agreedamount, agencyfeepercent, agencyfeeamount, confirmedat, notes, createdat, updatedat)
                VALUES (v_campaign_id, v_creator_id, v_status_done, v_deliv_gross, v_fee_pct, v_deliv_fee,
                        v_month_date + INTERVAL '2 days', 'Histórico — entregue', v_month_date, v_month_date + INTERVAL '15 days')
                RETURNING id INTO v_campaign_creator_id;

                -- Cria Deliverable (publicado)
                INSERT INTO campaigndeliverable (campaignid, campaigncreatorid, deliverablekindid, platformid, title, description, dueat, publishedat, status, grossamount, creatoramount, agencyfeeamount, publishedurl, createdat, updatedat)
                VALUES (v_campaign_id, v_campaign_creator_id, v_kind_id, v_plat_id,
                        format('%s - %s no %s', v_brand_name, v_kind_label, v_plat_label),
                        format('Entrega histórica de %s', v_brand_name),
                        v_month_date + INTERVAL '10 days', v_month_date + INTERVAL '12 days',
                        4, v_deliv_gross, v_deliv_creator, v_deliv_fee,
                        format('https://%s.com/demo-%s-%s', lower(v_plat_label), to_char(v_month_date, 'YYYYMM'), i),
                        v_month_date, v_month_date + INTERVAL '15 days');
            END;
        END LOOP;
    END LOOP;

    RAISE NOTICE 'Histórico de deliverables populado.';
END $$;


-- Verificação: receita bruta x fee por mês (últimos 12 meses)
SELECT
    TO_CHAR(createdat, 'YYYY-MM') AS mes,
    COUNT(*) AS deliverables,
    SUM(grossamount)     AS receita_bruta,
    SUM(agencyfeeamount) AS fee_agencia,
    ROUND(SUM(agencyfeeamount) / NULLIF(SUM(grossamount), 0) * 100, 1) AS fee_pct
FROM campaigndeliverable
WHERE createdat >= NOW() - INTERVAL '13 months'
GROUP BY TO_CHAR(createdat, 'YYYY-MM')
ORDER BY mes;
