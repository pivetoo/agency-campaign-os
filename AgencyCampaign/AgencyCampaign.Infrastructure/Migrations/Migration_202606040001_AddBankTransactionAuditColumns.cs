using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    // Corrige as colunas de auditoria ausentes na tabela banktransaction.
    // A migration original (202605180006) criou a tabela sem createdat/updatedat,
    // que toda Entity do Archon exige - o EF as inclui em todo INSERT/UPDATE.
    // Sem elas, qualquer escrita em banktransaction (importar extrato, casar,
    // desfazer conciliacao) estourava HTTP 500 (column "createdat" does not exist).
    [Migration(202606040001)]
    public sealed class Migration_202606040001_AddBankTransactionAuditColumns : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                ALTER TABLE banktransaction ADD COLUMN IF NOT EXISTS createdat TIMESTAMPTZ NOT NULL DEFAULT NOW();
                ALTER TABLE banktransaction ADD COLUMN IF NOT EXISTS updatedat TIMESTAMPTZ NULL;
            ");
        }

        public override void Down()
        {
            Execute.Sql(@"
                ALTER TABLE banktransaction DROP COLUMN IF EXISTS updatedat;
                ALTER TABLE banktransaction DROP COLUMN IF EXISTS createdat;
            ");
        }
    }
}
