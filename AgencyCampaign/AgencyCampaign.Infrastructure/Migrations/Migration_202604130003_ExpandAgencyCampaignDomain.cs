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
                .AddColumn("notes").AsString(1000).Nullable();

            Alter.Table("campaign")
                .AddColumn("objective").AsString(500).Nullable()
                .AddColumn("briefing").AsString(4000).Nullable()
                .AddColumn("status").AsInt32().NotNullable().WithDefaultValue(1)
                .AddColumn("internalownername").AsString(150).Nullable()
                .AddColumn("notes").AsString(1000).Nullable();

            Create.Table("campaign_creator")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("campaignid").AsInt64().NotNullable()
                .WithColumn("creatorid").AsInt64().NotNullable()
                .WithColumn("status").AsInt32().NotNullable().WithDefaultValue(1)
                .WithColumn("agreedamount").AsDecimal(18, 2).NotNullable().WithDefaultValue(0)
                .WithColumn("agencyfeeamount").AsDecimal(18, 2).NotNullable().WithDefaultValue(0)
                .WithColumn("notes").AsString(1000).Nullable()
                .WithColumn("confirmedat").AsDateTimeOffset().Nullable()
                .WithColumn("cancelledat").AsDateTimeOffset().Nullable()
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.ForeignKey("fk_campaign_creator_campaign_campaignid")
                .FromTable("campaign_creator").ForeignColumn("campaignid")
                .ToTable("campaign").PrimaryColumn("id");

            Create.ForeignKey("fk_campaign_creator_creator_creatorid")
                .FromTable("campaign_creator").ForeignColumn("creatorid")
                .ToTable("creator").PrimaryColumn("id");

            Create.Index("ix_campaign_creator_campaignid")
                .OnTable("campaign_creator")
                .OnColumn("campaignid").Ascending();

            Create.Index("ix_campaign_creator_creatorid")
                .OnTable("campaign_creator")
                .OnColumn("creatorid").Ascending();

            Create.Index("ix_campaign_creator_campaignid_creatorid")
                .OnTable("campaign_creator")
                .OnColumn("campaignid").Ascending()
                .OnColumn("creatorid").Ascending()
                .WithOptions().Unique();

            Execute.Sql(@"
                INSERT INTO campaign_creator (campaignid, creatorid, status, agreedamount, agencyfeeamount, createdat)
                SELECT DISTINCT campaignid, creatorid, 3, 0, 0, NOW()
                FROM campaign_deliverable;
            ");

            Alter.Table("campaign_deliverable")
                .AddColumn("campaigncreatorid").AsInt64().Nullable()
                .AddColumn("type").AsInt32().NotNullable().WithDefaultValue(7)
                .AddColumn("platform").AsInt32().NotNullable().WithDefaultValue(6)
                .AddColumn("publishedurl").AsString(1000).Nullable()
                .AddColumn("evidenceurl").AsString(1000).Nullable()
                .AddColumn("notes").AsString(1000).Nullable();

            Execute.Sql(@"
                UPDATE campaign_deliverable cd
                SET campaigncreatorid = cc.id
                FROM campaign_creator cc
                WHERE cc.campaignid = cd.campaignid
                  AND cc.creatorid = cd.creatorid;
            ");

            Alter.Column("campaigncreatorid").OnTable("campaign_deliverable").AsInt64().NotNullable();

            Create.ForeignKey("fk_campaign_deliverable_campaign_creator_campaigncreatorid")
                .FromTable("campaign_deliverable").ForeignColumn("campaigncreatorid")
                .ToTable("campaign_creator").PrimaryColumn("id");

            Create.Index("ix_campaign_deliverable_campaigncreatorid")
                .OnTable("campaign_deliverable")
                .OnColumn("campaigncreatorid").Ascending();

            Delete.ForeignKey("fk_campaign_deliverable_creator_creatorid").OnTable("campaign_deliverable");
            Delete.Index("ix_campaign_deliverable_creatorid").OnTable("campaign_deliverable");
            Delete.Column("creatorid").FromTable("campaign_deliverable");

            Create.Table("deliverable_approval")
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

            Create.ForeignKey("fk_deliverable_approval_campaign_deliverable_campaigndeliverableid")
                .FromTable("deliverable_approval").ForeignColumn("campaigndeliverableid")
                .ToTable("campaign_deliverable").PrimaryColumn("id");

            Create.Index("ix_deliverable_approval_campaigndeliverableid")
                .OnTable("deliverable_approval")
                .OnColumn("campaigndeliverableid").Ascending();

            Create.Index("ix_deliverable_approval_campaigndeliverableid_approvaltype")
                .OnTable("deliverable_approval")
                .OnColumn("campaigndeliverableid").Ascending()
                .OnColumn("approvaltype").Ascending()
                .WithOptions().Unique();

            Alter.Table("campaign_financial_entry")
                .AddColumn("category").AsInt32().NotNullable().WithDefaultValue(4)
                .AddColumn("occurredat").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
                .AddColumn("paymentmethod").AsString(100).Nullable()
                .AddColumn("referencecode").AsString(100).Nullable();
        }

        public override void Down()
        {
            Delete.Column("referencecode").FromTable("campaign_financial_entry");
            Delete.Column("paymentmethod").FromTable("campaign_financial_entry");
            Delete.Column("occurredat").FromTable("campaign_financial_entry");
            Delete.Column("category").FromTable("campaign_financial_entry");

            Delete.Index("ix_deliverable_approval_campaigndeliverableid_approvaltype").OnTable("deliverable_approval");
            Delete.Index("ix_deliverable_approval_campaigndeliverableid").OnTable("deliverable_approval");
            Delete.ForeignKey("fk_deliverable_approval_campaign_deliverable_campaigndeliverableid").OnTable("deliverable_approval");
            Delete.Table("deliverable_approval");

            Alter.Table("campaign_deliverable")
                .AddColumn("creatorid").AsInt64().Nullable();

            Execute.Sql(@"
                UPDATE campaign_deliverable cd
                SET creatorid = cc.creatorid
                FROM campaign_creator cc
                WHERE cc.id = cd.campaigncreatorid;
            ");

            Alter.Column("creatorid").OnTable("campaign_deliverable").AsInt64().NotNullable();

            Create.ForeignKey("fk_campaign_deliverable_creator_creatorid")
                .FromTable("campaign_deliverable").ForeignColumn("creatorid")
                .ToTable("creator").PrimaryColumn("id");

            Create.Index("ix_campaign_deliverable_creatorid")
                .OnTable("campaign_deliverable")
                .OnColumn("creatorid").Ascending();

            Delete.Index("ix_campaign_deliverable_campaigncreatorid").OnTable("campaign_deliverable");
            Delete.ForeignKey("fk_campaign_deliverable_campaign_creator_campaigncreatorid").OnTable("campaign_deliverable");
            Delete.Column("notes").FromTable("campaign_deliverable");
            Delete.Column("evidenceurl").FromTable("campaign_deliverable");
            Delete.Column("publishedurl").FromTable("campaign_deliverable");
            Delete.Column("platform").FromTable("campaign_deliverable");
            Delete.Column("type").FromTable("campaign_deliverable");
            Delete.Column("campaigncreatorid").FromTable("campaign_deliverable");

            Delete.Index("ix_campaign_creator_campaignid_creatorid").OnTable("campaign_creator");
            Delete.Index("ix_campaign_creator_creatorid").OnTable("campaign_creator");
            Delete.Index("ix_campaign_creator_campaignid").OnTable("campaign_creator");
            Delete.ForeignKey("fk_campaign_creator_creator_creatorid").OnTable("campaign_creator");
            Delete.ForeignKey("fk_campaign_creator_campaign_campaignid").OnTable("campaign_creator");
            Delete.Table("campaign_creator");

            Delete.Column("notes").FromTable("campaign");
            Delete.Column("internalownername").FromTable("campaign");
            Delete.Column("status").FromTable("campaign");
            Delete.Column("briefing").FromTable("campaign");
            Delete.Column("objective").FromTable("campaign");

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
