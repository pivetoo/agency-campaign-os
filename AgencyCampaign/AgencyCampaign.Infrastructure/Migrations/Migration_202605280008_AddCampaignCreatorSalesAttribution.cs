using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605280008)]
    public sealed class Migration_202605280008_AddCampaignCreatorSalesAttribution : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                ALTER TABLE campaigncreator ADD COLUMN couponcode VARCHAR(100) NULL;
                ALTER TABLE campaigncreator ADD COLUMN trackingurl VARCHAR(1000) NULL;
                ALTER TABLE campaigncreator ADD COLUMN attributedorders INTEGER NULL;
                ALTER TABLE campaigncreator ADD COLUMN attributedrevenue NUMERIC(18,2) NULL;
            ");
        }

        public override void Down()
        {
            Execute.Sql(@"
                ALTER TABLE campaigncreator DROP COLUMN IF EXISTS couponcode;
                ALTER TABLE campaigncreator DROP COLUMN IF EXISTS trackingurl;
                ALTER TABLE campaigncreator DROP COLUMN IF EXISTS attributedorders;
                ALTER TABLE campaigncreator DROP COLUMN IF EXISTS attributedrevenue;
            ");
        }
    }
}
