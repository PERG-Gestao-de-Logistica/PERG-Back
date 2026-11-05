using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Perg.Api.Domains;
using Perg.Api.Domains.Enums;

namespace Perg.Api.Contexts;

public partial class PergDbContext : DbContext
{
    public PergDbContext(DbContextOptions<PergDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Assinatura> Assinaturas { get; set; }

    public virtual DbSet<Avaliacao> Avaliacoes { get; set; }

    public virtual DbSet<CarteiraLancamento> CarteiraLancamentos { get; set; }

    public virtual DbSet<DocumentoEntregador> DocumentosEntregador { get; set; }

    public virtual DbSet<EmpresaFavorito> EmpresaFavoritos { get; set; }

    public virtual DbSet<Empresa> Empresas { get; set; }

    public virtual DbSet<Entregador> Entregadores { get; set; }

    public virtual DbSet<Importacao> Importacoes { get; set; }

    public virtual DbSet<Jornada> Jornadas { get; set; }

    public virtual DbSet<Mensagem> Mensagens { get; set; }

    public virtual DbSet<MensagemProcessada> MensagensProcessadas { get; set; }

    public virtual DbSet<Notificacao> Notificacoes { get; set; }

    public virtual DbSet<Ocorrencia> Ocorrencias { get; set; }

    public virtual DbSet<OfertaRota> OfertasRota { get; set; }

    public virtual DbSet<Origem> Origens { get; set; }

    public virtual DbSet<Outbox> Outbox { get; set; }

    public virtual DbSet<Parada> Paradas { get; set; }

    public virtual DbSet<PedidoEvento> PedidoEventos { get; set; }

    public virtual DbSet<Pedido> Pedidos { get; set; }

    public virtual DbSet<Plano> Planos { get; set; }

    public virtual DbSet<PosicaoEntregador> PosicoesEntregador { get; set; }

    public virtual DbSet<Rota> Rotas { get; set; }

    public virtual DbSet<Roteirizacao> Roteirizacoes { get; set; }

    public virtual DbSet<Saque> Saques { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

    public virtual DbSet<UsuarioLoginExterno> UsuariosLoginsExternos { get; set; }

    public virtual DbSet<Veiculo> Veiculos { get; set; }

    public virtual DbSet<VwEntregadorRede> VwEntregadoresRede { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Assinatura>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("assinaturas_pkey");

            entity.ToTable("assinaturas");

            entity.HasIndex(e => e.EmpresaId, "ux_assinaturas_vigente")
                .IsUnique()
                .HasFilter("(status <> 3)");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.AtualizadoEm)
                .HasDefaultValueSql("now()")
                .HasColumnName("atualizado_em");
            entity.Property(e => e.CanceladaEm).HasColumnName("cancelada_em");
            entity.Property(e => e.CriadoEm)
                .HasDefaultValueSql("now()")
                .HasColumnName("criado_em");
            entity.Property(e => e.EmpresaId).HasColumnName("empresa_id");
            entity.Property(e => e.Gateway)
                .HasMaxLength(20)
                .HasColumnName("gateway");
            entity.Property(e => e.GatewayAssinaturaId)
                .HasMaxLength(100)
                .HasColumnName("gateway_assinatura_id");
            entity.Property(e => e.PlanoId).HasColumnName("plano_id");
            entity.Property(e => e.ProximoVencimento).HasColumnName("proximo_vencimento");
            entity.Property(e => e.Status)
                .HasDefaultValue((short)1)
                .HasColumnName("status");

            entity.HasOne(d => d.Empresa).WithMany(p => p.Assinaturas)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("assinaturas_empresa_id_fkey");

            entity.HasOne(d => d.Plano).WithMany(p => p.Assinaturas)
                .HasForeignKey(d => d.PlanoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("assinaturas_plano_id_fkey");
        });

        modelBuilder.Entity<Avaliacao>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("avaliacoes_pkey");

            entity.ToTable("avaliacoes");

            entity.HasIndex(e => e.RotaId, "avaliacoes_rota_id_key").IsUnique();

            entity.HasIndex(e => e.EntregadorId, "ix_avaliacoes_entregador");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.AvaliadoPor).HasColumnName("avaliado_por");
            entity.Property(e => e.Comentario).HasColumnName("comentario");
            entity.Property(e => e.CriadoEm)
                .HasDefaultValueSql("now()")
                .HasColumnName("criado_em");
            entity.Property(e => e.EmpresaId).HasColumnName("empresa_id");
            entity.Property(e => e.EntregadorId).HasColumnName("entregador_id");
            entity.Property(e => e.Nota).HasColumnName("nota");
            entity.Property(e => e.RotaId).HasColumnName("rota_id");

            entity.HasOne(d => d.AvaliadoPorNavigation).WithMany(p => p.Avaliacoes)
                .HasForeignKey(d => d.AvaliadoPor)
                .HasConstraintName("avaliacoes_avaliado_por_fkey");

            entity.HasOne(d => d.Empresa).WithMany(p => p.Avaliacoes)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("avaliacoes_empresa_id_fkey");

            entity.HasOne(d => d.Entregador).WithMany(p => p.Avaliacoes)
                .HasForeignKey(d => d.EntregadorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("avaliacoes_entregador_id_fkey");

            entity.HasOne(d => d.Rota).WithOne(p => p.Avaliacao)
                .HasForeignKey<Avaliacao>(d => d.RotaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("avaliacoes_rota_id_fkey");
        });

        modelBuilder.Entity<CarteiraLancamento>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("carteira_lancamentos_pkey");

            entity.ToTable("carteira_lancamentos");

            entity.HasIndex(e => new { e.EntregadorId, e.CriadoEm }, "ix_lancamentos_entregador").IsDescending(false, true);

