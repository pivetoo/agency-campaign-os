using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605070011)]
    public sealed class Migration_202605070011_AddDeliverableShareLinks : Migration
    {
        public override void Up()
        {
            Create.Table("deliverablesharelink")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("campaigndeliverableid").AsInt64().NotNullable()
                .WithColumn("token").AsString(80).NotNullable()
                .WithColumn("reviewername").AsString(150).NotNullable()
                .WithColumn("expiresat").AsDateTimeOffset().Nullable()
                .WithColumn("revokedat").AsDateTimeOffset().Nullable()
                .WithColumn("createdbyuserid").AsInt64().Nullable()
                .WithColumn("createdbyusername").AsString(150).Nullable()
                .WithColumn("lastviewedat").AsDateTimeOffset().Nullable()
                .WithColumn("viewcount").AsInt32().NotNullable().WithDefaultValue(0)
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.ForeignKey("fkdeliverablesharelinkcampaigndeliverable")
                .FromTable("deliverablesharelink").ForeignColumn("campaigndeliverableid")
                .ToTable("campaigndeliverable").PrimaryColumn("id")
                .OnDelete(System.Data.Rule.Cascade);

            Create.Index("uxdeliverablesharelinktoken")
                .OnTable("deliverablesharelink")
                .OnColumn("token").Ascending()
                .WithOptions().Unique();

            Create.Index("ixdeliverablesharelinkcampaigndeliverableid")
                .OnTable("deliverablesharelink")
                .OnColumn("campaigndeliverableid").Ascending();
        }

        public override void Down()
        {
            Delete.Table("deliverablesharelink");
        }
    }
}
