using Perg.Api.Interfaces;
using StackExchange.Redis;

namespace Perg.Api.Applications.Cache
{
    public class CacheCurtoRedis : ICacheCurto
    {
        private readonly IDatabase _redis;

        public CacheCurtoRedis(IConnectionMultiplexer conexao)
        {
            _redis = conexao.GetDatabase();
        }

        public string? Obter(string chave)
        {
            RedisValue valor = _redis.StringGet("perg:" + chave);
            return valor.IsNullOrEmpty ? null : valor.ToString();
        }

        public void Gravar(string chave, string valor, TimeSpan validade) => _redis.StringSet("perg:" + chave, valor, validade);

        public void Remover(string chave) => _redis.KeyDelete("perg:" + chave);
    }
}
