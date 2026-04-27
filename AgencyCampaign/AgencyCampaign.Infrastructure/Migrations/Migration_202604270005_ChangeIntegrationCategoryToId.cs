using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202604270005)]
    public sealed class Migration_202604270005_ChangeIntegrationCategoryToId : Migration
    {
        public override void Up()
        {
            Rename.Column("category").OnTable("integration").To("categoryid");
            Alter.Column("categoryid").OnTable("integration").AsInt64().NotNullable();
        }

        public override void Down()
        {
            Rename.Column("categoryid").OnTable("integration").To("category");
            Alter.Column("category").OnTable("integration").AsInt32().NotNullable().WithDefaultValue(6);
        }
    }
}
