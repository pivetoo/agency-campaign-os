using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605260002)]
    public sealed class Migration_202605260002_AddCampaignReportLink : Migration
    {
        public override void Up()
        {
            Create.Table("campaignreportlink")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("campaignid").AsInt64().NotNullable()
                .WithColumn("token").AsString(80).NotNullable()
                .WithColumn("revokedat").AsDateTimeOffset().Nullable()
                .WithColumn("createdbyuserid").AsInt64().Nullable()
                .WithColumn("createdbyusername").AsString(150).Nullable()
                .WithColumn("lastviewedat").AsDateTimeOffset().Nullable()
                .WithColumn("viewcount").AsInt32().NotNullable().WithDefaultValue(0)
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.ForeignKey("fkcampaignreportlinkcampaign")
                .FromTable("campaignreportlink").ForeignColumn("campaignid")
                .ToTable("campaign").PrimaryColumn("id")
                .OnDelete(System.Data.Rule.Cascade);

            Create.Index("uxcampaignreportlinktoken")
                .OnTable("campaignreportlink")
                .OnColumn("token").Ascending()
                .WithOptions().Unique();

            Create.Index("ixcampaignreportlinkcampaignid")
                .OnTable("campaignreportlink")
                .OnColumn("campaignid").Ascending();
        }

        public override void Down()
        {
            Delete.Table("campaignreportlink");
        }
    }
}
