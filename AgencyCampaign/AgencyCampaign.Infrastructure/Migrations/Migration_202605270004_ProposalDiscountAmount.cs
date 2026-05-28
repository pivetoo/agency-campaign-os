using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605270004)]
    public sealed class Migration_202605270004_ProposalDiscountAmount : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                ALTER TABLE proposal DROP COLUMN IF EXISTS discountpercent;
                ALTER TABLE proposal ADD COLUMN IF NOT EXISTS discountamount NUMERIC(18,2);
            ");
        }

        public override void Down()
        {
            Execute.Sql(@"
                ALTER TABLE proposal DROP COLUMN IF EXISTS discountamount;
                ALTER TABLE proposal ADD COLUMN IF NOT EXISTS discountpercent NUMERIC(5,2);
            ");
        }
    }
}
