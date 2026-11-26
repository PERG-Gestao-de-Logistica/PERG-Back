using Microsoft.EntityFrameworkCore;
using Perg.Api.Domains;

namespace Perg.Api.Contexts;

// Tabela de valores pagos aos entregadores por loja (criada depois do scaffold, escrita à mão)
public partial class PergDbContext
{
    public virtual DbSet<ValorVeiculoEmpresa> ValoresVeiculo { get; set; }

    private static void ConfigurarValoresVeiculo(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ValorVeiculoEmpresa>(entity =>
        {
            entity.HasKey(e => new { e.EmpresaId, e.TipoVeiculo }).HasName("empresa_valores_veiculo_pkey");
            entity.ToTable("empresa_valores_veiculo");

            entity.Property(e => e.EmpresaId).HasColumnName("empresa_id");
            entity.Property(e => e.TipoVeiculo).HasMaxLength(20).HasColumnName("tipo_veiculo");
            entity.Property(e => e.BaseRota).HasPrecision(8, 2).HasColumnName("base_rota");
            entity.Property(e => e.PorKm).HasPrecision(8, 2).HasColumnName("por_km");
            entity.Property(e => e.PorEntrega).HasPrecision(8, 2).HasColumnName("por_entrega");
            entity.Property(e => e.CriadoEm).HasDefaultValueSql("now()").HasColumnName("criado_em");
            entity.Property(e => e.AtualizadoEm).HasDefaultValueSql("now()").HasColumnName("atualizado_em");
        });
    }
}