            entity.HasIndex(e => e.RotaId, "ux_lancamentos_rota")
                .IsUnique()
                .HasFilter("((tipo)::text = 'rota'::text)");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CriadoEm)
                .HasDefaultValueSql("now()")
                .HasColumnName("criado_em");
            entity.Property(e => e.Descricao)
                .HasMaxLength(255)
                .HasColumnName("descricao");
            entity.Property(e => e.EntregadorId).HasColumnName("entregador_id");
            entity.Property(e => e.RotaId).HasColumnName("rota_id");
            entity.Property(e => e.SaqueId).HasColumnName("saque_id");
            entity.Property(e => e.Tipo)
                .HasMaxLength(20)
                .HasColumnName("tipo");
            entity.Property(e => e.Valor)
                .HasPrecision(12, 2)
                .HasColumnName("valor");

            entity.HasOne(d => d.Entregador).WithMany(p => p.CarteiraLancamentos)
                .HasForeignKey(d => d.EntregadorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("carteira_lancamentos_entregador_id_fkey");

            entity.HasOne(d => d.Rota).WithMany(p => p.CarteiraLancamentos)
                .HasForeignKey(d => d.RotaId)
                .HasConstraintName("carteira_lancamentos_rota_id_fkey");

            entity.HasOne(d => d.Saque).WithMany(p => p.CarteiraLancamentos)
                .HasForeignKey(d => d.SaqueId)
                .HasConstraintName("carteira_lancamentos_saque_id_fkey");
        });

        modelBuilder.Entity<DocumentoEntregador>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("documentos_entregador_pkey");

            entity.ToTable("documentos_entregador");

            entity.HasIndex(e => new { e.EntregadorId, e.Tipo }, "ix_documentos_entregador");

            entity.HasIndex(e => e.Numero, "ux_documentos_cnh")
                .IsUnique()
                .HasFilter("(((tipo)::text = 'cnh'::text) AND ((status)::text <> 'rejeitado'::text))");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.AnalisadoEm).HasColumnName("analisado_em");
            entity.Property(e => e.ArquivoChave).HasColumnName("arquivo_chave");
            entity.Property(e => e.Categoria)
                .HasMaxLength(2)
                .HasColumnName("categoria");
            entity.Property(e => e.CriadoEm)
                .HasDefaultValueSql("now()")
                .HasColumnName("criado_em");
            entity.Property(e => e.Ear).HasColumnName("ear");
            entity.Property(e => e.EntregadorId).HasColumnName("entregador_id");
            entity.Property(e => e.MotivoRejeicao).HasColumnName("motivo_rejeicao");
            entity.Property(e => e.Numero)
                .HasMaxLength(20)
                .HasColumnName("numero");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'pendente'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.Tipo)
                .HasMaxLength(30)
                .HasColumnName("tipo");
            entity.Property(e => e.Validade).HasColumnName("validade");
            entity.Property(e => e.VeiculoId).HasColumnName("veiculo_id");

            entity.HasOne(d => d.Entregador).WithMany(p => p.DocumentosEntregador)
                .HasForeignKey(d => d.EntregadorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("documentos_entregador_entregador_id_fkey");

            entity.HasOne(d => d.Veiculo).WithMany(p => p.DocumentosEntregador)
                .HasForeignKey(d => d.VeiculoId)
                .HasConstraintName("documentos_entregador_veiculo_id_fkey");
        });

        modelBuilder.Entity<EmpresaFavorito>(entity =>
        {
            entity.HasKey(e => new { e.EmpresaId, e.EntregadorId }).HasName("empresa_favoritos_pkey");

            entity.ToTable("empresa_favoritos");

            entity.Property(e => e.EmpresaId).HasColumnName("empresa_id");
            entity.Property(e => e.EntregadorId).HasColumnName("entregador_id");
            entity.Property(e => e.CriadoEm)
                .HasDefaultValueSql("now()")
                .HasColumnName("criado_em");

            entity.HasOne(d => d.Empresa).WithMany(p => p.EmpresaFavoritos)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("empresa_favoritos_empresa_id_fkey");

            entity.HasOne(d => d.Entregador).WithMany(p => p.EmpresaFavoritos)
                .HasForeignKey(d => d.EntregadorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("empresa_favoritos_entregador_id_fkey");
        });

        modelBuilder.Entity<Empresa>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("empresas_pkey");

            entity.ToTable("empresas");

            entity.HasIndex(e => e.Cnpj, "empresas_cnpj_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.AtualizadoEm)
                .HasDefaultValueSql("now()")
                .HasColumnName("atualizado_em");
            entity.Property(e => e.Cnpj)
                .HasMaxLength(14)
                .HasColumnName("cnpj");
            entity.Property(e => e.CriadoEm)
                .HasDefaultValueSql("now()")
                .HasColumnName("criado_em");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.EntregaAgendada).HasColumnName("entrega_agendada");
            entity.Property(e => e.EntregaMesmoDia).HasColumnName("entrega_mesmo_dia");
            entity.Property(e => e.FaixaPedidosDia)
                .HasMaxLength(20)
                .HasColumnName("faixa_pedidos_dia");
            entity.Property(e => e.FotoChave).HasColumnName("foto_chave");
            entity.Property(e => e.FusoHorario)
                .HasMaxLength(40)
                .HasDefaultValueSql("'America/Sao_Paulo'::character varying")
                .HasColumnName("fuso_horario");
            entity.Property(e => e.MetaNoPrazoPct)
                .HasPrecision(5, 2)
                .HasDefaultValue(95m)
                .HasColumnName("meta_no_prazo_pct");
            entity.Property(e => e.NomeFantasia)
                .HasMaxLength(100)
                .HasColumnName("nome_fantasia");
            entity.Property(e => e.RazaoSocial)
                .HasMaxLength(150)
                .HasColumnName("razao_social");
            entity.Property(e => e.RetiradaLoja).HasColumnName("retirada_loja");
            entity.Property(e => e.Segmento).HasColumnName("segmento");
            entity.Property(e => e.SomenteFavoritos).HasColumnName("somente_favoritos");
            entity.Property(e => e.Status)
                .HasDefaultValue(StatusEmpresa.Ativa)
                .HasColumnName("status");
            entity.Property(e => e.Telefone)
                .HasMaxLength(20)
                .HasColumnName("telefone");
            entity.Property(e => e.TempoOfertaFavoritosS)
                .HasDefaultValue(120)
                .HasColumnName("tempo_oferta_favoritos_s");
            entity.Property(e => e.UsaCarro).HasColumnName("usa_carro");
            entity.Property(e => e.UsaMoto).HasColumnName("usa_moto");
            entity.Property(e => e.UsaVanCaminhao).HasColumnName("usa_van_caminhao");
            entity.Property(e => e.VendeShopify).HasColumnName("vende_shopify");
            entity.Property(e => e.VendeSiteProprio).HasColumnName("vende_site_proprio");
            entity.Property(e => e.VendeWhatsapp).HasColumnName("vende_whatsapp");
        });

        modelBuilder.Entity<Entregador>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("entregadores_pkey");

            entity.ToTable("entregadores");

            entity.HasIndex(e => e.Cpf, "entregadores_cpf_key").IsUnique();

            entity.HasIndex(e => e.Email, "entregadores_email_key").IsUnique();

            entity.HasIndex(e => e.Telefone, "entregadores_telefone_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.AprovadoEm).HasColumnName("aprovado_em");
            entity.Property(e => e.AtualizadoEm)
                .HasDefaultValueSql("now()")
                .HasColumnName("atualizado_em");
            entity.Property(e => e.Bairro)
                .HasMaxLength(100)
                .HasColumnName("bairro");
            entity.Property(e => e.Cep)
                .HasMaxLength(8)
                .HasColumnName("cep");
            entity.Property(e => e.ChavePix)
                .HasMaxLength(100)
                .HasColumnName("chave_pix");
            entity.Property(e => e.Cidade)
                .HasMaxLength(100)
                .HasColumnName("cidade");
            entity.Property(e => e.Complemento)
                .HasMaxLength(100)
                .HasColumnName("complemento");
            entity.Property(e => e.Cpf)
                .HasMaxLength(11)
                .HasColumnName("cpf");
            entity.Property(e => e.CriadoEm)
                .HasDefaultValueSql("now()")
                .HasColumnName("criado_em");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.EmailConfirmado).HasColumnName("email_confirmado");
            entity.Property(e => e.FotoChave).HasColumnName("foto_chave");
            entity.Property(e => e.Logradouro)
                .HasMaxLength(150)
                .HasColumnName("logradouro");
            entity.Property(e => e.Nome)
                .HasMaxLength(150)
                .HasColumnName("nome");
            entity.Property(e => e.Numero)
                .HasMaxLength(10)
                .HasColumnName("numero");
            entity.Property(e => e.SenhaAlteradaEm).HasColumnName("senha_alterada_em");
            entity.Property(e => e.SenhaHash).HasColumnName("senha_hash");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'em_analise'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.Telefone)
                .HasMaxLength(20)
                .HasColumnName("telefone");
            entity.Property(e => e.TelefoneConfirmado).HasColumnName("telefone_confirmado");
            entity.Property(e => e.TermosAceitosEm).HasColumnName("termos_aceitos_em");
            entity.Property(e => e.TermosVersao)
                .HasMaxLength(20)
                .HasColumnName("termos_versao");
            entity.Property(e => e.Uf)
                .HasMaxLength(2)
                .HasColumnName("uf");
            entity.Property(e => e.VeiculoAtualId).HasColumnName("veiculo_atual_id");

            entity.HasOne(d => d.VeiculoAtual).WithMany(p => p.Entregadores)
                .HasForeignKey(d => d.VeiculoAtualId)
                .HasConstraintName("entregadores_veiculo_atual_id_fkey");
        });

        modelBuilder.Entity<Importacao>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("importacoes_pkey");

            entity.ToTable("importacoes");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.ArquivoChave).HasColumnName("arquivo_chave");
            entity.Property(e => e.ArquivoNome)
                .HasMaxLength(255)
                .HasColumnName("arquivo_nome");
            entity.Property(e => e.ConcluidaEm).HasColumnName("concluida_em");
            entity.Property(e => e.CriadoEm)
                .HasDefaultValueSql("now()")
                .HasColumnName("criado_em");
            entity.Property(e => e.EmpresaId).HasColumnName("empresa_id");
            entity.Property(e => e.Erros)
                .HasColumnType("jsonb")
                .HasColumnName("erros");
            entity.Property(e => e.LinhasComErro).HasColumnName("linhas_com_erro");
            entity.Property(e => e.LinhasImportadas).HasColumnName("linhas_importadas");
            entity.Property(e => e.MapeamentoColunas)
                .HasColumnType("jsonb")
                .HasColumnName("mapeamento_colunas");
            entity.Property(e => e.OrigemId).HasColumnName("origem_id");
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasDefaultValueSql("'enviada'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.TotalLinhas).HasColumnName("total_linhas");
            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");

            entity.HasOne(d => d.Empresa).WithMany(p => p.Importacoes)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("importacoes_empresa_id_fkey");

            entity.HasOne(d => d.Origem).WithMany(p => p.Importacoes)
                .HasForeignKey(d => d.OrigemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("importacoes_origem_id_fkey");

            entity.HasOne(d => d.Usuario).WithMany(p => p.Importacoes)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("importacoes_usuario_id_fkey");
        });

        modelBuilder.Entity<Jornada>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("jornadas_pkey");

            entity.ToTable("jornadas");

            entity.HasIndex(e => e.EntregadorId, "ux_jornadas_aberta")
                .IsUnique()
                .HasFilter("(encerrada_em IS NULL)");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.DistanciaM).HasColumnName("distancia_m");
            entity.Property(e => e.EncerradaEm).HasColumnName("encerrada_em");
            entity.Property(e => e.EntregadorId).HasColumnName("entregador_id");
            entity.Property(e => e.IniciadaEm)
                .HasDefaultValueSql("now()")
                .HasColumnName("iniciada_em");
            entity.Property(e => e.VeiculoId).HasColumnName("veiculo_id");

            entity.HasOne(d => d.Entregador).WithMany(p => p.Jornadas)
                .HasForeignKey(d => d.EntregadorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("jornadas_entregador_id_fkey");

            entity.HasOne(d => d.Veiculo).WithMany(p => p.Jornadas)
                .HasForeignKey(d => d.VeiculoId)
                .HasConstraintName("jornadas_veiculo_id_fkey");
        });

        modelBuilder.Entity<Mensagem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("mensagens_pkey");

            entity.ToTable("mensagens");

            entity.HasIndex(e => new { e.EmpresaId, e.EntregadorId, e.EnviadaEm }, "ix_mensagens_conversa").IsDescending(false, false, true);

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.AutorTipo)
                .HasMaxLength(20)
                .HasColumnName("autor_tipo");
            entity.Property(e => e.AutorUsuarioId).HasColumnName("autor_usuario_id");
            entity.Property(e => e.EmpresaId).HasColumnName("empresa_id");
            entity.Property(e => e.EntregadorId).HasColumnName("entregador_id");
            entity.Property(e => e.EnviadaEm)
                .HasDefaultValueSql("now()")
                .HasColumnName("enviada_em");
            entity.Property(e => e.LidaEm).HasColumnName("lida_em");
            entity.Property(e => e.RotaId).HasColumnName("rota_id");
            entity.Property(e => e.Texto).HasColumnName("texto");

            entity.HasOne(d => d.AutorUsuario).WithMany(p => p.Mensagens)
                .HasForeignKey(d => d.AutorUsuarioId)
                .HasConstraintName("mensagens_autor_usuario_id_fkey");

            entity.HasOne(d => d.Empresa).WithMany(p => p.Mensagens)
                .HasForeignKey(d => d.EmpresaId)
                .HasConstraintName("mensagens_empresa_id_fkey");

            entity.HasOne(d => d.Entregador).WithMany(p => p.Mensagens)
                .HasForeignKey(d => d.EntregadorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("mensagens_entregador_id_fkey");

            entity.HasOne(d => d.Rota).WithMany(p => p.Mensagens)
                .HasForeignKey(d => d.RotaId)
                .HasConstraintName("mensagens_rota_id_fkey");
        });

        modelBuilder.Entity<MensagemProcessada>(entity =>
        {
            entity.HasKey(e => new { e.Consumidor, e.MensagemId }).HasName("mensagens_processadas_pkey");

            entity.ToTable("mensagens_processadas");

            entity.Property(e => e.Consumidor)
                .HasMaxLength(50)
                .HasColumnName("consumidor");
            entity.Property(e => e.MensagemId).HasColumnName("mensagem_id");
            entity.Property(e => e.ProcessadaEm)
                .HasDefaultValueSql("now()")
                .HasColumnName("processada_em");
        });

        modelBuilder.Entity<Notificacao>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("notificacoes_pkey");

            entity.ToTable("notificacoes");

            entity.HasIndex(e => new { e.UsuarioId, e.CriadoEm }, "ix_notificacoes_nao_lidas")
                .IsDescending(false, true)
                .HasFilter("(lida_em IS NULL)");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CriadoEm)
                .HasDefaultValueSql("now()")
                .HasColumnName("criado_em");
            entity.Property(e => e.EmpresaId).HasColumnName("empresa_id");
            entity.Property(e => e.LidaEm).HasColumnName("lida_em");
            entity.Property(e => e.Link)
                .HasMaxLength(255)
                .HasColumnName("link");
            entity.Property(e => e.Mensagem).HasColumnName("mensagem");
            entity.Property(e => e.Tipo)
                .HasMaxLength(30)
                .HasColumnName("tipo");
            entity.Property(e => e.Titulo)
                .HasMaxLength(150)
                .HasColumnName("titulo");
            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");

            entity.HasOne(d => d.Empresa).WithMany(p => p.Notificacoes)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("notificacoes_empresa_id_fkey");

            entity.HasOne(d => d.Usuario).WithMany(p => p.Notificacoes)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("notificacoes_usuario_id_fkey");
        });

        modelBuilder.Entity<Ocorrencia>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("ocorrencias_pkey");

            entity.ToTable("ocorrencias");

            entity.HasIndex(e => e.EmpresaId, "ix_ocorrencias_abertas").HasFilter("((status)::text = 'aberta'::text)");

            entity.HasIndex(e => e.ParadaId, "ix_ocorrencias_parada");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Descricao).HasColumnName("descricao");
            entity.Property(e => e.EmpresaId).HasColumnName("empresa_id");
            entity.Property(e => e.EntregadorId).HasColumnName("entregador_id");
            entity.Property(e => e.FotoChave).HasColumnName("foto_chave");
            entity.Property(e => e.Latitude)
                .HasPrecision(9, 6)
                .HasColumnName("latitude");
            entity.Property(e => e.Longitude)
                .HasPrecision(9, 6)
                .HasColumnName("longitude");
            entity.Property(e => e.ParadaId).HasColumnName("parada_id");
            entity.Property(e => e.RegistradaEm)
                .HasDefaultValueSql("now()")
                .HasColumnName("registrada_em");
            entity.Property(e => e.ResolvidaEm).HasColumnName("resolvida_em");
            entity.Property(e => e.ResolvidaPor).HasColumnName("resolvida_por");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'aberta'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.Tipo)
                .HasMaxLength(30)
                .HasColumnName("tipo");

            entity.HasOne(d => d.Empresa).WithMany(p => p.Ocorrencias)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("ocorrencias_empresa_id_fkey");

            entity.HasOne(d => d.Entregador).WithMany(p => p.Ocorrencias)
                .HasForeignKey(d => d.EntregadorId)
                .HasConstraintName("ocorrencias_entregador_id_fkey");

            entity.HasOne(d => d.Parada).WithMany(p => p.Ocorrencias)
                .HasForeignKey(d => d.ParadaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("ocorrencias_parada_id_fkey");

            entity.HasOne(d => d.ResolvidaPorNavigation).WithMany(p => p.Ocorrencias)
                .HasForeignKey(d => d.ResolvidaPor)
                .HasConstraintName("ocorrencias_resolvida_por_fkey");
        });

        modelBuilder.Entity<OfertaRota>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("ofertas_rota_pkey");

            entity.ToTable("ofertas_rota");

            entity.HasIndex(e => new { e.EntregadorId, e.Status }, "ix_ofertas_entregador");

            entity.HasIndex(e => new { e.RotaId, e.EntregadorId }, "ofertas_rota_rota_id_entregador_id_key").IsUnique();

            entity.HasIndex(e => e.RotaId, "ux_ofertas_uma_aceita")
                .IsUnique()
                .HasFilter("((status)::text = 'aceita'::text)");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.EmpresaId).HasColumnName("empresa_id");
            entity.Property(e => e.EntregadorId).HasColumnName("entregador_id");
            entity.Property(e => e.EnviadaEm)
                .HasDefaultValueSql("now()")
                .HasColumnName("enviada_em");
            entity.Property(e => e.ExpiraEm).HasColumnName("expira_em");
            entity.Property(e => e.RespondidaEm).HasColumnName("respondida_em");
            entity.Property(e => e.Rodada)
                .HasMaxLength(20)
                .HasColumnName("rodada");
            entity.Property(e => e.RotaId).HasColumnName("rota_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'enviada'::character varying")
                .HasColumnName("status");

            entity.HasOne(d => d.Empresa).WithMany(p => p.OfertasRota)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("ofertas_rota_empresa_id_fkey");

            entity.HasOne(d => d.Entregador).WithMany(p => p.OfertasRota)
                .HasForeignKey(d => d.EntregadorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("ofertas_rota_entregador_id_fkey");

            entity.HasOne(d => d.Rota).WithMany(p => p.OfertasRota)
                .HasForeignKey(d => d.RotaId)
                .HasConstraintName("ofertas_rota_rota_id_fkey");
        });

        modelBuilder.Entity<Origem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("origens_pkey");

            entity.ToTable("origens");

            entity.HasIndex(e => e.EmpresaId, "ux_origens_padrao")
                .IsUnique()
                .HasFilter("padrao");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Abre).HasColumnName("abre");
            entity.Property(e => e.AbreDomingo).HasColumnName("abre_domingo");
            entity.Property(e => e.AbreSabado)
                .HasDefaultValue(true)
                .HasColumnName("abre_sabado");
            entity.Property(e => e.Ativa)
                .HasDefaultValue(true)
                .HasColumnName("ativa");
            entity.Property(e => e.AtualizadoEm)
                .HasDefaultValueSql("now()")
                .HasColumnName("atualizado_em");
            entity.Property(e => e.Bairro)
                .HasMaxLength(100)
                .HasColumnName("bairro");
            entity.Property(e => e.Cep)
                .HasMaxLength(8)
                .HasColumnName("cep");
            entity.Property(e => e.Cidade)
                .HasMaxLength(100)
                .HasColumnName("cidade");
            entity.Property(e => e.Complemento)
                .HasMaxLength(100)
                .HasColumnName("complemento");
            entity.Property(e => e.CriadoEm)
                .HasDefaultValueSql("now()")
                .HasColumnName("criado_em");
            entity.Property(e => e.EmpresaId).HasColumnName("empresa_id");
            entity.Property(e => e.Fecha).HasColumnName("fecha");
            entity.Property(e => e.Latitude)
                .HasPrecision(9, 6)
                .HasColumnName("latitude");
            entity.Property(e => e.Logradouro)
                .HasMaxLength(150)
                .HasColumnName("logradouro");
            entity.Property(e => e.Longitude)
                .HasPrecision(9, 6)
                .HasColumnName("longitude");
            entity.Property(e => e.Nome)
                .HasMaxLength(100)
                .HasColumnName("nome");
            entity.Property(e => e.Numero)
                .HasMaxLength(10)
                .HasColumnName("numero");
            entity.Property(e => e.Padrao).HasColumnName("padrao");
            entity.Property(e => e.ResponsavelId).HasColumnName("responsavel_id");
            entity.Property(e => e.Uf)
                .HasMaxLength(2)
                .HasColumnName("uf");

            entity.HasOne(d => d.Empresa).WithMany(p => p.Origens)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("origens_empresa_id_fkey");

            entity.HasOne(d => d.Responsavel).WithMany(p => p.Origens)
                .HasForeignKey(d => d.ResponsavelId)
                .HasConstraintName("origens_responsavel_id_fkey");
        });

        modelBuilder.Entity<Outbox>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("outbox_pkey");

            entity.ToTable("outbox");

            entity.HasIndex(e => e.CriadoEm, "ix_outbox_pendentes").HasFilter("(publicado_em IS NULL)");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Chave)
                .HasMaxLength(50)
                .HasColumnName("chave");
            entity.Property(e => e.CriadoEm)
                .HasDefaultValueSql("now()")
                .HasColumnName("criado_em");
            entity.Property(e => e.Payload)
                .HasColumnType("jsonb")
                .HasColumnName("payload");
            entity.Property(e => e.PublicadoEm).HasColumnName("publicado_em");
            entity.Property(e => e.TipoEvento)
                .HasMaxLength(50)
                .HasColumnName("tipo_evento");
            entity.Property(e => e.Topico)
                .HasMaxLength(50)
                .HasColumnName("topico");
        });

        modelBuilder.Entity<Parada>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("paradas_pkey");

            entity.ToTable("paradas");

            entity.HasIndex(e => new { e.RotaId, e.Sequencia }, "ix_paradas_rota");

            entity.HasIndex(e => e.PedidoId, "ux_paradas_pedido_ativo")
                .IsUnique()
                .HasFilter("((status)::text = ANY ((ARRAY['pendente'::character varying, 'a_caminho'::character varying])::text[]))");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.AtualizadoEm)
                .HasDefaultValueSql("now()")
                .HasColumnName("atualizado_em");
            entity.Property(e => e.ChegadaPrevista).HasColumnName("chegada_prevista");
            entity.Property(e => e.ChegadaReal).HasColumnName("chegada_real");
            entity.Property(e => e.ComprovanteChave).HasColumnName("comprovante_chave");
            entity.Property(e => e.ComprovanteTipo)
                .HasMaxLength(20)
                .HasColumnName("comprovante_tipo");
            entity.Property(e => e.ConcluidaEm).HasColumnName("concluida_em");
            entity.Property(e => e.CriadoEm)
                .HasDefaultValueSql("now()")
                .HasColumnName("criado_em");
            entity.Property(e => e.EmpresaId).HasColumnName("empresa_id");
            entity.Property(e => e.LatitudeConclusao)
                .HasPrecision(9, 6)
                .HasColumnName("latitude_conclusao");
            entity.Property(e => e.LongitudeConclusao)
                .HasPrecision(9, 6)
                .HasColumnName("longitude_conclusao");
            entity.Property(e => e.PedidoId).HasColumnName("pedido_id");
            entity.Property(e => e.RecebedorNome)
                .HasMaxLength(150)
                .HasColumnName("recebedor_nome");
            entity.Property(e => e.RotaId).HasColumnName("rota_id");
            entity.Property(e => e.Sequencia).HasColumnName("sequencia");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'pendente'::character varying")
                .HasColumnName("status");

            entity.HasOne(d => d.Empresa).WithMany(p => p.Paradas)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("paradas_empresa_id_fkey");

            entity.HasOne(d => d.Pedido).WithMany(p => p.Paradas)
                .HasForeignKey(d => d.PedidoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("paradas_pedido_id_fkey");

            entity.HasOne(d => d.Rota).WithMany(p => p.Paradas)
                .HasForeignKey(d => d.RotaId)
                .HasConstraintName("paradas_rota_id_fkey");
        });

        modelBuilder.Entity<PedidoEvento>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pedido_eventos_pkey");

            entity.ToTable("pedido_eventos");

            entity.HasIndex(e => new { e.PedidoId, e.OcorridoEm }, "ix_pedido_eventos_pedido");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.AutorId).HasColumnName("autor_id");
            entity.Property(e => e.AutorTipo)
                .HasMaxLength(20)
                .HasColumnName("autor_tipo");
            entity.Property(e => e.Dados)
                .HasColumnType("jsonb")
                .HasColumnName("dados");
            entity.Property(e => e.Descricao)
                .HasMaxLength(255)
                .HasColumnName("descricao");
            entity.Property(e => e.EmpresaId).HasColumnName("empresa_id");
            entity.Property(e => e.OcorridoEm)
                .HasDefaultValueSql("now()")
                .HasColumnName("ocorrido_em");
            entity.Property(e => e.PedidoId).HasColumnName("pedido_id");
            entity.Property(e => e.RotaId).HasColumnName("rota_id");
            entity.Property(e => e.StatusNovo)
                .HasMaxLength(20)
                .HasColumnName("status_novo");
            entity.Property(e => e.TipoEvento)
                .HasMaxLength(50)
                .HasColumnName("tipo_evento");

            entity.HasOne(d => d.Empresa).WithMany(p => p.PedidoEventos)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("pedido_eventos_empresa_id_fkey");

            entity.HasOne(d => d.Pedido).WithMany(p => p.PedidoEventos)
                .HasForeignKey(d => d.PedidoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("pedido_eventos_pedido_id_fkey");

            entity.HasOne(d => d.Rota).WithMany(p => p.PedidoEventos)
                .HasForeignKey(d => d.RotaId)
                .HasConstraintName("pedido_eventos_rota_id_fkey");
        });

        modelBuilder.Entity<Pedido>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pedidos_pkey");

            entity.ToTable("pedidos");

            entity.HasIndex(e => new { e.EmpresaId, e.Status, e.CriadoEm }, "ix_pedidos_status").IsDescending(false, false, true);

            entity.HasIndex(e => e.TokenRastreamento, "pedidos_token_rastreamento_key").IsUnique();

            entity.HasIndex(e => new { e.EmpresaId, e.NumeroPedido }, "uq_pedidos_numero").IsUnique();

            entity.HasIndex(e => new { e.EmpresaId, e.Canal, e.IdExterno }, "ux_pedidos_externo")
                .IsUnique()
                .HasFilter("(id_externo IS NOT NULL)");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.AtualizadoEm)
                .HasDefaultValueSql("now()")
                .HasColumnName("atualizado_em");
            entity.Property(e => e.Bairro)
                .HasMaxLength(100)
                .HasColumnName("bairro");
            entity.Property(e => e.Canal)
                .HasMaxLength(20)
                .HasColumnName("canal");
            entity.Property(e => e.Cep)
                .HasMaxLength(8)
                .HasColumnName("cep");
            entity.Property(e => e.Cidade)
                .HasMaxLength(100)
                .HasColumnName("cidade");
            entity.Property(e => e.ClienteEmail)
                .HasMaxLength(255)
                .HasColumnName("cliente_email");
            entity.Property(e => e.ClienteNome)
                .HasMaxLength(150)
                .HasColumnName("cliente_nome");
            entity.Property(e => e.ClienteTelefone)
                .HasMaxLength(20)
                .HasColumnName("cliente_telefone");
            entity.Property(e => e.Complemento)
                .HasMaxLength(100)
                .HasColumnName("complemento");
            entity.Property(e => e.CriadoEm)
                .HasDefaultValueSql("now()")
                .HasColumnName("criado_em");
            entity.Property(e => e.DadosOriginais)
                .HasColumnType("jsonb")
                .HasColumnName("dados_originais");
            entity.Property(e => e.EmpresaId).HasColumnName("empresa_id");
            entity.Property(e => e.EntregueEm).HasColumnName("entregue_em");
            entity.Property(e => e.GeocodificacaoConfianca)
                .HasPrecision(3, 2)
                .HasColumnName("geocodificacao_confianca");
            entity.Property(e => e.GeocodificacaoStatus)
                .HasMaxLength(20)
                .HasDefaultValueSql("'pendente'::character varying")
                .HasColumnName("geocodificacao_status");
            entity.Property(e => e.IdExterno)
                .HasMaxLength(100)
                .HasColumnName("id_externo");
            entity.Property(e => e.ImportacaoId).HasColumnName("importacao_id");
            entity.Property(e => e.JanelaFim).HasColumnName("janela_fim");
            entity.Property(e => e.JanelaInicio).HasColumnName("janela_inicio");
            entity.Property(e => e.Latitude)
                .HasPrecision(9, 6)
                .HasColumnName("latitude");
            entity.Property(e => e.Logradouro)
                .HasMaxLength(150)
                .HasColumnName("logradouro");
            entity.Property(e => e.Longitude)
                .HasPrecision(9, 6)
                .HasColumnName("longitude");
            entity.Property(e => e.Modalidade)
                .HasMaxLength(20)
                .HasDefaultValueSql("'mesmo_dia'::character varying")
                .HasColumnName("modalidade");
            entity.Property(e => e.Numero)
                .HasMaxLength(10)
                .HasColumnName("numero");
            entity.Property(e => e.NumeroExterno)
                .HasMaxLength(50)
                .HasColumnName("numero_externo");
            entity.Property(e => e.NumeroPedido)
                .ValueGeneratedOnAdd() // preenchido pelo gatilho trg_pedidos_numero
                .HasColumnName("numero_pedido");
            entity.Property(e => e.Observacoes).HasColumnName("observacoes");
            entity.Property(e => e.OrigemId).HasColumnName("origem_id");
            entity.Property(e => e.PesoKg)
                .HasPrecision(8, 2)
                .HasColumnName("peso_kg");
            entity.Property(e => e.Referencia)
                .HasMaxLength(255)
                .HasColumnName("referencia");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'novo'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.TokenExpiraEm).HasColumnName("token_expira_em");
            entity.Property(e => e.TokenRastreamento)
                .HasMaxLength(64)
                .HasColumnName("token_rastreamento");
            entity.Property(e => e.Uf)
                .HasMaxLength(2)
                .HasColumnName("uf");
            entity.Property(e => e.Volumes)
                .HasDefaultValue(1)
                .HasColumnName("volumes");

            entity.HasOne(d => d.Empresa).WithMany(p => p.Pedidos)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("pedidos_empresa_id_fkey");

            entity.HasOne(d => d.Importacao).WithMany(p => p.Pedidos)
                .HasForeignKey(d => d.ImportacaoId)
                .HasConstraintName("pedidos_importacao_id_fkey");

            entity.HasOne(d => d.Origem).WithMany(p => p.Pedidos)
                .HasForeignKey(d => d.OrigemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("pedidos_origem_id_fkey");
        });

        modelBuilder.Entity<Plano>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("planos_pkey");

            entity.ToTable("planos");

            entity.HasIndex(e => e.Nome, "planos_nome_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Ativo)
                .HasDefaultValue(true)
                .HasColumnName("ativo");
            entity.Property(e => e.CriadoEm)
                .HasDefaultValueSql("now()")
                .HasColumnName("criado_em");
            entity.Property(e => e.LimitePedidosMes).HasColumnName("limite_pedidos_mes");
            entity.Property(e => e.Nome)
                .HasMaxLength(50)
                .HasColumnName("nome");
            entity.Property(e => e.PrecoMensal)
                .HasPrecision(10, 2)
                .HasColumnName("preco_mensal");
        });

        modelBuilder.Entity<PosicaoEntregador>(entity =>
        {
            entity.HasKey(e => new { e.EntregadorId, e.RegistradaEm }).HasName("posicoes_entregador_pkey");

            entity.ToTable("posicoes_entregador");

            entity.HasIndex(e => e.RegistradaEm, "ix_posicoes_data");

            entity.HasIndex(e => new { e.RotaId, e.RegistradaEm }, "ix_posicoes_rota").HasFilter("(rota_id IS NOT NULL)");

            entity.Property(e => e.EntregadorId).HasColumnName("entregador_id");
            entity.Property(e => e.RegistradaEm).HasColumnName("registrada_em");
            entity.Property(e => e.EmpresaId).HasColumnName("empresa_id");
            entity.Property(e => e.Latitude)
                .HasPrecision(9, 6)
                .HasColumnName("latitude");
            entity.Property(e => e.Longitude)
                .HasPrecision(9, 6)
                .HasColumnName("longitude");
            entity.Property(e => e.PrecisaoM)
                .HasPrecision(6, 1)
                .HasColumnName("precisao_m");
            entity.Property(e => e.RecebidaEm)
                .HasDefaultValueSql("now()")
                .HasColumnName("recebida_em");
            entity.Property(e => e.RotaId).HasColumnName("rota_id");
            entity.Property(e => e.VelocidadeKmh)
                .HasPrecision(5, 1)
                .HasColumnName("velocidade_kmh");
        });

        modelBuilder.Entity<Rota>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("rotas_pkey");

            entity.ToTable("rotas");

            entity.HasIndex(e => new { e.EmpresaId, e.DataOperacao, e.Status }, "ix_rotas_dia");

            entity.HasIndex(e => new { e.EntregadorId, e.Status }, "ix_rotas_entregador");

            entity.HasIndex(e => new { e.EmpresaId, e.DataOperacao, e.Numero }, "uq_rotas_numero").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Atrasada).HasColumnName("atrasada");
            entity.Property(e => e.AtualizadoEm)
                .HasDefaultValueSql("now()")
                .HasColumnName("atualizado_em");
            entity.Property(e => e.ColetaPrevistaEm).HasColumnName("coleta_prevista_em");
            entity.Property(e => e.ConcluidaEm).HasColumnName("concluida_em");
            entity.Property(e => e.Cor)
                .HasMaxLength(7)
                .HasColumnName("cor");
            entity.Property(e => e.CriadoEm)
                .HasDefaultValueSql("now()")
                .HasColumnName("criado_em");
            entity.Property(e => e.CustoEstimado)
                .HasPrecision(10, 2)
                .HasColumnName("custo_estimado");
            entity.Property(e => e.DataOperacao).HasColumnName("data_operacao");
            entity.Property(e => e.DistanciaPrevistaM).HasColumnName("distancia_prevista_m");
            entity.Property(e => e.DistanciaRealM).HasColumnName("distancia_real_m");
            entity.Property(e => e.DuracaoPrevistaS).HasColumnName("duracao_prevista_s");
            entity.Property(e => e.EmpresaId).HasColumnName("empresa_id");
            entity.Property(e => e.EntregadorId).HasColumnName("entregador_id");
            entity.Property(e => e.EntregadorSugeridoId).HasColumnName("entregador_sugerido_id");
            entity.Property(e => e.FimPrevistoEm).HasColumnName("fim_previsto_em");
            entity.Property(e => e.IniciadaEm).HasColumnName("iniciada_em");
            entity.Property(e => e.Numero).HasColumnName("numero");
            entity.Property(e => e.Regiao)
                .HasMaxLength(60)
                .HasColumnName("regiao");
            entity.Property(e => e.RoteirizacaoId).HasColumnName("roteirizacao_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'proposta'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.TipoVeiculo)
                .HasMaxLength(20)
                .HasColumnName("tipo_veiculo");
            entity.Property(e => e.Trajeto).HasColumnName("trajeto");
            entity.Property(e => e.ValorEntregador)
                .HasPrecision(10, 2)
                .HasColumnName("valor_entregador");
            entity.Property(e => e.VeiculoId).HasColumnName("veiculo_id");

            entity.HasOne(d => d.Empresa).WithMany(p => p.Rotas)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("rotas_empresa_id_fkey");

            entity.HasOne(d => d.Entregador).WithMany(p => p.RotasEntregador)
                .HasForeignKey(d => d.EntregadorId)
                .HasConstraintName("rotas_entregador_id_fkey");

            entity.HasOne(d => d.EntregadorSugerido).WithMany(p => p.RotasEntregadorSugerido)
                .HasForeignKey(d => d.EntregadorSugeridoId)
                .HasConstraintName("rotas_entregador_sugerido_id_fkey");

            entity.HasOne(d => d.Roteirizacao).WithMany(p => p.Rotas)
                .HasForeignKey(d => d.RoteirizacaoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("rotas_roteirizacao_id_fkey");

            entity.HasOne(d => d.Veiculo).WithMany(p => p.Rotas)
                .HasForeignKey(d => d.VeiculoId)
                .HasConstraintName("rotas_veiculo_id_fkey");
        });

        modelBuilder.Entity<Roteirizacao>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("roteirizacoes_pkey");

            entity.ToTable("roteirizacoes");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.ConcluidaEm).HasColumnName("concluida_em");
            entity.Property(e => e.CriadoEm)
                .HasDefaultValueSql("now()")
                .HasColumnName("criado_em");
            entity.Property(e => e.EmpresaId).HasColumnName("empresa_id");
            entity.Property(e => e.Erro).HasColumnName("erro");
            entity.Property(e => e.IniciadaEm).HasColumnName("iniciada_em");
            entity.Property(e => e.Objetivo)
                .HasMaxLength(20)
                .HasDefaultValueSql("'custo'::character varying")
                .HasColumnName("objetivo");
            entity.Property(e => e.OrigemId).HasColumnName("origem_id");
            entity.Property(e => e.Parametros)
                .HasDefaultValueSql("'{}'::jsonb")
                .HasColumnType("jsonb")
                .HasColumnName("parametros");
            entity.Property(e => e.Resultado)
                .HasColumnType("jsonb")
                .HasColumnName("resultado");
            entity.Property(e => e.SolicitadaPor).HasColumnName("solicitada_por");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'pendente'::character varying")
                .HasColumnName("status");

            entity.HasOne(d => d.Empresa).WithMany(p => p.Roteirizacoes)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("roteirizacoes_empresa_id_fkey");

            entity.HasOne(d => d.Origem).WithMany(p => p.Roteirizacoes)
                .HasForeignKey(d => d.OrigemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("roteirizacoes_origem_id_fkey");

            entity.HasOne(d => d.SolicitadaPorNavigation).WithMany(p => p.Roteirizacoes)
                .HasForeignKey(d => d.SolicitadaPor)
                .HasConstraintName("roteirizacoes_solicitada_por_fkey");
        });

        modelBuilder.Entity<Saque>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("saques_pkey");

            entity.ToTable("saques");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.ChavePix)
                .HasMaxLength(100)
                .HasColumnName("chave_pix");
            entity.Property(e => e.EntregadorId).HasColumnName("entregador_id");
            entity.Property(e => e.Erro).HasColumnName("erro");
            entity.Property(e => e.GatewayTransferenciaId)
                .HasMaxLength(100)
                .HasColumnName("gateway_transferencia_id");
            entity.Property(e => e.PagoEm).HasColumnName("pago_em");
            entity.Property(e => e.SolicitadoEm)
                .HasDefaultValueSql("now()")
                .HasColumnName("solicitado_em");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'solicitado'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.Valor)
                .HasPrecision(12, 2)
                .HasColumnName("valor");

            entity.HasOne(d => d.Entregador).WithMany(p => p.Saques)
                .HasForeignKey(d => d.EntregadorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("saques_entregador_id_fkey");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("usuarios_pkey");

            entity.ToTable("usuarios");

            entity.HasIndex(e => e.EmpresaId, "ix_usuarios_empresa");

            entity.HasIndex(e => e.Email, "usuarios_email_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Ativo)
                .HasDefaultValue(true)
                .HasColumnName("ativo");
            entity.Property(e => e.AtualizadoEm)
                .HasDefaultValueSql("now()")
                .HasColumnName("atualizado_em");
            entity.Property(e => e.CriadoEm)
                .HasDefaultValueSql("now()")
                .HasColumnName("criado_em");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.EmailConfirmado).HasColumnName("email_confirmado");
            entity.Property(e => e.EmpresaId).HasColumnName("empresa_id");
            entity.Property(e => e.FotoChave).HasColumnName("foto_chave");
            entity.Property(e => e.Nome)
                .HasMaxLength(150)
                .HasColumnName("nome");
            entity.Property(e => e.NotifCobranca)
                .HasDefaultValue(true)
                .HasColumnName("notif_cobranca");
            entity.Property(e => e.NotifOcorrencias)
                .HasDefaultValue(true)
                .HasColumnName("notif_ocorrencias");
            entity.Property(e => e.Papel)
                .HasMaxLength(35)
                .HasDefaultValueSql("'funcionario'::character varying")
                .HasColumnName("papel");
            entity.Property(e => e.SenhaHash).HasColumnName("senha_hash");
            entity.Property(e => e.Telefone)
                .HasMaxLength(20)
                .HasColumnName("telefone");
            entity.Property(e => e.TermosAceitosEm).HasColumnName("termos_aceitos_em");
            entity.Property(e => e.TermosVersao)
                .HasMaxLength(20)
                .HasColumnName("termos_versao");
            entity.Property(e => e.TrocarSenha).HasColumnName("trocar_senha");
            entity.Property(e => e.UltimoAcessoEm).HasColumnName("ultimo_acesso_em");

            entity.HasOne(d => d.Empresa).WithMany(p => p.Usuarios)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("usuarios_empresa_id_fkey");
        });

        modelBuilder.Entity<UsuarioLoginExterno>(entity =>
        {
            entity.HasKey(e => new { e.Provedor, e.ChaveProvedor }).HasName("usuarios_logins_externos_pkey");

            entity.ToTable("usuarios_logins_externos");

            entity.HasIndex(e => e.UsuarioId, "ix_logins_externos_usuario");

            entity.Property(e => e.Provedor)
                .HasMaxLength(20)
                .HasColumnName("provedor");
            entity.Property(e => e.ChaveProvedor)
                .HasMaxLength(255)
                .HasColumnName("chave_provedor");
            entity.Property(e => e.CriadoEm)
                .HasDefaultValueSql("now()")
                .HasColumnName("criado_em");
            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");

            entity.HasOne(d => d.Usuario).WithMany(p => p.UsuariosLoginsExternos)
                .HasForeignKey(d => d.UsuarioId)
                .HasConstraintName("usuarios_logins_externos_usuario_id_fkey");
        });

        modelBuilder.Entity<Veiculo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("veiculos_pkey");

            entity.ToTable("veiculos");

            entity.HasIndex(e => e.Placa, "veiculos_placa_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Ativo)
                .HasDefaultValue(true)
                .HasColumnName("ativo");
            entity.Property(e => e.AtualizadoEm)
                .HasDefaultValueSql("now()")
                .HasColumnName("atualizado_em");
            entity.Property(e => e.CapacidadeKg)
                .HasPrecision(8, 2)
                .HasColumnName("capacidade_kg");
            entity.Property(e => e.CapacidadeLitros).HasColumnName("capacidade_litros");
            entity.Property(e => e.CapacidadeVolumes).HasColumnName("capacidade_volumes");
            entity.Property(e => e.CriadoEm)
                .HasDefaultValueSql("now()")
                .HasColumnName("criado_em");
            entity.Property(e => e.EntregadorId).HasColumnName("entregador_id");
            entity.Property(e => e.Modelo)
                .HasMaxLength(60)
                .HasColumnName("modelo");
            entity.Property(e => e.Placa)
                .HasMaxLength(8)
                .HasColumnName("placa");
            entity.Property(e => e.Tipo)
                .HasMaxLength(20)
                .HasColumnName("tipo");
            entity.Property(e => e.Verificado).HasColumnName("verificado");

            entity.HasOne(d => d.Entregador).WithMany(p => p.Veiculos)
                .HasForeignKey(d => d.EntregadorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("veiculos_entregador_id_fkey");
        });

        modelBuilder.Entity<VwEntregadorRede>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_entregadores_rede");

            entity.Property(e => e.CnhVerificada).HasColumnName("cnh_verificada");
            entity.Property(e => e.FotoChave).HasColumnName("foto_chave");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Nome)
                .HasMaxLength(150)
                .HasColumnName("nome");
            entity.Property(e => e.NotaMedia).HasColumnName("nota_media");
            entity.Property(e => e.TempoAceiteMedioS).HasColumnName("tempo_aceite_medio_s");
            entity.Property(e => e.TotalAvaliacoes).HasColumnName("total_avaliacoes");
            entity.Property(e => e.TotalEntregas).HasColumnName("total_entregas");
            entity.Property(e => e.TotalFalhas).HasColumnName("total_falhas");
            entity.Property(e => e.VeiculoPlaca)
                .HasMaxLength(8)
                .HasColumnName("veiculo_placa");
            entity.Property(e => e.VeiculoTipo)
                .HasMaxLength(20)
                .HasColumnName("veiculo_tipo");
            entity.Property(e => e.VeiculoVerificado).HasColumnName("veiculo_verificado");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
