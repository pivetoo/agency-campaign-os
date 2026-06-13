using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    // Artefatos bancarios reais da cobranca: boleto (linha digitavel, codigo de barras, nosso numero, PDF)
    // e PIX (copia-e-cola/EMV, QR, txid). Preenchidos pelo conector do IntegrationPlatform no callback.
    [Migration(202606130001)]
    public sealed class Migration_202606130001_AddChargeArtifacts : Migration
    {
        public override void Up()
        {
            Alter.Table("financialentry")
                .AddColumn("chargedigitableline").AsString(60).Nullable()
                .AddColumn("chargebarcode").AsString(48).Nullable()
                .AddColumn("chargenossonumero").AsString(40).Nullable()
                .AddColumn("chargepixcopypaste").AsString(1024).Nullable()
                .AddColumn("chargepixqrcodeurl").AsString(1000).Nullable()
                .AddColumn("chargetxid").AsString(40).Nullable()
                .AddColumn("chargebankslipurl").AsString(1000).Nullable();
        }

        public override void Down()
        {
            Delete.Column("chargedigitableline")
                .Column("chargebarcode")
                .Column("chargenossonumero")
                .Column("chargepixcopypaste")
                .Column("chargepixqrcodeurl")
                .Column("chargetxid")
                .Column("chargebankslipurl")
                .FromTable("financialentry");
        }
    }
}
