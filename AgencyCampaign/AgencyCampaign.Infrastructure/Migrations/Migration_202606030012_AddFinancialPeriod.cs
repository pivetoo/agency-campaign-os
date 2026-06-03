using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    // Fechamento de periodo mensal (D3c): trava contabil. Um mes fechado bloqueia criar/editar/marcar-pago
    // lancamentos datados nele (back-dating); o estorno segue liberado (lanca no mes aberto corrente).
    [Migration(202606030012)]
    public sealed class Migration_202606030012_AddFinancialPeriod : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                CREATE TABLE IF NOT EXISTS financialperiod (
                    id BIGSERIAL PRIMARY KEY,
                    year INTEGER NOT NULL,
                    month INTEGER NOT NULL,
                    isclosed BOOLEAN NOT NULL DEFAULT false,
                    closedat TIMESTAMPTZ NULL,
                    closedbyuserid BIGINT NULL,
                    reopenedat TIMESTAMPTZ NULL,
                    reopenedbyuserid BIGINT NULL,
                    createdat TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    updatedat TIMESTAMPTZ NULL
                );
            ");

            Execute.Sql("CREATE UNIQUE INDEX IF NOT EXISTS uxfinancialperiodyearmonth ON financialperiod(year, month);");
        }

        public override void Down()
        {
            Execute.Sql("DROP INDEX IF EXISTS uxfinancialperiodyearmonth;");
            Execute.Sql("DROP TABLE IF EXISTS financialperiod;");
        }
    }
}
