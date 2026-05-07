using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605070008)]
    public sealed class Migration_202605070008_AddCampaignCreatorStatusMarksAsConfirmed : Migration
    {
        public override void Up()
        {
            Alter.Table("campaigncreatorstatus")
                .AddColumn("marksasconfirmed").AsBoolean().NotNullable().WithDefaultValue(false);

            Execute.Sql("""
                UPDATE campaigncreatorstatus
                SET marksasconfirmed = true
                WHERE name = 'Confirmado';
                """);
        }

        public override void Down()
        {
            Delete.Column("marksasconfirmed").FromTable("campaigncreatorstatus");
        }
    }
}
