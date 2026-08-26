using Estoque.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Estoque.Api.Data;

public class EstoqueDbContext(DbContextOptions<EstoqueDbContext> options) : DbContext(options)
{
    public DbSet<Produto> Produtos => Set<Produto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Produto>(entity =>
        {
            entity.HasKey(p => p.Codigo);
            entity.Property(p => p.Codigo).HasMaxLength(30);
            entity.Property(p => p.Descricao).HasMaxLength(200).IsRequired();
            entity.Property(p => p.Saldo).IsRequired();
            entity.ToTable(t => t.HasCheckConstraint("CK_Produto_Saldo_NaoNegativo", "\"Saldo\" >= 0"));

        });
    }
}