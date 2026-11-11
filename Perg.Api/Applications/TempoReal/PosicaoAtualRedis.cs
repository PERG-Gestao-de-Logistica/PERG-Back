using System.Text.Json;
using Perg.Api.Interfaces;
using StackExchange.Redis;

namespace Perg.Api.Applications.TempoReal
{
    // Última posição no Redis: "perg:posicao:{entregador}" (JSON, expira em 10 min)
    // e o conjunto "perg:posicoes:empresa:{empresa}" com os entregadores daquela loja.
    public class PosicaoAtualRedis : IPosicaoAtualCache
    {
        private readonly IDatabase _redis;

        public PosicaoAtualRedis(IConnectionMultiplexer conexao)
        {
            _redis = conexao.GetDatabase();
        }

        public void Atualizar(PosicaoAtual posicao)
        {
            // Posição mais velha que a guardada (chegou fora de ordem) é ignorada
            PosicaoAtual? atual = Ler(posicao.EntregadorId);
            if (atual != null && atual.RegistradaEm > posicao.RegistradaEm)
            {
                return;
            }

            _redis.StringSet(ChavePosicao(posicao.EntregadorId), JsonSerializer.Serialize(posicao), PosicaoAtualCache.Validade);

            if (posicao.EmpresaId.HasValue)
            {
                string chaveEmpresa = ChaveEmpresa(posicao.EmpresaId.Value);
                _redis.SetAdd(chaveEmpresa, posicao.EntregadorId.ToString());
                _redis.KeyExpire(chaveEmpresa, PosicaoAtualCache.Validade);
            }
        }

        public PosicaoAtual? Obter(Guid entregadorId)
        {
            PosicaoAtual? posicao = Ler(entregadorId);
            return posicao != null && DateTime.UtcNow - posicao.RegistradaEm <= PosicaoAtualCache.Validade ? posicao : null;
        }

        public List<PosicaoAtual> ListarDaEmpresa(Guid empresaId)
        {
            string chaveEmpresa = ChaveEmpresa(empresaId);
            RedisValue[] ids = _redis.SetMembers(chaveEmpresa);
            if (ids.Length == 0) return new List<PosicaoAtual>();

            RedisValue[] valores = _redis.StringGet(ids.Select(id => (RedisKey)("perg:posicao:" + id)).ToArray());
            DateTime limite = DateTime.UtcNow - PosicaoAtualCache.Validade;
            var lista = new List<PosicaoAtual>();

            for (int i = 0; i < ids.Length; i++)
            {
                PosicaoAtual? posicao = valores[i].IsNullOrEmpty ? null : JsonSerializer.Deserialize<PosicaoAtual>(valores[i].ToString());

                // Entregador que mudou de loja ou cuja posição expirou sai do conjunto
                if (posicao == null || posicao.EmpresaId != empresaId || posicao.RegistradaEm < limite)
                {
                    _redis.SetRemove(chaveEmpresa, ids[i]);
                    continue;
                }

                lista.Add(posicao);
            }

            return lista;
        }

        private PosicaoAtual? Ler(Guid entregadorId)
        {
            RedisValue valor = _redis.StringGet(ChavePosicao(entregadorId));
            return valor.IsNullOrEmpty ? null : JsonSerializer.Deserialize<PosicaoAtual>(valor.ToString());
        }

        private static string ChavePosicao(Guid entregadorId) => $"perg:posicao:{entregadorId}";
        private static string ChaveEmpresa(Guid empresaId) => $"perg:posicoes:empresa:{empresaId}";
    }
}
