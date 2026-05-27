using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605270003)]
    public sealed class Migration_202605270003_RetireOpportunityNegotiation : Migration
    {
        public override void Up()
        {
            // Proposta passa a carregar desconto e prazo de pagamento.
            Execute.Sql(@"
                ALTER TABLE proposal ADD COLUMN IF NOT EXISTS discountpercent NUMERIC(5,2);
                ALTER TABLE proposal ADD COLUMN IF NOT EXISTS paymenttermdays INT;
            ");

            // Aprovacao interna passa a referenciar a proposta. Dados de producao sao descartaveis:
            // re-aponta a FK sem migrar dados das negociacoes existentes.
            Execute.Sql(@"
                ALTER TABLE opportunityapprovalrequest DROP CONSTRAINT IF EXISTS fkopportunityapprovalrequestnegotiation;
                DROP INDEX IF EXISTS ixopportunityapprovalrequestnegotiationid;
                ALTER TABLE opportunityapprovalrequest DROP COLUMN IF EXISTS opportunitynegotiationid;
                ALTER TABLE opportunityapprovalrequest ADD COLUMN IF NOT EXISTS proposalid BIGINT;
                DELETE FROM opportunityapprovalrequest WHERE proposalid IS NULL;
                ALTER TABLE opportunityapprovalrequest ALTER COLUMN proposalid SET NOT NULL;
                ALTER TABLE opportunityapprovalrequest
                    ADD CONSTRAINT fkopportunityapprovalrequestproposal
                    FOREIGN KEY (proposalid) REFERENCES proposal (id) ON DELETE CASCADE;
                CREATE INDEX IF NOT EXISTS ixopportunityapprovalrequestproposalid
                    ON opportunityapprovalrequest (proposalid);
            ");

            // Entidade negociacao aposentada.
            Execute.Sql(@"
                DROP TABLE IF EXISTS opportunitynegotiation CASCADE;
            ");
        }

        public override void Down()
        {
            // Recria a tabela de negociacoes vazia e restaura a FK antiga da aprovacao.
            Execute.Sql(@"
                CREATE TABLE IF NOT EXISTS opportunitynegotiation (
                    id BIGSERIAL PRIMARY KEY,
                    opportunityid BIGINT NOT NULL,
                    title VARCHAR(150) NOT NULL,
                    amount NUMERIC(18,2) NOT NULL,
                    status INT NOT NULL DEFAULT 1,
                    negotiatedat TIMESTAMPTZ NOT NULL,
                    notes VARCHAR(1000),
                    discountpercent NUMERIC(5,2),
                    marginpercent NUMERIC(5,2),
                    paymenttermdays INT,
                    createdat TIMESTAMPTZ NOT NULL,
                    updatedat TIMESTAMPTZ
                );
            ");

            Execute.Sql(@"
                ALTER TABLE opportunityapprovalrequest DROP CONSTRAINT IF EXISTS fkopportunityapprovalrequestproposal;
                DROP INDEX IF EXISTS ixopportunityapprovalrequestproposalid;
                ALTER TABLE opportunityapprovalrequest DROP COLUMN IF EXISTS proposalid;
                ALTER TABLE opportunityapprovalrequest ADD COLUMN IF NOT EXISTS opportunitynegotiationid BIGINT;
                DELETE FROM opportunityapprovalrequest WHERE opportunitynegotiationid IS NULL;
                ALTER TABLE opportunityapprovalrequest ALTER COLUMN opportunitynegotiationid SET NOT NULL;
                ALTER TABLE opportunityapprovalrequest
                    ADD CONSTRAINT fkopportunityapprovalrequestnegotiation
                    FOREIGN KEY (opportunitynegotiationid) REFERENCES opportunitynegotiation (id);
                CREATE INDEX IF NOT EXISTS ixopportunityapprovalrequestnegotiationid
                    ON opportunityapprovalrequest (opportunitynegotiationid);
            ");

            Execute.Sql(@"
                ALTER TABLE proposal DROP COLUMN IF EXISTS discountpercent;
                ALTER TABLE proposal DROP COLUMN IF EXISTS paymenttermdays;
            ");
        }
    }
}
