using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    // Cobranca de recebivel (integracao IntegrationPlatform): emissao de boleto/PIX e baixa por webhook.
    [Migration(202606120003)]
    public sealed class Migration_202606120003_AddReceivableChargeColumns : Migration
    {
        public override void Up()
        {
            Alter.Table("financialentry")
                .AddColumn("chargeprovider").AsString(50).Nullable()
                .AddColumn("chargeid").AsString(150).Nullable()
                .AddColumn("chargeurl").AsString(1000).Nullable()
                .AddColumn("chargestatus").AsInt32().NotNullable().WithDefaultValue(0)
                .AddColumn("chargeissuedat").AsDateTimeOffset().Nullable();

            // Lookup de correlacao no callback do provedor (provider + chargeid).
            Create.Index("ixfinancialentrychargeid")
                .OnTable("financialentry")
                .OnColumn("chargeid").Ascending();
        }

        public override void Down()
        {
            Delete.Index("ixfinancialentrychargeid").OnTable("financialentry");
            Delete.Column("chargeprovider")
                .Column("chargeid")
                .Column("chargeurl")
                .Column("chargestatus")
                .Column("chargeissuedat")
                .FromTable("financialentry");
        }
    }
}
