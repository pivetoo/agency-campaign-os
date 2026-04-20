using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202604200002)]
    public sealed class Migration_202604200002_AddCommercialApprovals : Migration
    {
        public override void Up()
        {
            Alter.Table("opportunitynegotiation")
                .AddColumn("status").AsInt32().NotNullable().WithDefaultValue(1);

            Create.Table("opportunityapprovalrequest")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("opportunitynegotiationid").AsInt64().NotNullable()
                .WithColumn("approvaltype").AsInt32().NotNullable()
                .WithColumn("status").AsInt32().NotNullable().WithDefaultValue(1)
                .WithColumn("reason").AsString(1000).NotNullable()
                .WithColumn("requestedbyuserid").AsInt64().Nullable()
                .WithColumn("requestedbyusername").AsString(150).NotNullable()
                .WithColumn("approvedbyuserid").AsInt64().Nullable()
                .WithColumn("approvedbyusername").AsString(150).Nullable()
                .WithColumn("requestedat").AsDateTimeOffset().NotNullable()
                .WithColumn("decidedat").AsDateTimeOffset().Nullable()
                .WithColumn("decisionnotes").AsString(1000).Nullable()
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.ForeignKey("fkopportunityapprovalrequestnegotiation")
                .FromTable("opportunityapprovalrequest").ForeignColumn("opportunitynegotiationid")
                .ToTable("opportunitynegotiation").PrimaryColumn("id");

            Create.Index("ixopportunityapprovalrequestnegotiationid")
                .OnTable("opportunityapprovalrequest")
                .OnColumn("opportunitynegotiationid").Ascending();

            Create.Index("ixopportunityapprovalrequeststatus")
                .OnTable("opportunityapprovalrequest")
                .OnColumn("status").Ascending();
        }

        public override void Down()
        {
            Delete.Index("ixopportunityapprovalrequeststatus").OnTable("opportunityapprovalrequest");
            Delete.Index("ixopportunityapprovalrequestnegotiationid").OnTable("opportunityapprovalrequest");
            Delete.ForeignKey("fkopportunityapprovalrequestnegotiation").OnTable("opportunityapprovalrequest");
            Delete.Table("opportunityapprovalrequest");
            Delete.Column("status").FromTable("opportunitynegotiation");
        }
    }
}
