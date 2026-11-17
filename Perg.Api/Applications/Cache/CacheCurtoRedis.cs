using Perg.Api.Interfaces;
using StackExchange.Redis;

namespace Perg.Api.Applications.Cache
{
    // Redis fora do ar ou lento não pode derrubar as requisições: sem cache, quem chama consulta o banco.
    public class CacheCurtoRedis : ICacheCurto
    {
        private readonly IDatabase _redis;
        private readonly ILogger<CacheCurtoRedis> _logger;

        public CacheCurtoRedis(IConnectionMultiplexer conexao, ILogger<CacheCurtoRedis> logger)
        {
            _redis = conexao.GetDatabase();
            _logger = logger;
        }

        public string? Obter(string chave)
        {
            try
            {
                RedisValue valor = _redis.StringGet("perg:" + chave);
                return valor.IsNullOrEmpty ? null : valor.ToString();
            }
            catch (Exception ex) when (ex is RedisException or RedisTimeoutException)
            {
                Avisar(ex);
                return null;
            }
        }

        public void Gravar(string chave, string valor, TimeSpan validade)
        {
            try
            {
                _redis.StringSet("perg:" + chave, valor, validade);
            }
            catch (Exception ex) when (ex is RedisException or RedisTimeoutException)
            {
                Avisar(ex);
            }
        }

        public void Remover(string chave)
        {
            try
            {
                _redis.KeyDelete("perg:" + chave);
            }
            catch (Exception ex) when (ex is RedisException or RedisTimeoutException)
            {
                Avisar(ex);
            }
        }

        private void Avisar(Exception ex) =>
            _logger.LogWarning("Redis indisponível ({Erro}); checagem de acesso direto no banco.", ex.GetType().Name);
    }
}
