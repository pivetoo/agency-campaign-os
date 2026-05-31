using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605300001)]
    public sealed class Migration_202605300001_DropCommercialPolicyMinMargin : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"ALTER TABLE commercialpolicy DROP COLUMN IF EXISTS minmarginpercent;");
        }

        public override void Down()
        {
            Execute.Sql(@"ALTER TABLE commercialpolicy ADD COLUMN IF NOT EXISTS minmarginpercent NUMERIC(5,2);");
        }
    }
}
