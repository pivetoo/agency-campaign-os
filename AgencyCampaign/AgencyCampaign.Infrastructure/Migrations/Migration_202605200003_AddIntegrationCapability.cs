using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605200003)]
    public sealed class Migration_202605200003_AddIntegrationCapability : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                CREATE TABLE IF NOT EXISTS integrationcapability (
                    id             BIGSERIAL PRIMARY KEY,
                    intentkey      VARCHAR(120) NOT NULL,
                    connectorid    BIGINT NOT NULL,
                    isactive       BOOLEAN NOT NULL DEFAULT TRUE,
                    createdat      TIMESTAMPTZ NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
                    updatedat      TIMESTAMPTZ NULL
                );
            ");

            Execute.Sql("CREATE UNIQUE INDEX IF NOT EXISTS ux_integrationcapability_intentkey ON integrationcapability(intentkey);");

            Execute.Sql(@"
                INSERT INTO integrationcapability (intentkey, connectorid, isactive, createdat, updatedat)
                SELECT DISTINCT ON (trigger) trigger, connectorid, isactive, createdat, COALESCE(updatedat, createdat)
                FROM automation
                WHERE triggertype = 2
                ORDER BY trigger, updatedat DESC NULLS LAST, createdat DESC
                ON CONFLICT (intentkey) DO NOTHING;
            ");

            Execute.Sql("DELETE FROM automation WHERE triggertype = 2;");
        }

        public override void Down()
        {
            Execute.Sql(@"
                INSERT INTO automation (name, trigger, triggertype, connectorid, pipelineid, variablemappingjson, isactive, createdat, updatedat)
                SELECT 'Padrão para ' || intentkey, intentkey, 2, connectorid, 0, '{}', isactive, createdat, COALESCE(updatedat, createdat)
                FROM integrationcapability;
            ");

            Execute.Sql("DROP INDEX IF EXISTS ux_integrationcapability_intentkey;");
            Execute.Sql("DROP TABLE IF EXISTS integrationcapability;");
        }
    }
}
