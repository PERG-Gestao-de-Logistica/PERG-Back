using Microsoft.Extensions.Caching.Memory;
using Perg.Api.Interfaces;

namespace Perg.Api.Applications.Cache
{
    public class CacheCurtoMemoria : ICacheCurto
    {
        private readonly IMemoryCache _cache;

        public CacheCurtoMemoria(IMemoryCache cache)
        {
            _cache = cache;
        }

        public string? Obter(string chave) => _cache.TryGetValue(chave, out string? valor) ? valor : null;

        public void Gravar(string chave, string valor, TimeSpan validade) => _cache.Set(chave, valor, validade);

        public void Remover(string chave) => _cache.Remove(chave);
    }
}
