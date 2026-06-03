using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF
{
    internal static class ConcurrencyTokenExtensions
    {
        // Concorrencia otimista (D5i): usa a coluna de sistema xmin do Postgres como token de versao.
        // Em writes concorrentes na mesma linha, o segundo SaveChanges lanca DbUpdateConcurrencyException
        // em vez de sobrescrever silenciosamente (lost update). Sob InMemory o token nao e enforced.
        public static void UseXminConcurrencyToken(this EntityTypeBuilder builder)
        {
            builder.Property<uint>("xmin")
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();
        }
    }
}
