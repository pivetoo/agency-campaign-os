using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202604130002)]
    public sealed class Migration_202604130002_CreateCampaignFinancialEntries : Migration
    {
        public override void Up()
        {
            Create.Table("campaignfinancialentry")
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

            Create.ForeignKey("fkcampaignfinancialentrycampaigncampaignid")
                .FromTable("campaignfinancialentry").ForeignColumn("campaignid")
                .ToTable("campaign").PrimaryColumn("id");

            Create.ForeignKey("fkcampaignfinancialentrycampaigndeliverablecampaigndeliverableid")
                .FromTable("campaignfinancialentry").ForeignColumn("campaigndeliverableid")
                .ToTable("campaigndeliverable").PrimaryColumn("id");

            Create.Index("ixcampaignfinancialentrycampaignid")
                .OnTable("campaignfinancialentry")
                .OnColumn("campaignid").Ascending();

            Create.Index("ixcampaignfinancialentrystatusdueat")
                .OnTable("campaignfinancialentry")
                .OnColumn("status").Ascending()
                .OnColumn("dueat").Ascending();
        }

        public override void Down()
        {
            Delete.Index("ixcampaignfinancialentrystatusdueat").OnTable("campaignfinancialentry");
            Delete.Index("ixcampaignfinancialentrycampaignid").OnTable("campaignfinancialentry");
            Delete.ForeignKey("fkcampaignfinancialentrycampaigndeliverablecampaigndeliverableid").OnTable("campaignfinancialentry");
            Delete.ForeignKey("fkcampaignfinancialentrycampaigncampaignid").OnTable("campaignfinancialentry");
            Delete.Table("campaignfinancialentry");
        }
    }
}
