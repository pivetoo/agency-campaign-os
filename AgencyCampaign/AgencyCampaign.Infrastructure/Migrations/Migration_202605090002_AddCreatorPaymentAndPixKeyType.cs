using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605090002)]
    public sealed class Migration_202605090002_AddCreatorPaymentAndPixKeyType : Migration
    {
        public override void Up()
        {
            Alter.Table("creator")
                .AddColumn("pixkeytype").AsInt32().Nullable();

            Create.Table("creatorpayment")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("campaigncreatorid").AsInt64().NotNullable()
                .WithColumn("creatorid").AsInt64().NotNullable()
                .WithColumn("campaigndocumentid").AsInt64().Nullable()
                .WithColumn("grossamount").AsDecimal(18, 2).NotNullable()
                .WithColumn("discounts").AsDecimal(18, 2).NotNullable().WithDefaultValue(0)
                .WithColumn("netamount").AsDecimal(18, 2).NotNullable()
                .WithColumn("description").AsString(500).Nullable()
                .WithColumn("method").AsInt32().NotNullable().WithDefaultValue(1)
                .WithColumn("status").AsInt32().NotNullable().WithDefaultValue(1)
                .WithColumn("provider").AsString(50).Nullable()
                .WithColumn("providertransactionid").AsString(150).Nullable()
                .WithColumn("pixkey").AsString(150).Nullable()
                .WithColumn("pixkeytype").AsInt32().Nullable()
                .WithColumn("invoicenumber").AsString(50).Nullable()
                .WithColumn("invoiceurl").AsString(1000).Nullable()
                .WithColumn("invoiceissuedat").AsDateTimeOffset().Nullable()
                .WithColumn("scheduledfor").AsDateTimeOffset().Nullable()
                .WithColumn("paidat").AsDateTimeOffset().Nullable()
                .WithColumn("failedat").AsDateTimeOffset().Nullable()
                .WithColumn("failurereason").AsString(1000).Nullable()
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.ForeignKey("fkcreatorpaymentcampaigncreatorcampaigncreatorid")
                .FromTable("creatorpayment").ForeignColumn("campaigncreatorid")
                .ToTable("campaigncreator").PrimaryColumn("id");

            Create.ForeignKey("fkcreatorpaymentcreatorcreatorid")
                .FromTable("creatorpayment").ForeignColumn("creatorid")
                .ToTable("creator").PrimaryColumn("id");

            Create.ForeignKey("fkcreatorpaymentcampaigndocumentcampaigndocumentid")
                .FromTable("creatorpayment").ForeignColumn("campaigndocumentid")
                .ToTable("campaigndocument").PrimaryColumn("id");

            Create.Index("ixcreatorpaymentcampaigncreatorid")
                .OnTable("creatorpayment").OnColumn("campaigncreatorid").Ascending();

            Create.Index("ixcreatorpaymentcreatorid")
                .OnTable("creatorpayment").OnColumn("creatorid").Ascending();

            Create.Index("ixcreatorpaymentcampaigndocumentid")
                .OnTable("creatorpayment").OnColumn("campaigndocumentid").Ascending();

            Create.Index("ixcreatorpaymentstatus")
                .OnTable("creatorpayment").OnColumn("status").Ascending();

            Create.Index("ixcreatorpaymentprovidertransactionid")
                .OnTable("creatorpayment").OnColumn("providertransactionid").Ascending();

            Create.Table("creatorpaymentevent")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("creatorpaymentid").AsInt64().NotNullable()
                .WithColumn("eventtype").AsInt32().NotNullable()
                .WithColumn("occurredat").AsDateTimeOffset().NotNullable()
                .WithColumn("description").AsString(500).Nullable()
                .WithColumn("metadata").AsCustom("TEXT").Nullable()
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.ForeignKey("fkcreatorpaymenteventcreatorpaymentcreatorpaymentid")
                .FromTable("creatorpaymentevent").ForeignColumn("creatorpaymentid")
                .ToTable("creatorpayment").PrimaryColumn("id");

            Create.Index("ixcreatorpaymenteventcreatorpaymentid")
                .OnTable("creatorpaymentevent").OnColumn("creatorpaymentid").Ascending();
        }

        public override void Down()
        {
            Delete.Index("ixcreatorpaymenteventcreatorpaymentid").OnTable("creatorpaymentevent");
            Delete.ForeignKey("fkcreatorpaymenteventcreatorpaymentcreatorpaymentid").OnTable("creatorpaymentevent");
            Delete.Table("creatorpaymentevent");

            Delete.Index("ixcreatorpaymentprovidertransactionid").OnTable("creatorpayment");
            Delete.Index("ixcreatorpaymentstatus").OnTable("creatorpayment");
            Delete.Index("ixcreatorpaymentcampaigndocumentid").OnTable("creatorpayment");
            Delete.Index("ixcreatorpaymentcreatorid").OnTable("creatorpayment");
            Delete.Index("ixcreatorpaymentcampaigncreatorid").OnTable("creatorpayment");
            Delete.ForeignKey("fkcreatorpaymentcampaigndocumentcampaigndocumentid").OnTable("creatorpayment");
            Delete.ForeignKey("fkcreatorpaymentcreatorcreatorid").OnTable("creatorpayment");
            Delete.ForeignKey("fkcreatorpaymentcampaigncreatorcampaigncreatorid").OnTable("creatorpayment");
            Delete.Table("creatorpayment");

            Delete.Column("pixkeytype").FromTable("creator");
        }
    }
}
