using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605230005)]
    public sealed class Migration_202605230005_AddNegotiationPolicyFields : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                ALTER TABLE opportunitynegotiation ADD COLUMN IF NOT EXISTS discountpercent NUMERIC(5,2);
                ALTER TABLE opportunitynegotiation ADD COLUMN IF NOT EXISTS marginpercent NUMERIC(5,2);
                ALTER TABLE opportunitynegotiation ADD COLUMN IF NOT EXISTS paymenttermdays INT;
            ");
        }

        public override void Down()
        {
            Execute.Sql(@"
                ALTER TABLE opportunitynegotiation DROP COLUMN IF EXISTS discountpercent;
                ALTER TABLE opportunitynegotiation DROP COLUMN IF EXISTS marginpercent;
                ALTER TABLE opportunitynegotiation DROP COLUMN IF EXISTS paymenttermdays;
            ");
        }
    }
}
