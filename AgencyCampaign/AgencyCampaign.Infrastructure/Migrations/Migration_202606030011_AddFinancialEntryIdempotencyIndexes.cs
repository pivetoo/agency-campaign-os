using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    // Idempotencia da auto-geracao no BANCO (D5): rede de seguranca contra recebivel/repasse auto-gerado
    // duplicado, alem da trava em memoria (AnyAsync) do FinancialAutoGenerationService. Indices unicos
    // PARCIAIS por origem: recebivel da marca (1) por proposta; repasse de creator (2/2) por entrega.
    //
    // O bloco DO ABORTA com mensagem clara se ja houver duplicata (NAO deleta linha de dinheiro - decisao do
    // dono: corrigir manualmente). Pre-verificado em producao (2026-06-03, via SSH): 0 duplicatas nos 4 bancos
    // agencycampaign*, todos vazios (pre-lancamento), entao a aplicacao e segura.
    [Migration(202606030011)]
    public sealed class Migration_202606030011_AddFinancialEntryIdempotencyIndexes : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                DO $$
                DECLARE dupes INT;
                BEGIN
                    SELECT count(*) INTO dupes FROM (
                        SELECT sourceproposalid FROM financialentry
                        WHERE sourceproposalid IS NOT NULL AND category = 1
                        GROUP BY sourceproposalid HAVING count(*) > 1
                    ) a;
                    IF dupes > 0 THEN
                        RAISE EXCEPTION 'Migration 202606030011 abortada: % recebivel(is) auto-gerado(s) duplicado(s) por sourceproposalid. Deduplicar manualmente antes de aplicar (nao destrutivo).', dupes;
                    END IF;

                    SELECT count(*) INTO dupes FROM (
                        SELECT campaigndeliverableid FROM financialentry
                        WHERE campaigndeliverableid IS NOT NULL AND type = 2 AND category = 2
                        GROUP BY campaigndeliverableid HAVING count(*) > 1
                    ) b;
                    IF dupes > 0 THEN
                        RAISE EXCEPTION 'Migration 202606030011 abortada: % repasse(s) auto-gerado(s) duplicado(s) por campaigndeliverableid. Deduplicar manualmente antes de aplicar (nao destrutivo).', dupes;
                    END IF;
                END $$;

                CREATE UNIQUE INDEX IF NOT EXISTS uxfinancialentryautoreceivable
                    ON financialentry (sourceproposalid) WHERE sourceproposalid IS NOT NULL AND category = 1;
                CREATE UNIQUE INDEX IF NOT EXISTS uxfinancialentryautopayout
                    ON financialentry (campaigndeliverableid) WHERE campaigndeliverableid IS NOT NULL AND type = 2 AND category = 2;
            ");
        }

        public override void Down()
        {
            Execute.Sql(@"
                DROP INDEX IF EXISTS uxfinancialentryautopayout;
                DROP INDEX IF EXISTS uxfinancialentryautoreceivable;
            ");
        }
    }
}
