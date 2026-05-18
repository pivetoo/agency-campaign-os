using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605180006)]
    public sealed class Migration_202605180006_AddBankTransaction : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                CREATE TABLE IF NOT EXISTS banktransaction (
                    id BIGSERIAL PRIMARY KEY,
                    accountid BIGINT NOT NULL,
                    externalid VARCHAR(200) NOT NULL,
                    occurredat TIMESTAMPTZ NOT NULL,
                    amount NUMERIC(18, 2) NOT NULL,
                    direction INTEGER NOT NULL,
                    description VARCHAR(500) NOT NULL,
                    category VARCHAR(200) NULL,
                    rawpayload TEXT NULL,
                    financialentryid BIGINT NULL,
                    matchedat TIMESTAMPTZ NULL,
                    matchkind INTEGER NULL,
                    importedat TIMESTAMPTZ NOT NULL DEFAULT NOW()
                );
            ");

            Execute.Sql("CREATE UNIQUE INDEX IF NOT EXISTS ux_banktransaction_account_external ON banktransaction(accountid, externalid);");
            Execute.Sql("CREATE INDEX IF NOT EXISTS ix_banktransaction_financialentryid ON banktransaction(financialentryid);");
            Execute.Sql("CREATE INDEX IF NOT EXISTS ix_banktransaction_occurredat ON banktransaction(occurredat);");

            Execute.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.table_constraints
                        WHERE constraint_name = 'fk_banktransaction_account'
                    ) THEN
                        ALTER TABLE banktransaction
                            ADD CONSTRAINT fk_banktransaction_account
                            FOREIGN KEY (accountid)
                            REFERENCES financialaccount(id);
                    END IF;
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.table_constraints
                        WHERE constraint_name = 'fk_banktransaction_financialentry'
                    ) THEN
                        ALTER TABLE banktransaction
                            ADD CONSTRAINT fk_banktransaction_financialentry
                            FOREIGN KEY (financialentryid)
                            REFERENCES financialentry(id)
                            ON DELETE SET NULL;
                    END IF;
                END $$;
            ");
        }

        public override void Down()
        {
            Execute.Sql("ALTER TABLE banktransaction DROP CONSTRAINT IF EXISTS fk_banktransaction_financialentry;");
            Execute.Sql("ALTER TABLE banktransaction DROP CONSTRAINT IF EXISTS fk_banktransaction_account;");
            Execute.Sql("DROP INDEX IF EXISTS ix_banktransaction_occurredat;");
            Execute.Sql("DROP INDEX IF EXISTS ix_banktransaction_financialentryid;");
            Execute.Sql("DROP INDEX IF EXISTS ux_banktransaction_account_external;");
            Execute.Sql("DROP TABLE IF EXISTS banktransaction;");
        }
    }
}
