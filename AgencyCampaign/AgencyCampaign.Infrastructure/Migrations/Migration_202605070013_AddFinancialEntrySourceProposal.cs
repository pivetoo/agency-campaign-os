using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605070013)]
    public sealed class Migration_202605070013_AddFinancialEntrySourceProposal : Migration
    {
        public override void Up()
        {
            Alter.Table("financialentry")
                .AddColumn("sourceproposalid").AsInt64().Nullable();

            Create.ForeignKey("fkfinancialentrysourceproposal")
                .FromTable("financialentry").ForeignColumn("sourceproposalid")
                .ToTable("proposal").PrimaryColumn("id")
                .OnDelete(System.Data.Rule.SetNull);

            Create.Index("ixfinancialentrysourceproposalid")
                .OnTable("financialentry")
                .OnColumn("sourceproposalid").Ascending();

            Create.Index("ixfinancialentrycampaigndeliverableid")
                .OnTable("financialentry")
                .OnColumn("campaigndeliverableid").Ascending();
        }

        public override void Down()
        {
            Delete.Index("ixfinancialentrycampaigndeliverableid").OnTable("financialentry");
            Delete.Index("ixfinancialentrysourceproposalid").OnTable("financialentry");
            Delete.ForeignKey("fkfinancialentrysourceproposal").OnTable("financialentry");
            Delete.Column("sourceproposalid").FromTable("financialentry");
        }
    }
}
