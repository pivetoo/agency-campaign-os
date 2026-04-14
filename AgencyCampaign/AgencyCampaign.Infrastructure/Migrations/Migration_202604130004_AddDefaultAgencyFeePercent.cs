using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202604130004)]
    public sealed class Migration_202604130004_AddDefaultAgencyFeePercent : Migration
    {
        public override void Up()
        {
            Alter.Table("creator")
                .AddColumn("defaultagencyfeepercent").AsDecimal(5, 2).NotNullable().WithDefaultValue(0);

            Alter.Table("campaigncreator")
                .AddColumn("agencyfeepercent").AsDecimal(5, 2).NotNullable().WithDefaultValue(0);
        }

        public override void Down()
        {
            Delete.Column("agencyfeepercent").FromTable("campaigncreator");
            Delete.Column("defaultagencyfeepercent").FromTable("creator");
        }
    }
}
