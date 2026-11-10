using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Perg.Api.Interfaces;

namespace Perg.Api.Applications.Autenticacao
{
    // Guarda os códigos no IMemoryCache: somem se a API reiniciar (aceitável em dev; produção → Redis)
    public class CodigosVerificacaoMemoria : ICodigosVerificacao
    {
        private static readonly TimeSpan Validade = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan IntervaloReenvio = TimeSpan.FromMinutes(1);
        private const int MaximoTentativas = 5;

        private readonly IMemoryCache _cache;
        private readonly object _trava = new();

        public CodigosVerificacaoMemoria(IMemoryCache cache)
        {
            _cache = cache;
        }

        public string? Gerar(string chave)
        {
            lock (_trava)
            {
                if (_cache.TryGetValue("reenvio:" + chave, out _))
                {
                    return null;
                }

                string codigo = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
                _cache.Set("codigo:" + chave, new CodigoGuardado(codigo), Validade);
                _cache.Set("reenvio:" + chave, true, IntervaloReenvio);
                return codigo;
            }
        }

        public ResultadoCodigo Validar(string chave, string codigo)
        {
            lock (_trava)
            {
                if (!_cache.TryGetValue("codigo:" + chave, out CodigoGuardado? guardado) || guardado == null)
                {
                    return ResultadoCodigo.Expirado;
                }

                if (guardado.Tentativas >= MaximoTentativas)
                {
                    return ResultadoCodigo.TentativasEsgotadas;
                }

                bool confere = CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(guardado.Codigo),
                    Encoding.UTF8.GetBytes((codigo ?? string.Empty).Trim()));

                if (!confere)
                {
                    guardado.Tentativas++;
                    return guardado.Tentativas >= MaximoTentativas ? ResultadoCodigo.TentativasEsgotadas : ResultadoCodigo.Invalido;
                }

                _cache.Remove("codigo:" + chave);
                return ResultadoCodigo.Valido;
            }
        }

        private class CodigoGuardado
        {
            public CodigoGuardado(string codigo) => Codigo = codigo;
            public string Codigo { get; }
            public int Tentativas { get; set; }
        }
    }
}
