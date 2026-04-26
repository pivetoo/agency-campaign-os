using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202604270001)]
    public sealed class Migration_202604270001_AddCommercialResponsible : Migration
    {
        public override void Up()
        {
            Create.Table("commercialresponsible")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("name").AsString(150).NotNullable()
                .WithColumn("email").AsString(255).Nullable()
                .WithColumn("phone").AsString(50).Nullable()
                .WithColumn("notes").AsString(1000).Nullable()
                .WithColumn("isactive").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            if (Schema.Table("opportunity").Column("internalownername").Exists())
            {
                Delete.Column("internalownername").FromTable("opportunity");
            }

            Alter.Table("opportunity")
                .AddColumn("commercialresponsibleid").AsInt64().Nullable();

            Create.Index("ixopportunitycommercialresponsibleid")
                .OnTable("opportunity")
                .OnColumn("commercialresponsibleid").Ascending();

            Create.ForeignKey("fkopportunitycommercialresponsible")
                .FromTable("opportunity").ForeignColumn("commercialresponsibleid")
                .ToTable("commercialresponsible").PrimaryColumn("id")
                .OnDelete(System.Data.Rule.SetNull);
        }

        public override void Down()
        {
            Delete.ForeignKey("fkopportunitycommercialresponsible").OnTable("opportunity");
            Delete.Index("ixopportunitycommercialresponsibleid").OnTable("opportunity");
            Delete.Column("commercialresponsibleid").FromTable("opportunity");

            Alter.Table("opportunity")
                .AddColumn("internalownername").AsString(150).Nullable();

            Delete.Table("commercialresponsible");
        }
    }
}
