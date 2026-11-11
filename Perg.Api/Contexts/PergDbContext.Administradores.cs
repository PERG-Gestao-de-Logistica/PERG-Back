using Microsoft.EntityFrameworkCore;
using Perg.Api.Domains;

namespace Perg.Api.Contexts;

// Tabela e colunas do backoffice, criadas depois do scaffold (escritas à mão, como combinado)
public partial class PergDbContext
{
    public virtual DbSet<Administrador> Administradores { get; set; }

    private static void ConfigurarAdministradores(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Administrador>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("administradores_pkey");
            entity.ToTable("administradores");
            entity.HasIndex(e => e.Email, "administradores_email_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Nome).HasMaxLength(150).HasColumnName("nome");
            entity.Property(e => e.Email).HasMaxLength(255).HasColumnName("email");
            entity.Property(e => e.SenhaHash).HasColumnName("senha_hash");
            entity.Property(e => e.TrocarSenha).HasColumnName("trocar_senha");
            entity.Property(e => e.Ativo).HasColumnName("ativo");
            entity.Property(e => e.UltimoAcessoEm).HasColumnName("ultimo_acesso_em");
            entity.Property(e => e.CriadoEm).HasDefaultValueSql("now()").HasColumnName("criado_em");
            entity.Property(e => e.AtualizadoEm).HasDefaultValueSql("now()").HasColumnName("atualizado_em");
        });

        modelBuilder.Entity<DocumentoEntregador>().Property(e => e.AnalisadoPor).HasColumnName("analisado_por");
        modelBuilder.Entity<Entregador>().Property(e => e.AprovadoPor).HasColumnName("aprovado_por");
    }
}
