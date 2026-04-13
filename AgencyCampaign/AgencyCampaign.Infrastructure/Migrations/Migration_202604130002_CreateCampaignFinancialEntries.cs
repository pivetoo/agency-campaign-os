using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202604130002)]
    public sealed class Migration_202604130002_CreateCampaignFinancialEntries : Migration
    {
        public override void Up()
        {
            Create.Table("campaign_financial_entry")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("campaignid").AsInt64().NotNullable()
                .WithColumn("campaigndeliverableid").AsInt64().Nullable()
                .WithColumn("type").AsInt32().NotNullable()
                .WithColumn("description").AsString(200).NotNullable()
                .WithColumn("amount").AsDecimal(18, 2).NotNullable()
                .WithColumn("dueat").AsDateTimeOffset().NotNullable()
                .WithColumn("paidat").AsDateTimeOffset().Nullable()
                .WithColumn("status").AsInt32().NotNullable().WithDefaultValue(1)
                .WithColumn("counterpartyname").AsString(150).Nullable()
                .WithColumn("notes").AsString(1000).Nullable()
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.ForeignKey("fk_campaign_financial_entry_campaign_campaignid")
                .FromTable("campaign_financial_entry").ForeignColumn("campaignid")
                .ToTable("campaign").PrimaryColumn("id");

            Create.ForeignKey("fk_campaign_financial_entry_campaign_deliverable_campaigndeliverableid")
                .FromTable("campaign_financial_entry").ForeignColumn("campaigndeliverableid")
                .ToTable("campaign_deliverable").PrimaryColumn("id");

            Create.Index("ix_campaign_financial_entry_campaignid")
                .OnTable("campaign_financial_entry")
                .OnColumn("campaignid").Ascending();

            Create.Index("ix_campaign_financial_entry_status_dueat")
                .OnTable("campaign_financial_entry")
                .OnColumn("status").Ascending()
                .OnColumn("dueat").Ascending();
        }

        public override void Down()
        {
            Delete.Index("ix_campaign_financial_entry_status_dueat").OnTable("campaign_financial_entry");
            Delete.Index("ix_campaign_financial_entry_campaignid").OnTable("campaign_financial_entry");
            Delete.ForeignKey("fk_campaign_financial_entry_campaign_deliverable_campaigndeliverableid").OnTable("campaign_financial_entry");
            Delete.ForeignKey("fk_campaign_financial_entry_campaign_campaignid").OnTable("campaign_financial_entry");
            Delete.Table("campaign_financial_entry");
        }
    }
}
