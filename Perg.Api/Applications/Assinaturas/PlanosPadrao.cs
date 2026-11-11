using Perg.Api.Domains;

namespace Perg.Api.Applications.Assinaturas
{
    // Planos criados na primeira vez que a API sobe com a tabela vazia.
    // Preços e limites são um ponto de partida: ajuste direto na tabela planos.
    public static class PlanosPadrao
    {
        public static List<Plano> Criar()
        {
            return
            [
                Novo("Grátis", 0m, 50),
                Novo("Essencial", 149.90m, 600),
                Novo("Profissional", 349.90m, 3000),
                Novo("Empresarial", 799.90m, null)
            ];
        }

        private static Plano Novo(string nome, decimal preco, int? limite) => new()
        {
            Id = Guid.CreateVersion7(),
            Nome = nome,
            PrecoMensal = preco,
            LimitePedidosMes = limite,
            Ativo = true
        };
    }
}
