using Perg.Api.Domains;
using Perg.Api.Interfaces;

namespace Perg.Api.Applications.Eventos
{
    // Cada consumidor recebe o evento uma única vez (mensagens_processadas); se um falhar, a mensagem
    // volta na próxima rodada só para quem não processou.
    public class PublicadorInterno : IPublicadorEventos
    {
        private readonly IEnumerable<IConsumidorEvento> _consumidores;
        private readonly IOutboxRepository _repository;

        public PublicadorInterno(IEnumerable<IConsumidorEvento> consumidores, IOutboxRepository repository)
        {
            _consumidores = consumidores;
            _repository = repository;
        }

        public void Publicar(Outbox mensagem)
        {
            foreach (IConsumidorEvento consumidor in _consumidores.Where(c => c.Aceita(mensagem.Topico, mensagem.TipoEvento)))
            {
                if (_repository.JaProcessada(consumidor.Nome, mensagem.Id))
                {
                    continue;
                }

                consumidor.Processar(mensagem);
                _repository.RegistrarProcessada(consumidor.Nome, mensagem.Id);
            }
        }
    }
}
