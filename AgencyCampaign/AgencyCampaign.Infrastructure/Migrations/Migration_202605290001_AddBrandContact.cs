using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605290001)]
    public sealed class Migration_202605290001_AddBrandContact : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                CREATE TABLE brandcontact (
                    id BIGSERIAL PRIMARY KEY,
                    brandid BIGINT NOT NULL REFERENCES brand (id) ON DELETE CASCADE,
                    type INTEGER NOT NULL,
                    value VARCHAR(255) NOT NULL,
                    label VARCHAR(100) NULL,
                    isprimary BOOLEAN NOT NULL DEFAULT FALSE,
                    createdat TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    updatedat TIMESTAMPTZ NULL
                );
                CREATE INDEX ixbrandcontactbrandid ON brandcontact (brandid);

                INSERT INTO brandcontact (brandid, type, value, isprimary, createdat)
                SELECT id, 1, contactemail, TRUE, NOW()
                FROM brand
                WHERE contactemail IS NOT NULL AND btrim(contactemail) <> '';

                INSERT INTO brandcontact (brandid, type, value, isprimary, createdat)
                SELECT id, 2, contactphone, TRUE, NOW()
                FROM brand
                WHERE contactphone IS NOT NULL AND btrim(contactphone) <> '';
            ");
        }

        public override void Down()
        {
            Execute.Sql(@"DROP TABLE IF EXISTS brandcontact;");
        }
    }
}
