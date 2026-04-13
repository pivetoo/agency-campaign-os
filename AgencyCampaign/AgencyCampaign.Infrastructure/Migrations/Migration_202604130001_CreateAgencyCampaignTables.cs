using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202604130001)]
    public sealed class Migration_202604130001_CreateAgencyCampaignTables : Migration
    {
        public override void Up()
        {
            Create.Table("brand")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("name").AsString(150).NotNullable()
                .WithColumn("contactname").AsString(100).Nullable()
                .WithColumn("contactemail").AsString(150).Nullable()
                .WithColumn("isactive").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.Index("ix_brand_name")
                .OnTable("brand")
                .OnColumn("name").Ascending();

            Create.Table("creator")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("name").AsString(150).NotNullable()
                .WithColumn("email").AsString(150).Nullable()
                .WithColumn("phone").AsString(50).Nullable()
                .WithColumn("document").AsString(30).Nullable()
                .WithColumn("pixkey").AsString(150).Nullable()
                .WithColumn("isactive").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.Index("ix_creator_name")
                .OnTable("creator")
                .OnColumn("name").Ascending();

            Create.Index("ix_creator_document")
                .OnTable("creator")
                .OnColumn("document").Ascending();

            Create.Table("campaign")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("brandid").AsInt64().NotNullable()
                .WithColumn("name").AsString(150).NotNullable()
                .WithColumn("description").AsString(1000).Nullable()
                .WithColumn("budget").AsDecimal(18, 2).NotNullable()
                .WithColumn("startsat").AsDateTimeOffset().NotNullable()
                .WithColumn("endsat").AsDateTimeOffset().Nullable()
                .WithColumn("isactive").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.ForeignKey("fk_campaign_brand_brandid")
                .FromTable("campaign").ForeignColumn("brandid")
                .ToTable("brand").PrimaryColumn("id");

            Create.Index("ix_campaign_brandid")
                .OnTable("campaign")
                .OnColumn("brandid").Ascending();

            Create.Index("ix_campaign_name")
                .OnTable("campaign")
                .OnColumn("name").Ascending();

            Create.Table("campaign_deliverable")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("campaignid").AsInt64().NotNullable()
                .WithColumn("creatorid").AsInt64().NotNullable()
                .WithColumn("title").AsString(150).NotNullable()
                .WithColumn("description").AsString(1000).Nullable()
                .WithColumn("dueat").AsDateTimeOffset().NotNullable()
                .WithColumn("publishedat").AsDateTimeOffset().Nullable()
                .WithColumn("status").AsInt32().NotNullable().WithDefaultValue(1)
                .WithColumn("grossamount").AsDecimal(18, 2).NotNullable()
                .WithColumn("creatoramount").AsDecimal(18, 2).NotNullable()
                .WithColumn("agencyfeeamount").AsDecimal(18, 2).NotNullable()
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.ForeignKey("fk_campaign_deliverable_campaign_campaignid")
                .FromTable("campaign_deliverable").ForeignColumn("campaignid")
                .ToTable("campaign").PrimaryColumn("id");

            Create.ForeignKey("fk_campaign_deliverable_creator_creatorid")
                .FromTable("campaign_deliverable").ForeignColumn("creatorid")
                .ToTable("creator").PrimaryColumn("id");

            Create.Index("ix_campaign_deliverable_campaignid")
                .OnTable("campaign_deliverable")
                .OnColumn("campaignid").Ascending();

            Create.Index("ix_campaign_deliverable_creatorid")
                .OnTable("campaign_deliverable")
                .OnColumn("creatorid").Ascending();

            Create.Index("ix_campaign_deliverable_status_dueat")
                .OnTable("campaign_deliverable")
                .OnColumn("status").Ascending()
                .OnColumn("dueat").Ascending();
        }

        public override void Down()
        {
            Delete.Index("ix_campaign_deliverable_status_dueat").OnTable("campaign_deliverable");
            Delete.Index("ix_campaign_deliverable_creatorid").OnTable("campaign_deliverable");
            Delete.Index("ix_campaign_deliverable_campaignid").OnTable("campaign_deliverable");
            Delete.ForeignKey("fk_campaign_deliverable_creator_creatorid").OnTable("campaign_deliverable");
            Delete.ForeignKey("fk_campaign_deliverable_campaign_campaignid").OnTable("campaign_deliverable");
            Delete.Table("campaign_deliverable");

            Delete.Index("ix_campaign_name").OnTable("campaign");
            Delete.Index("ix_campaign_brandid").OnTable("campaign");
            Delete.ForeignKey("fk_campaign_brand_brandid").OnTable("campaign");
            Delete.Table("campaign");

            Delete.Index("ix_creator_document").OnTable("creator");
            Delete.Index("ix_creator_name").OnTable("creator");
            Delete.Table("creator");

            Delete.Index("ix_brand_name").OnTable("brand");
            Delete.Table("brand");
        }
    }
}
