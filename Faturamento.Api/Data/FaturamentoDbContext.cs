using Faturamento.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Faturamento.Api.Data;

public class FaturamentoDbContext(DbContextOptions<FaturamentoDbContext> options) : DbContext(options)
{
    public DbSet<NotaFiscal> NotasFiscais => Set<NotaFiscal>();
    public DbSet<ItemNotaFiscal> ItensNotaFiscal => Set<ItemNotaFiscal>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NotaFiscal>(entity =>
        {
            entity.HasKey(n => n.Numero);
            entity.Property(n => n.Numero).ValueGeneratedOnAdd();
            entity.Property(n => n.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(n => n.DataCriacao).IsRequired();

            entity.HasMany(n => n.Itens)
                  .WithOne(i => i.NotaFiscal)
                  .HasForeignKey(i => i.NotaFiscalId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ItemNotaFiscal>(entity =>
        {
            entity.HasKey(i => i.Id);
            entity.Property(i => i.CodigoProduto).HasMaxLength(30).IsRequired();
            entity.Property(i => i.DescricaoProduto).HasMaxLength(200).IsRequired();
            entity.Property(i => i.Quantidade).IsRequired();
            entity.ToTable(t => t.HasCheckConstraint("CK_Item_Quantidade_Positiva", "\"Quantidade\" > 0"));
        });
    }
}
