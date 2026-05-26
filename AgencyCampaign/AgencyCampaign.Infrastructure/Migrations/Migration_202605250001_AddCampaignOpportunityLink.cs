using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605250001)]
    public sealed class Migration_202605250001_AddCampaignOpportunityLink : Migration
    {
        public override void Up()
        {
            Alter.Table("campaign")
                .AddColumn("opportunityid").AsInt64().Nullable()
                .AddColumn("sourceproposalid").AsInt64().Nullable();
        }

        public override void Down()
        {
            Delete.Column("opportunityid").FromTable("campaign");
            Delete.Column("sourceproposalid").FromTable("campaign");
        }
    }
}
