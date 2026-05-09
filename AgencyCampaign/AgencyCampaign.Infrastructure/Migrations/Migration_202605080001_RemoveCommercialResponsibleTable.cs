using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605080001)]
    public sealed class Migration_202605080001_RemoveCommercialResponsibleTable : Migration
    {
        public override void Up()
        {
            if (!Schema.Table("opportunity").Column("responsibleuserid").Exists())
            {
                Alter.Table("opportunity")
                    .AddColumn("responsibleuserid").AsInt64().Nullable();
            }

            if (!Schema.Table("opportunity").Column("responsibleusername").Exists())
            {
                Alter.Table("opportunity")
                    .AddColumn("responsibleusername").AsString(150).Nullable();
            }

            if (Schema.Table("commercialresponsible").Exists())
            {
                Execute.Sql(@"
                    UPDATE opportunity
                    SET responsibleuserid = cr.userid,
                        responsibleusername = cr.name
                    FROM commercialresponsible cr
                    WHERE opportunity.commercialresponsibleid = cr.id;
                ");

                Execute.Sql(@"
                    UPDATE proposal
                    SET internalownerid = cr.userid
                    FROM commercialresponsible cr
                    WHERE proposal.internalownerid = cr.id;
                ");
            }

            if (Schema.Table("opportunity").Column("commercialresponsibleid").Exists())
            {
                Delete.ForeignKey("fkopportunitycommercialresponsible").OnTable("opportunity");

                if (Schema.Table("opportunity").Index("ixopportunitycommercialresponsibleid").Exists())
                {
                    Delete.Index("ixopportunitycommercialresponsibleid").OnTable("opportunity");
                }

                Delete.Column("commercialresponsibleid").FromTable("opportunity");
            }

            if (!Schema.Table("opportunity").Index("ixopportunityresponsibleuserid").Exists())
            {
                Create.Index("ixopportunityresponsibleuserid")
                    .OnTable("opportunity")
                    .OnColumn("responsibleuserid").Ascending();
            }

            if (!Schema.Table("proposal").Index("ixproposalinternalownerid").Exists())
            {
                Create.Index("ixproposalinternalownerid")
                    .OnTable("proposal")
                    .OnColumn("internalownerid").Ascending();
            }

            if (Schema.Table("commercialresponsible").Exists())
            {
                Delete.Table("commercialresponsible");
            }
        }

        public override void Down()
        {
            throw new System.NotSupportedException(
                "Migration RemoveCommercialResponsibleTable nao suporta rollback. " +
                "Os ids armazenados em opportunity.responsibleuserid e proposal.internalownerid passam a referenciar users.id no IdentityManagement, " +
                "e a tabela commercialresponsible foi descartada.");
        }
    }
}
