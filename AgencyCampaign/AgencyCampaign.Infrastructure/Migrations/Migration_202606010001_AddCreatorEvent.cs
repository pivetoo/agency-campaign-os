using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202606010001)]
    public sealed class Migration_202606010001_AddCreatorEvent : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                CREATE TABLE creatorevent (
                    id BIGSERIAL PRIMARY KEY,
                    creatorid BIGINT NOT NULL REFERENCES creator (id) ON DELETE CASCADE,
                    eventtype INTEGER NOT NULL,
                    occurredat TIMESTAMPTZ NOT NULL,
                    description VARCHAR(500) NULL,
                    metadata TEXT NULL,
                    createdat TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    updatedat TIMESTAMPTZ NULL
                );
                CREATE INDEX ixcreatoreventcreatorid ON creatorevent (creatorid);
            ");
        }

        public override void Down()
        {
            Execute.Sql(@"DROP TABLE IF EXISTS creatorevent;");
        }
    }
}
