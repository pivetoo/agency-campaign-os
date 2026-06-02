using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    // Consolidacao do briefing (D5): migra o campo livre legado campaign.briefing para o briefing
    // estruturado (campaignbriefing.keymessage) onde ainda nao existe estrutura, tornando o
    // estruturado a fonte unica de verdade. O campo livre fica deprecado (mantido por seguranca
    // de dados, nao mais consumido pelos documentos).
    [Migration(202606020003)]
    public sealed class Migration_202606020003_BackfillStructuredBriefing : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                INSERT INTO campaignbriefing (campaignid, keymessage)
                SELECT c.id, c.briefing
                FROM campaign c
                WHERE c.briefing IS NOT NULL
                  AND TRIM(c.briefing) <> ''
                  AND NOT EXISTS (SELECT 1 FROM campaignbriefing b WHERE b.campaignid = c.id);");
        }

        public override void Down()
        {
            // Backfill de dados idempotente: nao ha rollback seguro (apagar linhas de briefing
            // poderia destruir conteudo editado depois). Down intencionalmente no-op.
        }
    }
}
