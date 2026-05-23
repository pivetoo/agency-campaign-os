using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605230001)]
    public sealed class Migration_202605230001_AddOpportunityApprovalComment : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                CREATE TABLE IF NOT EXISTS opportunityapprovalcomment (
                    id BIGSERIAL PRIMARY KEY,
                    opportunityapprovalrequestid BIGINT NOT NULL REFERENCES opportunityapprovalrequest(id) ON DELETE CASCADE,
                    userid BIGINT,
                    username VARCHAR(150) NOT NULL,
                    role VARCHAR(40) NOT NULL DEFAULT 'observador',
                    body VARCHAR(4000) NOT NULL,
                    createdat TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    updatedat TIMESTAMPTZ
                );

                CREATE INDEX IF NOT EXISTS ixopportunityapprovalcommentrequestcreated
                    ON opportunityapprovalcomment (opportunityapprovalrequestid, createdat);
            ");
        }

        public override void Down()
        {
            Execute.Sql(@"
                DROP INDEX IF EXISTS ixopportunityapprovalcommentrequestcreated;
                DROP TABLE IF EXISTS opportunityapprovalcomment;
            ");
        }
    }
}
