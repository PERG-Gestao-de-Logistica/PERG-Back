using Perg.Api.Applications.Email;
using Perg.Api.Domains;
using Perg.Api.Interfaces;

namespace Perg.Api.Applications.Eventos
{
    // Avisa o cliente final por e-mail quando o pedido sai para entrega e quando é entregue (com o link de rastreio).
    // Só para pedidos com e-mail do cliente. Em desenvolvimento, Email:RedirecionarClientesPara manda tudo para um
    // endereço de teste (os pedidos de exemplo têm e-mails de outras pessoas).
    public class ConsumidorEmailCliente : IConsumidorEvento
    {
        // Evento velho (ex.: fila acumulada antes de o publicador existir) não vira e-mail
        private static readonly TimeSpan IdadeMaxima = TimeSpan.FromMinutes(30);

        private readonly IPedidoRepository _pedidos;
        private readonly IEmpresaRepository _empresas;
        private readonly IEnvioEmail _email;
        private readonly IConfiguration _config;
        private readonly ILogger<ConsumidorEmailCliente> _logger;

        public ConsumidorEmailCliente(IPedidoRepository pedidos, IEmpresaRepository empresas, IEnvioEmail email,
            IConfiguration config, ILogger<ConsumidorEmailCliente> logger)
        {
            _pedidos = pedidos;
            _empresas = empresas;
            _email = email;
            _config = config;
            _logger = logger;
        }

        public string Nome => "email-cliente";

        public bool Aceita(string topico, string tipoEvento)
        {
            return topico == "pedido" && tipoEvento is "PedidoSaiuParaEntrega" or "PedidoEntregue";
        }

        public void Processar(Outbox mensagem)
        {
            if (DateTime.UtcNow - mensagem.CriadoEm > IdadeMaxima)
            {
                return;
            }

            if (!Guid.TryParse(mensagem.Chave, out Guid pedidoId))
            {
                return;
            }

            Pedido? pedido = _pedidos.ObterPorId(pedidoId);
            if (pedido == null || string.IsNullOrWhiteSpace(pedido.ClienteEmail))
            {
                return;
            }

            Empresa? empresa = _empresas.ObterPorId(pedido.EmpresaId);
            string loja = empresa?.NomeFantasia ?? "a loja";
            string link = (_config["Front:UrlRastreio"] ?? "http://localhost:3000/rastreio/{token}")
                .Replace("{token}", Uri.EscapeDataString(pedido.TokenRastreamento));

            (string assunto, string html) = mensagem.TipoEvento == "PedidoSaiuParaEntrega"
                ? ModelosEmail.PedidoSaiuParaEntrega(pedido.ClienteNome, loja, pedido.NumeroExterno ?? pedido.NumeroPedido.ToString(), link)
                : ModelosEmail.PedidoEntregue(pedido.ClienteNome, loja, pedido.NumeroExterno ?? pedido.NumeroPedido.ToString());

            string? redirecionar = _config["Email:RedirecionarClientesPara"];
            string destino = string.IsNullOrWhiteSpace(redirecionar) ? pedido.ClienteEmail : redirecionar;

            // Falha de e-mail não trava a fila: o aviso ao cliente é "melhor esforço"
            if (!_email.Enviar(destino, assunto, html))
            {
                _logger.LogWarning("E-mail de {Evento} do pedido {Pedido} não enviado.", mensagem.TipoEvento, pedido.Id);
            }
        }
    }
}
