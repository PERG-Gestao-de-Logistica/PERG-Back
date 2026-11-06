using Microsoft.AspNetCore.SignalR;
using Perg.Api.Domains;
using Perg.Api.DTOs.NotificacoesDto;
using Perg.Api.Hubs;

namespace Perg.Api.Applications.TempoReal
{
    // Empurra mensagens para o front pelo SignalR (sem o front precisar ficar perguntando)
    public class NotificadorTempoReal
    {
        private readonly IHubContext<RastreamentoHub> _hub;
        private readonly ILogger<NotificadorTempoReal> _logger;

        public NotificadorTempoReal(IHubContext<RastreamentoHub> hub, ILogger<NotificadorTempoReal> logger)
        {
            _hub = hub;
            _logger = logger;
        }

        // Chamado depois do SaveChanges: só avisa o que já está gravado
        public void EnviarNotificacoes(IEnumerable<Notificacao> notificacoes)
        {
            foreach (Notificacao n in notificacoes)
            {
                var dto = new LerNotificacaoDto
                {
                    Id = n.Id,
                    Tipo = n.Tipo,
                    Titulo = n.Titulo,
                    Mensagem = n.Mensagem,
                    Link = n.Link,
                    Lida = false,
                    CriadoEm = n.CriadoEm
                };
                Enviar(GruposTempoReal.Usuario(n.UsuarioId), "NovaNotificacao", dto);
            }
        }

        public void EnviarPosicaoParaLoja(Guid empresaId, PosicaoAtual posicao)
        {
            Enviar(GruposTempoReal.Empresa(empresaId), "PosicaoEntregador", new
            {
                posicao.EntregadorId,
                posicao.EntregadorNome,
                posicao.RotaId,
                posicao.Latitude,
                posicao.Longitude,
                posicao.VelocidadeKmh,
                posicao.RegistradaEm
            });
        }

        // Para o cliente vai só a posição (sem nome nem rota)
        public void EnviarPosicaoParaCliente(Guid pedidoId, PosicaoAtual posicao)
        {
            Enviar(GruposTempoReal.Pedido(pedidoId), "PosicaoEntregador", new
            {
                posicao.Latitude,
                posicao.Longitude,
                posicao.RegistradaEm
            });
        }

        // Falha no envio em tempo real nunca derruba a requisição: o dado já está salvo
        private void Enviar(string grupo, string metodo, object dados)
        {
            _hub.Clients.Group(grupo).SendAsync(metodo, dados).ContinueWith(tarefa =>
            {
                if (tarefa.IsFaulted)
                {
                    _logger.LogWarning(tarefa.Exception, "Falha ao enviar {Metodo} para {Grupo}.", metodo, grupo);
                }
            });
        }
    }
}
