using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605230004)]
    public sealed class Migration_202605230004_AddOpportunityApprovalImpact : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                CREATE TABLE IF NOT EXISTS opportunityapprovalimpact (
                    id BIGSERIAL PRIMARY KEY,
                    opportunityapprovalrequestid BIGINT NOT NULL REFERENCES opportunityapprovalrequest(id) ON DELETE CASCADE,
                    label VARCHAR(60) NOT NULL,
                    value VARCHAR(80) NOT NULL,
                    isgood BOOLEAN NOT NULL DEFAULT TRUE,
                    displayorder INT NOT NULL DEFAULT 0,
                    createdat TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    updatedat TIMESTAMPTZ
                );

                CREATE INDEX IF NOT EXISTS ixopportunityapprovalimpactrequestorder
                    ON opportunityapprovalimpact (opportunityapprovalrequestid, displayorder);
            ");
        }

        public override void Down()
        {
            Execute.Sql(@"
                DROP INDEX IF EXISTS ixopportunityapprovalimpactrequestorder;
                DROP TABLE IF EXISTS opportunityapprovalimpact;
            ");
        }
    }
}
