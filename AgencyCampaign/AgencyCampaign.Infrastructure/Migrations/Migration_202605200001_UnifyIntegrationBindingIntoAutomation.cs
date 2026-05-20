using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605200001)]
    public sealed class Migration_202605200001_UnifyIntegrationBindingIntoAutomation : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                ALTER TABLE automation
                    ADD COLUMN IF NOT EXISTS triggertype INTEGER NOT NULL DEFAULT 1;

                INSERT INTO automation (name, trigger, triggertype, connectorid, pipelineid, variablemappingjson, isactive, createdat, updatedat)
                SELECT
                    'Padrão para ' || intentkey,
                    intentkey,
                    2,
                    connectorid,
                    pipelineid,
                    '{}',
                    isactive,
                    createdat,
                    COALESCE(updatedat, createdat)
                FROM agencyintegrationbinding;

                DROP TABLE IF EXISTS agencyintegrationbinding;
            ");
        }

        public override void Down()
        {
            Execute.Sql(@"
                CREATE TABLE IF NOT EXISTS agencyintegrationbinding (
                    id                  BIGSERIAL PRIMARY KEY,
                    intentkey           VARCHAR(80) NOT NULL,
                    connectorid         BIGINT NOT NULL,
                    pipelineid          BIGINT NOT NULL,
                    isactive            BOOLEAN NOT NULL DEFAULT TRUE,
                    createdbyuserid     BIGINT NULL,
                    createdbyusername   VARCHAR(150) NULL,
                    createdat           TIMESTAMPTZ NOT NULL,
                    updatedat           TIMESTAMPTZ NULL
                );

                CREATE UNIQUE INDEX IF NOT EXISTS ux_agencyintegrationbinding_intentkey
                    ON agencyintegrationbinding (intentkey);

                INSERT INTO agencyintegrationbinding (intentkey, connectorid, pipelineid, isactive, createdat, updatedat)
                SELECT trigger, connectorid, pipelineid, isactive, createdat, updatedat
                FROM automation
                WHERE triggertype = 2;

                DELETE FROM automation WHERE triggertype = 2;

                ALTER TABLE automation DROP COLUMN IF EXISTS triggertype;
            ");
        }
    }
}
