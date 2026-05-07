using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605070004)]
    public sealed class Migration_202605070004_LinkCommercialResponsibleToUser : Migration
    {
        public override void Up()
        {
            Alter.Table("commercialresponsible")
                .AddColumn("userid").AsInt64().Nullable();

            Execute.Sql("DELETE FROM commercialresponsible WHERE userid IS NULL;");

            Alter.Column("userid").OnTable("commercialresponsible").AsInt64().NotNullable();

            Create.Index("ixcommercialresponsibleuserid")
                .OnTable("commercialresponsible")
                .OnColumn("userid").Ascending()
                .WithOptions().Unique();
        }

        public override void Down()
        {
            Delete.Index("ixcommercialresponsibleuserid").OnTable("commercialresponsible");
            Delete.Column("userid").FromTable("commercialresponsible");
        }
    }
}
