using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202604130006)]
    public sealed class Migration_202604130006_AddCampaignDocuments : Migration
    {
        public override void Up()
        {
            Create.Table("campaigndocument")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("campaignid").AsInt64().NotNullable()
                .WithColumn("campaigncreatorid").AsInt64().Nullable()
                .WithColumn("documenttype").AsInt32().NotNullable()
                .WithColumn("title").AsString(150).NotNullable()
                .WithColumn("documenturl").AsString(1000).Nullable()
                .WithColumn("status").AsInt32().NotNullable().WithDefaultValue(1)
                .WithColumn("recipientemail").AsString(150).Nullable()
                .WithColumn("emailsubject").AsString(200).Nullable()
                .WithColumn("emailbody").AsString(5000).Nullable()
                .WithColumn("sentat").AsDateTimeOffset().Nullable()
                .WithColumn("signedat").AsDateTimeOffset().Nullable()
                .WithColumn("notes").AsString(1000).Nullable()
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.ForeignKey("fkcampaigndocumentcampaigncampaignid")
                .FromTable("campaigndocument").ForeignColumn("campaignid")
                .ToTable("campaign").PrimaryColumn("id");

            Create.ForeignKey("fkcampaigndocumentcampaigncreatorcampaigncreatorid")
                .FromTable("campaigndocument").ForeignColumn("campaigncreatorid")
                .ToTable("campaigncreator").PrimaryColumn("id");

            Create.Index("ixcampaigndocumentcampaignid")
                .OnTable("campaigndocument")
                .OnColumn("campaignid").Ascending();

            Create.Index("ixcampaigndocumentcampaigncreatorid")
                .OnTable("campaigndocument")
                .OnColumn("campaigncreatorid").Ascending();
        }

        public override void Down()
        {
            Delete.Index("ixcampaigndocumentcampaigncreatorid").OnTable("campaigndocument");
            Delete.Index("ixcampaigndocumentcampaignid").OnTable("campaigndocument");
            Delete.ForeignKey("fkcampaigndocumentcampaigncreatorcampaigncreatorid").OnTable("campaigndocument");
            Delete.ForeignKey("fkcampaigndocumentcampaigncampaignid").OnTable("campaigndocument");
            Delete.Table("campaigndocument");
        }
    }
}
