using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605180008)]
    public sealed class Migration_202605180008_AddBankCatalog : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                CREATE TABLE IF NOT EXISTS bank (
                    id BIGSERIAL PRIMARY KEY,
                    compe VARCHAR(3) NOT NULL,
                    ispb VARCHAR(8) NULL,
                    name VARCHAR(160) NOT NULL,
                    shortname VARCHAR(80) NOT NULL,
                    logourl VARCHAR(500) NULL,
                    isactive BOOLEAN NOT NULL DEFAULT true,
                    issystem BOOLEAN NOT NULL DEFAULT false,
                    createdat TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    updatedat TIMESTAMPTZ NULL
                );
            ");

            Execute.Sql("CREATE UNIQUE INDEX IF NOT EXISTS ux_bank_compe ON bank(compe);");

            Execute.Sql(@"
                INSERT INTO bank (compe, ispb, name, shortname, isactive, issystem, createdat, updatedat) VALUES
                    ('001', '00000000', 'Banco do Brasil S.A.',                                'BB',             true, true, NOW(), NOW()),
                    ('003', '04902979', 'Banco da Amazônia S.A.',                              'Amazônia',       true, true, NOW(), NOW()),
                    ('004', '07237373', 'Banco do Nordeste do Brasil S.A.',                    'BNB',            true, true, NOW(), NOW()),
                    ('033', '90400888', 'Banco Santander (Brasil) S.A.',                       'Santander',      true, true, NOW(), NOW()),
                    ('041', '92702067', 'Banco do Estado do Rio Grande do Sul S.A.',           'Banrisul',       true, true, NOW(), NOW()),
                    ('070', '00000208', 'Banco de Brasília S.A.',                              'BRB',            true, true, NOW(), NOW()),
                    ('077', '00416968', 'Banco Inter S.A.',                                    'Inter',          true, true, NOW(), NOW()),
                    ('085', '05463212', 'Cooperativa Central de Crédito - Ailos',              'Ailos',          true, true, NOW(), NOW()),
                    ('104', '00360305', 'Caixa Econômica Federal',                             'Caixa',          true, true, NOW(), NOW()),
                    ('197', '16501555', 'Stone Pagamentos S.A.',                               'Stone',          true, true, NOW(), NOW()),
                    ('208', '30306294', 'Banco BTG Pactual S.A.',                              'BTG Pactual',    true, true, NOW(), NOW()),
                    ('212', '92894922', 'Banco Original S.A.',                                 'Original',       true, true, NOW(), NOW()),
                    ('218', '71027866', 'Banco BS2 S.A.',                                      'BS2',            true, true, NOW(), NOW()),
                    ('237', '60746948', 'Banco Bradesco S.A.',                                 'Bradesco',       true, true, NOW(), NOW()),
                    ('246', '28195667', 'Banco ABC Brasil S.A.',                               'ABC Brasil',     true, true, NOW(), NOW()),
                    ('260', '18236120', 'Nu Pagamentos S.A.',                                  'Nubank',         true, true, NOW(), NOW()),
                    ('290', '08561701', 'PagSeguro Internet S.A.',                             'PagBank',        true, true, NOW(), NOW()),
                    ('318', '61186680', 'Banco BMG S.A.',                                      'BMG',            true, true, NOW(), NOW()),
                    ('323', '10573521', 'Mercado Pago Instituição de Pagamento Ltda.',         'Mercado Pago',   true, true, NOW(), NOW()),
                    ('336', '31872495', 'Banco C6 S.A.',                                       'C6',             true, true, NOW(), NOW()),
                    ('341', '60701190', 'Itaú Unibanco S.A.',                                  'Itaú',           true, true, NOW(), NOW()),
                    ('364', '09089356', 'Efí S.A. - Instituição de Pagamento',                 'Efí',            true, true, NOW(), NOW()),
                    ('376', '33172537', 'Banco J.P. Morgan S.A.',                              'JPMorgan',       true, true, NOW(), NOW()),
                    ('380', '22896431', 'PicPay Serviços S.A.',                                'PicPay',         true, true, NOW(), NOW()),
                    ('389', '17184037', 'Banco Mercantil do Brasil S.A.',                      'Mercantil',      true, true, NOW(), NOW()),
                    ('422', '58160789', 'Banco Safra S.A.',                                    'Safra',          true, true, NOW(), NOW()),
                    ('477', '33042953', 'Citibank N.A.',                                       'Citibank',       true, true, NOW(), NOW()),
                    ('655', '59109165', 'Banco Votorantim S.A.',                               'BV',             true, true, NOW(), NOW()),
                    ('748', '01181521', 'Banco Cooperativo Sicredi S.A.',                      'Sicredi',        true, true, NOW(), NOW()),
                    ('756', '02038232', 'Banco Cooperativo do Brasil S.A. - Bancoob',          'Sicoob',         true, true, NOW(), NOW())
                ON CONFLICT (compe) DO NOTHING;
            ");

            Execute.Sql("ALTER TABLE financialaccount ADD COLUMN IF NOT EXISTS bankid BIGINT NULL;");

            Execute.Sql(@"
                UPDATE financialaccount fa
                SET bankid = b.id
                FROM bank b
                WHERE fa.bankid IS NULL
                  AND fa.bank IS NOT NULL
                  AND LOWER(TRIM(fa.bank)) IN (LOWER(b.shortname), LOWER(b.name));
            ");

            Execute.Sql("CREATE INDEX IF NOT EXISTS ix_financialaccount_bankid ON financialaccount(bankid);");

            Execute.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.table_constraints
                        WHERE constraint_name = 'fk_financialaccount_bank'
                    ) THEN
                        ALTER TABLE financialaccount
                            ADD CONSTRAINT fk_financialaccount_bank
                            FOREIGN KEY (bankid)
                            REFERENCES bank(id)
                            ON DELETE SET NULL;
                    END IF;
                END $$;
            ");
        }

        public override void Down()
        {
            Execute.Sql("ALTER TABLE financialaccount DROP CONSTRAINT IF EXISTS fk_financialaccount_bank;");
            Execute.Sql("DROP INDEX IF EXISTS ix_financialaccount_bankid;");
            Execute.Sql("ALTER TABLE financialaccount DROP COLUMN IF EXISTS bankid;");
            Execute.Sql("DROP INDEX IF EXISTS ux_bank_compe;");
            Execute.Sql("DROP TABLE IF EXISTS bank;");
        }
    }
}
