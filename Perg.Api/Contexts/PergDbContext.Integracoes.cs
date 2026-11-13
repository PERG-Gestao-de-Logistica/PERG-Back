using Microsoft.EntityFrameworkCore;
using Perg.Api.Domains;

namespace Perg.Api.Contexts;

// Tabelas da integração com sistemas de vendas (escritas à mão, depois do scaffold)
public partial class PergDbContext
{
    public virtual DbSet<ChaveApi> ChavesApi { get; set; }
    public virtual DbSet<Webhook> Webhooks { get; set; }
    public virtual DbSet<WebhookEntrega> WebhookEntregas { get; set; }

    private static void ConfigurarIntegracoes(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChaveApi>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("chaves_api_pkey");
            entity.ToTable("chaves_api");
            entity.HasIndex(e => e.HashChave, "chaves_api_hash_chave_key").IsUnique();
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.EmpresaId).HasColumnName("empresa_id");
            entity.Property(e => e.Nome).HasMaxLength(100).HasColumnName("nome");
            entity.Property(e => e.Prefixo).HasMaxLength(16).HasColumnName("prefixo");
            entity.Property(e => e.HashChave).HasMaxLength(64).HasColumnName("hash_chave");
            entity.Property(e => e.CriadoPor).HasColumnName("criado_por");
            entity.Property(e => e.CriadoEm).HasDefaultValueSql("now()").HasColumnName("criado_em");
            entity.Property(e => e.UltimoUsoEm).HasColumnName("ultimo_uso_em");
            entity.Property(e => e.RevogadaEm).HasColumnName("revogada_em");
        });

        modelBuilder.Entity<Webhook>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("webhooks_pkey");
            entity.ToTable("webhooks");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.EmpresaId).HasColumnName("empresa_id");
            entity.Property(e => e.Url).HasColumnName("url");
            entity.Property(e => e.Segredo).HasMaxLength(64).HasColumnName("segredo");
            entity.Property(e => e.Ativo).HasColumnName("ativo");
            entity.Property(e => e.FalhasSeguidas).HasColumnName("falhas_seguidas");
            entity.Property(e => e.CriadoEm).HasDefaultValueSql("now()").HasColumnName("criado_em");
            entity.Property(e => e.AtualizadoEm).HasDefaultValueSql("now()").HasColumnName("atualizado_em");
        });

        modelBuilder.Entity<WebhookEntrega>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("webhook_entregas_pkey");
            entity.ToTable("webhook_entregas");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.WebhookId).HasColumnName("webhook_id");
            entity.Property(e => e.EmpresaId).HasColumnName("empresa_id");
            entity.Property(e => e.Evento).HasMaxLength(50).HasColumnName("evento");
            entity.Property(e => e.Payload).HasColumnType("jsonb").HasColumnName("payload");
            entity.Property(e => e.Status).HasMaxLength(20).HasColumnName("status");
            entity.Property(e => e.Tentativas).HasColumnName("tentativas");
            entity.Property(e => e.ProximaTentativaEm).HasColumnName("proxima_tentativa_em");
            entity.Property(e => e.UltimoStatusHttp).HasColumnName("ultimo_status_http");
            entity.Property(e => e.UltimoErro).HasColumnName("ultimo_erro");
            entity.Property(e => e.EntregueEm).HasColumnName("entregue_em");
            entity.Property(e => e.CriadoEm).HasDefaultValueSql("now()").HasColumnName("criado_em");
            entity.HasOne(d => d.Webhook).WithMany().HasForeignKey(d => d.WebhookId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
