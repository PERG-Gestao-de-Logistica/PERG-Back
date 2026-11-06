using Perg.Api.Domains.Enums;

namespace Perg.Api.Applications.Execucao
{
    public static class TextosOcorrencia
    {
        public static string Descrever(TipoOcorrencia tipo) => tipo switch
        {
            TipoOcorrencia.ClienteAusente => "cliente ausente",
            TipoOcorrencia.EnderecoNaoEncontrado => "endereço não encontrado",
            TipoOcorrencia.Recusado => "entrega recusada",
            TipoOcorrencia.Avaria => "produto avariado",
            _ => "outro motivo"
        };
    }
}
