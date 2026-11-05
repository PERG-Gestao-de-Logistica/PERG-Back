namespace Perg.Api.DTOs
{
    public class ListaPaginadaDto<T>
    {
        public List<T> Itens { get; set; } = new();
        public int Total { get; set; }
        public int Pagina { get; set; }
        public int TamanhoPagina { get; set; }
    }
}
