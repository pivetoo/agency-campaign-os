using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605090004)]
    public sealed class Migration_202605090004_AddBrandLogoUrl : Migration
    {
        public override void Up()
        {
            Alter.Table("brand")
                .AddColumn("logourl").AsString(255).Nullable();
        }

        public override void Down()
        {
            Delete.Column("logourl").FromTable("brand");
        }
    }
}
