using Perg.Api.Domains;
using Perg.Api.DTOs;
using Perg.Api.DTOs.NotificacoesDto;
using Perg.Api.Exceptions;
using Perg.Api.Interfaces;

namespace Perg.Api.Applications.Services
{
    public class NotificacaoService
    {
        private readonly INotificacaoRepository _repository;

        public NotificacaoService(INotificacaoRepository repository)
        {
            _repository = repository;
        }

        public ListaPaginadaDto<LerNotificacaoDto> Listar(Guid usuarioId, bool somenteNaoLidas, int pagina, int tamanhoPagina)
        {
            pagina = Math.Max(pagina, 1);
            tamanhoPagina = Math.Clamp(tamanhoPagina, 1, 50);

            var (itens, total) = _repository.Listar(usuarioId, somenteNaoLidas, pagina, tamanhoPagina);

            return new ListaPaginadaDto<LerNotificacaoDto>
            {
                Itens = itens.Select(ParaDto).ToList(),
                Total = total,
                Pagina = pagina,
                TamanhoPagina = tamanhoPagina
            };
        }

        public ContagemNotificacoesDto Contar(Guid usuarioId)
        {
            return new ContagemNotificacoesDto { NaoLidas = _repository.ContarNaoLidas(usuarioId) };
        }

        public LerNotificacaoDto MarcarComoLida(Guid id, Guid usuarioId)
        {
            Notificacao notificacao = _repository.ObterPorId(id, usuarioId)
                ?? throw new DomainException("Notificação não encontrada.");

            if (notificacao.LidaEm == null)
            {
                notificacao.LidaEm = DateTime.UtcNow;
                _repository.Salvar();
            }

            return ParaDto(notificacao);
        }

        public int MarcarTodasComoLidas(Guid usuarioId)
        {
            return _repository.MarcarTodasComoLidas(usuarioId, DateTime.UtcNow);
        }

        public PreferenciasNotificacaoDto ObterPreferencias(Guid usuarioId)
        {
            Usuario usuario = _repository.ObterUsuario(usuarioId)
                ?? throw new DomainException("Usuário não encontrado.");

            return new PreferenciasNotificacaoDto { NotifOcorrencias = usuario.NotifOcorrencias, NotifCobranca = usuario.NotifCobranca };
        }

        public PreferenciasNotificacaoDto AtualizarPreferencias(Guid usuarioId, PreferenciasNotificacaoDto dto)
        {
            Usuario usuario = _repository.ObterUsuario(usuarioId)
                ?? throw new DomainException("Usuário não encontrado.");

            usuario.NotifOcorrencias = dto.NotifOcorrencias;
            usuario.NotifCobranca = dto.NotifCobranca;
            _repository.Salvar();

            return dto;
        }

        private static LerNotificacaoDto ParaDto(Notificacao n)
        {
            return new LerNotificacaoDto
            {
                Id = n.Id,
                Tipo = n.Tipo,
                Titulo = n.Titulo,
                Mensagem = n.Mensagem,
                Link = n.Link,
                Lida = n.LidaEm != null,
                CriadoEm = n.CriadoEm
            };
        }
    }
}
