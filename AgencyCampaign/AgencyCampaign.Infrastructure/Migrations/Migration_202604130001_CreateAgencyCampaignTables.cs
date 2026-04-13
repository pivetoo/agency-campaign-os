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

            Create.Index("ixbrandname")
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

            Create.Index("ixcreatorname")
                .OnTable("creator")
                .OnColumn("name").Ascending();

            Create.Index("ixcreatordocument")
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

            Create.ForeignKey("fkcampaignbrandbrandid")
                .FromTable("campaign").ForeignColumn("brandid")
                .ToTable("brand").PrimaryColumn("id");

            Create.Index("ixcampaignbrandid")
                .OnTable("campaign")
                .OnColumn("brandid").Ascending();

            Create.Index("ixcampaignname")
                .OnTable("campaign")
                .OnColumn("name").Ascending();

            Create.Table("campaigndeliverable")
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

            Create.ForeignKey("fkcampaigndeliverablecampaigncampaignid")
                .FromTable("campaigndeliverable").ForeignColumn("campaignid")
                .ToTable("campaign").PrimaryColumn("id");

            Create.ForeignKey("fkcampaigndeliverablecreatorcreatorid")
                .FromTable("campaigndeliverable").ForeignColumn("creatorid")
                .ToTable("creator").PrimaryColumn("id");

            Create.Index("ixcampaigndeliverablecampaignid")
                .OnTable("campaigndeliverable")
                .OnColumn("campaignid").Ascending();

            Create.Index("ixcampaigndeliverablecreatorid")
                .OnTable("campaigndeliverable")
                .OnColumn("creatorid").Ascending();

            Create.Index("ixcampaigndeliverablestatusdueat")
                .OnTable("campaigndeliverable")
                .OnColumn("status").Ascending()
                .OnColumn("dueat").Ascending();
        }

        public override void Down()
        {
            Delete.Index("ixcampaigndeliverablestatusdueat").OnTable("campaigndeliverable");
            Delete.Index("ixcampaigndeliverablecreatorid").OnTable("campaigndeliverable");
            Delete.Index("ixcampaigndeliverablecampaignid").OnTable("campaigndeliverable");
            Delete.ForeignKey("fkcampaigndeliverablecreatorcreatorid").OnTable("campaigndeliverable");
            Delete.ForeignKey("fkcampaigndeliverablecampaigncampaignid").OnTable("campaigndeliverable");
            Delete.Table("campaigndeliverable");

            Delete.Index("ixcampaignname").OnTable("campaign");
            Delete.Index("ixcampaignbrandid").OnTable("campaign");
            Delete.ForeignKey("fkcampaignbrandbrandid").OnTable("campaign");
            Delete.Table("campaign");

            Delete.Index("ixcreatordocument").OnTable("creator");
            Delete.Index("ixcreatorname").OnTable("creator");
            Delete.Table("creator");

            Delete.Index("ixbrandname").OnTable("brand");
            Delete.Table("brand");
        }
    }
}
