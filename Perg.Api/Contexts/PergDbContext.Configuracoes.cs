using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Perg.Api.Contexts.Conversores;
using Perg.Api.Domains;

namespace Perg.Api.Contexts;

// Configurações nossas, separadas do arquivo gerado pelo scaffold.
public partial class PergDbContext
{
    // Empresa do usuário logado (vem do token). Null = sem filtro (cadastro, login).
    public Guid? EmpresaIdAtual { get; set; }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        // Entidades escritas à mão depois do scaffold (antes do laço, para receberem as mesmas regras)
        ConfigurarAdministradores(modelBuilder);
        ConfigurarIntegracoes(modelBuilder);

        foreach (IMutableEntityType entidade in modelBuilder.Model.GetEntityTypes().ToList())
        {
            foreach (IMutableProperty propriedade in entidade.GetProperties())
            {
                // Enums "normais" (int) são gravados como texto snake_case ('em_rota', 'admin').
                // Enums ": short" (segmento e status da empresa) continuam como smallint.
                Type tipo = Nullable.GetUnderlyingType(propriedade.ClrType) ?? propriedade.ClrType;
                if (tipo.IsEnum && Enum.GetUnderlyingType(tipo) == typeof(int))
                {
                    Type tipoConversor = typeof(ConversorEnumSnakeCase<>).MakeGenericType(tipo);
                    propriedade.SetValueConverter((ValueConverter)Activator.CreateInstance(tipoConversor)!);
                }
            }

            // Booleanos com DEFAULT no banco: o EF não enviaria "false" num INSERT
            // (false é o valor padrão do C#), e o banco gravaria o DEFAULT true.
            // Removendo o default do modelo, o EF sempre envia o valor que está na entidade.
            foreach (IMutableProperty propriedade in entidade.GetProperties())
            {
                if (propriedade.ClrType == typeof(bool) && propriedade.GetDefaultValue() != null)
                {
                    propriedade.SetDefaultValue(null);
                    propriedade.ValueGenerated = ValueGenerated.Never;
                }
            }

            // Isolamento entre lojas: toda tabela com EmpresaId só mostra dados da empresa logada
            IMutableProperty? empresaId = entidade.FindProperty("EmpresaId");
            if (empresaId != null && entidade.ClrType != typeof(VwEntregadorRede))
            {
                modelBuilder.Entity(entidade.ClrType).HasQueryFilter(CriarFiltroEmpresa(entidade.ClrType));
            }
        }
    }

    // Monta: e => EmpresaIdAtual == null || e.EmpresaId == EmpresaIdAtual
    private LambdaExpression CriarFiltroEmpresa(Type tipoEntidade)
    {
        ParameterExpression e = Expression.Parameter(tipoEntidade, "e");
        MemberExpression empresaAtual = Expression.Property(Expression.Constant(this), nameof(EmpresaIdAtual));
        Expression empresaDaLinha = Expression.Convert(Expression.Property(e, "EmpresaId"), typeof(Guid?));

        Expression corpo = Expression.OrElse(
            Expression.Equal(empresaAtual, Expression.Constant(null, typeof(Guid?))),
            Expression.Equal(empresaDaLinha, empresaAtual));

        return Expression.Lambda(corpo, e);
    }

    // Preenche criado_em e atualizado_em automaticamente
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        PreencherDatas();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        PreencherDatas();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void PreencherDatas()
    {
        DateTime agora = DateTime.UtcNow;

        foreach (var entrada in ChangeTracker.Entries())
        {
            if (entrada.State == EntityState.Added && entrada.Metadata.FindProperty("CriadoEm") != null)
            {
                entrada.Property("CriadoEm").CurrentValue = agora;
            }

            if ((entrada.State == EntityState.Added || entrada.State == EntityState.Modified)
                && entrada.Metadata.FindProperty("AtualizadoEm") != null)
            {
                entrada.Property("AtualizadoEm").CurrentValue = agora;
            }
        }
    }
}
