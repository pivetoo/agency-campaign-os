using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605080002)]
    public sealed class Migration_202605080002_AddMissingForeignKeyIndexes : Migration
    {
        public override void Up()
        {
            CreateIndex("opportunity", "opportunitysourceid", "ixopportunityopportunitysourceid");
            CreateIndex("proposal", "campaignid", "ixproposalcampaignid");
            CreateIndex("proposalsharelink", "proposalid", "ixproposalsharelinkproposalid");
            CreateIndex("opportunitystagehistory", "fromstageid", "ixopportunitystagehistoryfromstageid");
            CreateIndex("opportunitystagehistory", "tostageid", "ixopportunitystagehistorytostageid");
            CreateIndex("campaigncreatorstatushistory", "fromstatusid", "ixcampaigncreatorstatushistoryfromstatusid");
            CreateIndex("campaigncreatorstatushistory", "tostatusid", "ixcampaigncreatorstatushistorytostatusid");
        }

        public override void Down()
        {
            DropIndex("opportunity", "ixopportunityopportunitysourceid");
            DropIndex("proposal", "ixproposalcampaignid");
            DropIndex("proposalsharelink", "ixproposalsharelinkproposalid");
            DropIndex("opportunitystagehistory", "ixopportunitystagehistoryfromstageid");
            DropIndex("opportunitystagehistory", "ixopportunitystagehistorytostageid");
            DropIndex("campaigncreatorstatushistory", "ixcampaigncreatorstatushistoryfromstatusid");
            DropIndex("campaigncreatorstatushistory", "ixcampaigncreatorstatushistorytostatusid");
        }

        private void CreateIndex(string table, string column, string indexName)
        {
            if (Schema.Table(table).Exists() && !Schema.Table(table).Index(indexName).Exists())
            {
                Create.Index(indexName)
                    .OnTable(table)
                    .OnColumn(column).Ascending();
            }
        }

        private void DropIndex(string table, string indexName)
        {
            if (Schema.Table(table).Exists() && Schema.Table(table).Index(indexName).Exists())
            {
                Delete.Index(indexName).OnTable(table);
            }
        }
    }
}
