using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605180005)]
    public sealed class Migration_202605180005_AddFinancialAccountSyncFields : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                ALTER TABLE financialaccount
                    ADD COLUMN IF NOT EXISTS integrationconnectorid BIGINT NULL,
                    ADD COLUMN IF NOT EXISTS lastsyncedbalance NUMERIC(18, 2) NULL,
                    ADD COLUMN IF NOT EXISTS lastsyncedat TIMESTAMPTZ NULL,
                    ADD COLUMN IF NOT EXISTS syncstatus INTEGER NOT NULL DEFAULT 0;
            ");

            Execute.Sql("CREATE INDEX IF NOT EXISTS ix_financialaccount_integrationconnectorid ON financialaccount(integrationconnectorid);");
        }

        public override void Down()
        {
            Execute.Sql("DROP INDEX IF EXISTS ix_financialaccount_integrationconnectorid;");
            Execute.Sql(@"
                ALTER TABLE financialaccount
                    DROP COLUMN IF EXISTS syncstatus,
                    DROP COLUMN IF EXISTS lastsyncedat,
                    DROP COLUMN IF EXISTS lastsyncedbalance,
                    DROP COLUMN IF EXISTS integrationconnectorid;
            ");
        }
    }
}
