using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202604130003)]
    public sealed class Migration_202604130003_ExpandAgencyCampaignDomain : Migration
    {
        public override void Up()
        {
            Alter.Table("brand")
                .AddColumn("tradename").AsString(150).Nullable()
                .AddColumn("document").AsString(30).Nullable()
                .AddColumn("notes").AsString(1000).Nullable();

            Alter.Table("creator")
                .AddColumn("stagename").AsString(150).Nullable()
                .AddColumn("primaryniche").AsString(120).Nullable()
                .AddColumn("city").AsString(120).Nullable()
                .AddColumn("state").AsString(50).Nullable()
                .AddColumn("notes").AsString(1000).Nullable()
                .AddColumn("defaultagencyfeepercent").AsDecimal(5, 2).NotNullable().WithDefaultValue(0);

            Alter.Table("campaign")
                .AddColumn("objective").AsString(500).Nullable()
                .AddColumn("briefing").AsString(4000).Nullable()
                .AddColumn("status").AsInt32().NotNullable().WithDefaultValue(1)
                .AddColumn("internalownername").AsString(150).Nullable()
                .AddColumn("notes").AsString(1000).Nullable();

            Create.Table("campaigncreator")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("campaignid").AsInt64().NotNullable()
                .WithColumn("creatorid").AsInt64().NotNullable()
                .WithColumn("status").AsInt32().NotNullable().WithDefaultValue(1)
                .WithColumn("agreedamount").AsDecimal(18, 2).NotNullable().WithDefaultValue(0)
                .WithColumn("agencyfeepercent").AsDecimal(5, 2).NotNullable().WithDefaultValue(0)
                .WithColumn("agencyfeeamount").AsDecimal(18, 2).NotNullable().WithDefaultValue(0)
                .WithColumn("notes").AsString(1000).Nullable()
                .WithColumn("confirmedat").AsDateTimeOffset().Nullable()
                .WithColumn("cancelledat").AsDateTimeOffset().Nullable()
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.ForeignKey("fkcampaigncreatorcampaigncampaignid")
                .FromTable("campaigncreator").ForeignColumn("campaignid")
                .ToTable("campaign").PrimaryColumn("id");

            Create.ForeignKey("fkcampaigncreatorcreatorcreatorid")
                .FromTable("campaigncreator").ForeignColumn("creatorid")
                .ToTable("creator").PrimaryColumn("id");

            Create.Index("ixcampaigncreatorcampaignid")
                .OnTable("campaigncreator")
                .OnColumn("campaignid").Ascending();

            Create.Index("ixcampaigncreatorcreatorid")
                .OnTable("campaigncreator")
                .OnColumn("creatorid").Ascending();

            Create.Index("ixcampaigncreatorcampaignidcreatorid")
                .OnTable("campaigncreator")
                .OnColumn("campaignid").Ascending()
                .OnColumn("creatorid").Ascending()
                .WithOptions().Unique();

            Execute.Sql(@"
                INSERT INTO campaigncreator (campaignid, creatorid, status, agreedamount, agencyfeepercent, agencyfeeamount, createdat)
                SELECT DISTINCT campaignid, creatorid, 3, 0, 0, 0, NOW()
                FROM campaigndeliverable;
            ");

            Alter.Table("campaigndeliverable")
                .AddColumn("campaigncreatorid").AsInt64().Nullable()
                .AddColumn("type").AsInt32().NotNullable().WithDefaultValue(7)
                .AddColumn("platform").AsInt32().NotNullable().WithDefaultValue(6)
                .AddColumn("publishedurl").AsString(1000).Nullable()
                .AddColumn("evidenceurl").AsString(1000).Nullable()
                .AddColumn("notes").AsString(1000).Nullable();

            Execute.Sql(@"
                UPDATE campaigndeliverable cd
                SET campaigncreatorid = cc.id
                FROM campaigncreator cc
                WHERE cc.campaignid = cd.campaignid
                  AND cc.creatorid = cd.creatorid;
            ");

            Alter.Column("campaigncreatorid").OnTable("campaigndeliverable").AsInt64().NotNullable();

            Create.ForeignKey("fkcampaigndeliverablecampaigncreatorcampaigncreatorid")
                .FromTable("campaigndeliverable").ForeignColumn("campaigncreatorid")
                .ToTable("campaigncreator").PrimaryColumn("id");

            Create.Index("ixcampaigndeliverablecampaigncreatorid")
                .OnTable("campaigndeliverable")
                .OnColumn("campaigncreatorid").Ascending();

            Delete.ForeignKey("fkcampaigndeliverablecreatorcreatorid").OnTable("campaigndeliverable");
            Delete.Index("ixcampaigndeliverablecreatorid").OnTable("campaigndeliverable");
            Delete.Column("creatorid").FromTable("campaigndeliverable");

            Create.Table("deliverableapproval")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("campaigndeliverableid").AsInt64().NotNullable()
                .WithColumn("approvaltype").AsInt32().NotNullable()
                .WithColumn("status").AsInt32().NotNullable().WithDefaultValue(1)
                .WithColumn("reviewername").AsString(150).NotNullable()
                .WithColumn("comment").AsString(1000).Nullable()
                .WithColumn("approvedat").AsDateTimeOffset().Nullable()
                .WithColumn("rejectedat").AsDateTimeOffset().Nullable()
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.ForeignKey("fkdeliverableapprovalcampaigndeliverablecampaigndeliverableid")
                .FromTable("deliverableapproval").ForeignColumn("campaigndeliverableid")
                .ToTable("campaigndeliverable").PrimaryColumn("id");

            Create.Index("ixdeliverableapprovalcampaigndeliverableid")
                .OnTable("deliverableapproval")
                .OnColumn("campaigndeliverableid").Ascending();

            Create.Index("ixdeliverableapprovalcampaigndeliverableidapprovaltype")
                .OnTable("deliverableapproval")
                .OnColumn("campaigndeliverableid").Ascending()
                .OnColumn("approvaltype").Ascending()
                .WithOptions().Unique();

            Alter.Table("campaignfinancialentry")
                .AddColumn("category").AsInt32().NotNullable().WithDefaultValue(4)
                .AddColumn("occurredat").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
                .AddColumn("paymentmethod").AsString(100).Nullable()
                .AddColumn("referencecode").AsString(100).Nullable();
        }

        public override void Down()
        {
            Delete.Column("referencecode").FromTable("campaignfinancialentry");
            Delete.Column("paymentmethod").FromTable("campaignfinancialentry");
            Delete.Column("occurredat").FromTable("campaignfinancialentry");
            Delete.Column("category").FromTable("campaignfinancialentry");

            Delete.Index("ixdeliverableapprovalcampaigndeliverableidapprovaltype").OnTable("deliverableapproval");
            Delete.Index("ixdeliverableapprovalcampaigndeliverableid").OnTable("deliverableapproval");
            Delete.ForeignKey("fkdeliverableapprovalcampaigndeliverablecampaigndeliverableid").OnTable("deliverableapproval");
            Delete.Table("deliverableapproval");

            Alter.Table("campaigndeliverable")
                .AddColumn("creatorid").AsInt64().Nullable();

            Execute.Sql(@"
                UPDATE campaigndeliverable cd
                SET creatorid = cc.creatorid
                FROM campaigncreator cc
                WHERE cc.id = cd.campaigncreatorid;
            ");

            Alter.Column("creatorid").OnTable("campaigndeliverable").AsInt64().NotNullable();

            Create.ForeignKey("fkcampaigndeliverablecreatorcreatorid")
                .FromTable("campaigndeliverable").ForeignColumn("creatorid")
                .ToTable("creator").PrimaryColumn("id");

            Create.Index("ixcampaigndeliverablecreatorid")
                .OnTable("campaigndeliverable")
                .OnColumn("creatorid").Ascending();

            Delete.Index("ixcampaigndeliverablecampaigncreatorid").OnTable("campaigndeliverable");
            Delete.ForeignKey("fkcampaigndeliverablecampaigncreatorcampaigncreatorid").OnTable("campaigndeliverable");
            Delete.Column("notes").FromTable("campaigndeliverable");
            Delete.Column("evidenceurl").FromTable("campaigndeliverable");
            Delete.Column("publishedurl").FromTable("campaigndeliverable");
            Delete.Column("platform").FromTable("campaigndeliverable");
            Delete.Column("type").FromTable("campaigndeliverable");
            Delete.Column("campaigncreatorid").FromTable("campaigndeliverable");

            Delete.Index("ixcampaigncreatorcampaignidcreatorid").OnTable("campaigncreator");
            Delete.Index("ixcampaigncreatorcreatorid").OnTable("campaigncreator");
            Delete.Index("ixcampaigncreatorcampaignid").OnTable("campaigncreator");
            Delete.ForeignKey("fkcampaigncreatorcreatorcreatorid").OnTable("campaigncreator");
            Delete.ForeignKey("fkcampaigncreatorcampaigncampaignid").OnTable("campaigncreator");
            Delete.Table("campaigncreator");

            Delete.Column("notes").FromTable("campaign");
            Delete.Column("internalownername").FromTable("campaign");
            Delete.Column("status").FromTable("campaign");
            Delete.Column("briefing").FromTable("campaign");
            Delete.Column("objective").FromTable("campaign");

            Delete.Column("defaultagencyfeepercent").FromTable("creator");
            Delete.Column("notes").FromTable("creator");
            Delete.Column("state").FromTable("creator");
            Delete.Column("city").FromTable("creator");
            Delete.Column("primaryniche").FromTable("creator");
            Delete.Column("stagename").FromTable("creator");

            Delete.Column("notes").FromTable("brand");
            Delete.Column("document").FromTable("brand");
            Delete.Column("tradename").FromTable("brand");
        }
    }
}
