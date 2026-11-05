namespace Perg.Api.Applications.Geocodificacao
{
    // Endereço que vai para o serviço de geocodificação
    public class EnderecoGeocodificacao
    {
        public string? Logradouro { get; set; }
        public string? Numero { get; set; }
        public string? Bairro { get; set; }
        public string? Cidade { get; set; }
        public string? Uf { get; set; }
        public string? Cep { get; set; }

        // "Avenida Paulista, 1000 - Bela Vista, São Paulo - SP, 01310100, Brasil"
        public string ParaTexto()
        {
            var partes = new List<string>();

            string rua = string.Join(", ", new[] { Logradouro, Numero }.Where(p => !string.IsNullOrWhiteSpace(p)));
            if (!string.IsNullOrWhiteSpace(Bairro))
            {
                rua = string.IsNullOrWhiteSpace(rua) ? Bairro! : $"{rua} - {Bairro}";
            }
            if (!string.IsNullOrWhiteSpace(rua)) partes.Add(rua);

            string cidade = string.Join(" - ", new[] { Cidade, Uf }.Where(p => !string.IsNullOrWhiteSpace(p)));
            if (!string.IsNullOrWhiteSpace(cidade)) partes.Add(cidade);

            if (!string.IsNullOrWhiteSpace(Cep)) partes.Add(Cep!);

            partes.Add("Brasil");
            return string.Join(", ", partes);
        }
    }
}
