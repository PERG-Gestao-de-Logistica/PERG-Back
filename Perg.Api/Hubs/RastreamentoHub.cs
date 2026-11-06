using Microsoft.AspNetCore.SignalR;
using Perg.Api.Applications.Autenticacao;
using Perg.Api.Applications.TempoReal;
using Perg.Api.Domains;
using Perg.Api.Interfaces;

namespace Perg.Api.Hubs
{
    // Conexão em tempo real em /hubs/rastreamento
    //  - Loja (com token): entra sozinha nos grupos da empresa e do usuário ao conectar.
    //  - Cliente (sem login): chama AcompanharPedido(token do link de rastreio).
    public class RastreamentoHub : Hub
    {
        private readonly IPedidoRepository _pedidoRepository;

        public RastreamentoHub(IPedidoRepository pedidoRepository)
        {
            _pedidoRepository = pedidoRepository;
        }

        public override async Task OnConnectedAsync()
        {
            string? empresaId = Context.User?.FindFirst(GeradorTokenJwt.ClaimEmpresaId)?.Value;
            string? usuarioId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (Guid.TryParse(empresaId, out Guid empresa) && Guid.TryParse(usuarioId, out Guid usuario))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, GruposTempoReal.Empresa(empresa));
                await Groups.AddToGroupAsync(Context.ConnectionId, GruposTempoReal.Usuario(usuario));
            }

            await base.OnConnectedAsync();
        }

        // Página de rastreio do cliente
        public async Task<bool> AcompanharPedido(string token)
        {
            Pedido? pedido = _pedidoRepository.ObterPorToken(token);

            if (pedido == null || (pedido.TokenExpiraEm.HasValue && pedido.TokenExpiraEm.Value < DateTime.UtcNow))
            {
                return false;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, GruposTempoReal.Pedido(pedido.Id));
            return true;
        }
    }
}
