using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605090001)]
    public sealed class Migration_202605090001_EvolveCampaignDocumentForDigitalSignature : Migration
    {
        public override void Up()
        {
            Create.Table("campaigndocumenttemplate")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("name").AsString(150).NotNullable()
                .WithColumn("description").AsString(500).Nullable()
                .WithColumn("documenttype").AsInt32().NotNullable()
                .WithColumn("body").AsCustom("TEXT").NotNullable()
                .WithColumn("isactive").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("createdbyuserid").AsInt64().Nullable()
                .WithColumn("createdbyusername").AsString(150).Nullable()
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.Index("ixcampaigndocumenttemplatedocumenttype")
                .OnTable("campaigndocumenttemplate")
                .OnColumn("documenttype").Ascending();

            Alter.Table("campaigndocument")
                .AddColumn("templateid").AsInt64().Nullable()
                .AddColumn("body").AsCustom("TEXT").Nullable()
                .AddColumn("provider").AsString(50).Nullable()
                .AddColumn("providerdocumentid").AsString(150).Nullable()
                .AddColumn("signeddocumenturl").AsString(1000).Nullable();

            Create.ForeignKey("fkcampaigndocumentcampaigndocumenttemplatetemplateid")
                .FromTable("campaigndocument").ForeignColumn("templateid")
                .ToTable("campaigndocumenttemplate").PrimaryColumn("id");

            Create.Index("ixcampaigndocumenttemplateid")
                .OnTable("campaigndocument")
                .OnColumn("templateid").Ascending();

            Create.Index("ixcampaigndocumentproviderdocumentid")
                .OnTable("campaigndocument")
                .OnColumn("providerdocumentid").Ascending();

            Create.Table("campaigndocumentsignature")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("campaigndocumentid").AsInt64().NotNullable()
                .WithColumn("role").AsInt32().NotNullable()
                .WithColumn("signername").AsString(150).NotNullable()
                .WithColumn("signeremail").AsString(150).NotNullable()
                .WithColumn("signerdocumentnumber").AsString(50).Nullable()
                .WithColumn("providersignerid").AsString(150).Nullable()
                .WithColumn("signedat").AsDateTimeOffset().Nullable()
                .WithColumn("ipaddress").AsString(50).Nullable()
                .WithColumn("useragent").AsString(500).Nullable()
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.ForeignKey("fkcampaigndocumentsignaturecampaigndocumentcampaigndocumentid")
                .FromTable("campaigndocumentsignature").ForeignColumn("campaigndocumentid")
                .ToTable("campaigndocument").PrimaryColumn("id");

            Create.Index("ixcampaigndocumentsignaturecampaigndocumentid")
                .OnTable("campaigndocumentsignature")
                .OnColumn("campaigndocumentid").Ascending();

            Create.Table("campaigndocumentevent")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("campaigndocumentid").AsInt64().NotNullable()
                .WithColumn("eventtype").AsInt32().NotNullable()
                .WithColumn("occurredat").AsDateTimeOffset().NotNullable()
                .WithColumn("description").AsString(500).Nullable()
                .WithColumn("metadata").AsCustom("TEXT").Nullable()
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.ForeignKey("fkcampaigndocumenteventcampaigndocumentcampaigndocumentid")
                .FromTable("campaigndocumentevent").ForeignColumn("campaigndocumentid")
                .ToTable("campaigndocument").PrimaryColumn("id");

            Create.Index("ixcampaigndocumenteventcampaigndocumentid")
                .OnTable("campaigndocumentevent")
                .OnColumn("campaigndocumentid").Ascending();
        }

        public override void Down()
        {
            Delete.Index("ixcampaigndocumenteventcampaigndocumentid").OnTable("campaigndocumentevent");
            Delete.ForeignKey("fkcampaigndocumenteventcampaigndocumentcampaigndocumentid").OnTable("campaigndocumentevent");
            Delete.Table("campaigndocumentevent");

            Delete.Index("ixcampaigndocumentsignaturecampaigndocumentid").OnTable("campaigndocumentsignature");
            Delete.ForeignKey("fkcampaigndocumentsignaturecampaigndocumentcampaigndocumentid").OnTable("campaigndocumentsignature");
            Delete.Table("campaigndocumentsignature");

            Delete.Index("ixcampaigndocumentproviderdocumentid").OnTable("campaigndocument");
            Delete.Index("ixcampaigndocumenttemplateid").OnTable("campaigndocument");
            Delete.ForeignKey("fkcampaigndocumentcampaigndocumenttemplatetemplateid").OnTable("campaigndocument");

            Delete.Column("signeddocumenturl").FromTable("campaigndocument");
            Delete.Column("providerdocumentid").FromTable("campaigndocument");
            Delete.Column("provider").FromTable("campaigndocument");
            Delete.Column("body").FromTable("campaigndocument");
            Delete.Column("templateid").FromTable("campaigndocument");

            Delete.Index("ixcampaigndocumenttemplatedocumenttype").OnTable("campaigndocumenttemplate");
            Delete.Table("campaigndocumenttemplate");
        }
    }
}
