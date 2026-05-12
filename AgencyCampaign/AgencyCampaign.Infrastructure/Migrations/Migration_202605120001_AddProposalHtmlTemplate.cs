using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605120001)]
    public sealed class Migration_202605120001_AddProposalHtmlTemplate : Migration
    {
        public override void Up()
        {
            Alter.Table("agencysettings")
                .AddColumn("proposalhtmltemplate").AsCustom("text").Nullable();
        }

        public override void Down()
        {
            Delete.Column("proposalhtmltemplate").FromTable("agencysettings");
        }
    }
}
