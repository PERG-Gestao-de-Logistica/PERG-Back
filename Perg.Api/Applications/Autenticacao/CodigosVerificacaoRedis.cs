using System.Security.Cryptography;
using System.Text;
using Perg.Api.Interfaces;
using StackExchange.Redis;

namespace Perg.Api.Applications.Autenticacao
{
    public class CodigosVerificacaoRedis : ICodigosVerificacao
    {
        private static readonly TimeSpan Validade = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan IntervaloReenvio = TimeSpan.FromMinutes(1);
        private const int MaximoTentativas = 5;

        private readonly IDatabase _redis;

        public CodigosVerificacaoRedis(IConnectionMultiplexer conexao)
        {
            _redis = conexao.GetDatabase();
        }

        public string? Gerar(string chave)
        {
            // SET NX: só um pedido por minuto passa, mesmo com dois servidores ao mesmo tempo
            if (!_redis.StringSet("perg:reenvio:" + chave, "1", IntervaloReenvio, When.NotExists))
            {
                return null;
            }

            string codigo = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
            string chaveCodigo = "perg:codigo:" + chave;

            _redis.KeyDelete(chaveCodigo);
            _redis.HashSet(chaveCodigo, [new HashEntry("codigo", codigo), new HashEntry("tentativas", 0)]);
            _redis.KeyExpire(chaveCodigo, Validade);
            return codigo;
        }

        public ResultadoCodigo Validar(string chave, string codigo)
        {
            string chaveCodigo = "perg:codigo:" + chave;
            RedisValue guardado = _redis.HashGet(chaveCodigo, "codigo");

            if (guardado.IsNullOrEmpty)
            {
                return ResultadoCodigo.Expirado;
            }

            if ((int)_redis.HashGet(chaveCodigo, "tentativas") >= MaximoTentativas)
            {
                return ResultadoCodigo.TentativasEsgotadas;
            }

            bool confere = CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(guardado.ToString()),
                Encoding.UTF8.GetBytes((codigo ?? string.Empty).Trim()));

            if (!confere)
            {
                // HINCRBY é atômico: tentativas em paralelo não escapam do limite
                long tentativas = _redis.HashIncrement(chaveCodigo, "tentativas");
                return tentativas >= MaximoTentativas ? ResultadoCodigo.TentativasEsgotadas : ResultadoCodigo.Invalido;
            }

            _redis.KeyDelete(chaveCodigo);
            return ResultadoCodigo.Valido;
        }
    }
}
