using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    // Estorno (D3b): contrapartida vinculada ao lancamento pago. reversalofentryid aponta da contrapartida
    // para o original; isreversed/reversedat marcam o original. O indice unico parcial garante que cada
    // original tenha no maximo UMA contrapartida (idempotencia do estorno). Coluna nova => indice nao falha.
    [Migration(202606030009)]
    public sealed class Migration_202606030009_AddFinancialEntryReversal : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                ALTER TABLE financialentry ADD COLUMN reversalofentryid BIGINT NULL;
                ALTER TABLE financialentry ADD COLUMN isreversed BOOLEAN NOT NULL DEFAULT FALSE;
                ALTER TABLE financialentry ADD COLUMN reversedat TIMESTAMPTZ NULL;
                CREATE UNIQUE INDEX IF NOT EXISTS uxfinancialentryreversalofentryid
                    ON financialentry (reversalofentryid) WHERE reversalofentryid IS NOT NULL;
            ");
        }

        public override void Down()
        {
            Execute.Sql(@"
                DROP INDEX IF EXISTS uxfinancialentryreversalofentryid;
                ALTER TABLE financialentry DROP COLUMN IF EXISTS reversedat;
                ALTER TABLE financialentry DROP COLUMN IF EXISTS isreversed;
                ALTER TABLE financialentry DROP COLUMN IF EXISTS reversalofentryid;
            ");
        }
    }
}
