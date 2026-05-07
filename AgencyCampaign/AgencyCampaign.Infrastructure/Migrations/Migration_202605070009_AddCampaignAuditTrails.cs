using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605070009)]
    public sealed class Migration_202605070009_AddCampaignAuditTrails : Migration
    {
        public override void Up()
        {
            Create.Table("campaignstatushistory")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("campaignid").AsInt64().NotNullable()
                .WithColumn("fromstatus").AsInt32().Nullable()
                .WithColumn("tostatus").AsInt32().NotNullable()
                .WithColumn("changedat").AsDateTimeOffset().NotNullable()
                .WithColumn("changedbyuserid").AsInt64().Nullable()
                .WithColumn("changedbyusername").AsString(150).Nullable()
                .WithColumn("reason").AsString(500).Nullable()
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.ForeignKey("fkcampaignstatushistorycampaign")
                .FromTable("campaignstatushistory").ForeignColumn("campaignid")
                .ToTable("campaign").PrimaryColumn("id")
                .OnDelete(System.Data.Rule.Cascade);

            Create.Index("ixcampaignstatushistorycampaignidchangedat")
                .OnTable("campaignstatushistory")
                .OnColumn("campaignid").Ascending()
                .OnColumn("changedat").Descending();

            Create.Table("campaigncreatorstatushistory")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("campaigncreatorid").AsInt64().NotNullable()
                .WithColumn("fromstatusid").AsInt64().Nullable()
                .WithColumn("tostatusid").AsInt64().NotNullable()
                .WithColumn("changedat").AsDateTimeOffset().NotNullable()
                .WithColumn("changedbyuserid").AsInt64().Nullable()
                .WithColumn("changedbyusername").AsString(150).Nullable()
                .WithColumn("reason").AsString(500).Nullable()
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.ForeignKey("fkcampaigncreatorstatushistorycampaigncreator")
                .FromTable("campaigncreatorstatushistory").ForeignColumn("campaigncreatorid")
                .ToTable("campaigncreator").PrimaryColumn("id")
                .OnDelete(System.Data.Rule.Cascade);

            Create.ForeignKey("fkcampaigncreatorstatushistoryfromstatus")
                .FromTable("campaigncreatorstatushistory").ForeignColumn("fromstatusid")
                .ToTable("campaigncreatorstatus").PrimaryColumn("id")
                .OnDelete(System.Data.Rule.SetNull);

            Create.ForeignKey("fkcampaigncreatorstatushistorytostatus")
                .FromTable("campaigncreatorstatushistory").ForeignColumn("tostatusid")
                .ToTable("campaigncreatorstatus").PrimaryColumn("id");

            Create.Index("ixcampaigncreatorstatushistorycampaigncreatoridchangedat")
                .OnTable("campaigncreatorstatushistory")
                .OnColumn("campaigncreatorid").Ascending()
                .OnColumn("changedat").Descending();
        }

        public override void Down()
        {
            Delete.Table("campaigncreatorstatushistory");
            Delete.Table("campaignstatushistory");
        }
    }
}
