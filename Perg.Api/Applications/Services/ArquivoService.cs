using Perg.Api.Applications.Arquivos;
using Perg.Api.Domains;
using Perg.Api.Domains.Enums;
using Perg.Api.DTOs.ArquivosDto;
using Perg.Api.Exceptions;
using Perg.Api.Interfaces;

namespace Perg.Api.Applications.Services
{
    public record ArquivoParaDownload(Stream Conteudo, string TipoConteudo);

    public class ArquivoService
    {
        private static readonly string[] CategoriasCnh = { "A", "B", "AB", "C", "D", "E", "AC", "AD", "AE" };

        private readonly IArquivoRepository _repository;
        private readonly IArmazenamentoArquivos _armazenamento;

        public ArquivoService(IArquivoRepository repository, IArmazenamentoArquivos armazenamento)
        {
            _repository = repository;
            _armazenamento = armazenamento;
        }

        // ---------- App do entregador ----------

        // Foto do comprovante ou da ocorrência: sobe antes, e a chave vai junto no "entregar"/"ocorrencia"
        public LerArquivoEnviadoDto EnviarArquivoApp(Guid entregadorId, FinalidadeArquivoApp finalidade, IFormFile? arquivo)
        {
            if (!Enum.IsDefined(finalidade))
            {
                throw new DomainException("Finalidade inválida.");
            }

            ArquivoValidado tipo = ValidadorArquivo.Validar(arquivo);
            string pasta = finalidade == FinalidadeArquivoApp.Comprovante
                ? PastasArquivos.Comprovantes(entregadorId)
                : PastasArquivos.Ocorrencias(entregadorId);

            return new LerArquivoEnviadoDto { Chave = Salvar(arquivo!, pasta, tipo) };
        }

        public LerArquivoEnviadoDto AtualizarFotoEntregador(Guid entregadorId, IFormFile? arquivo)
        {
            ArquivoValidado tipo = ValidadorArquivo.Validar(arquivo);
            Entregador entregador = _repository.ObterEntregador(entregadorId)
                ?? throw new DomainException("Entregador não encontrado.");

            string? antiga = entregador.FotoChave;
            entregador.FotoChave = Salvar(arquivo!, PastasArquivos.FotoEntregador(entregadorId), tipo);
            _repository.Salvar();
            RemoverSeExistir(antiga);

            return new LerArquivoEnviadoDto { Chave = entregador.FotoChave, Url = PastasArquivos.UrlPublica(entregador.FotoChave) };
        }

        public LerDocumentoDto EnviarDocumento(Guid entregadorId, EnviarDocumentoDto dto)
        {
            if (!Enum.IsDefined(dto.Tipo))
            {
                throw new DomainException("Tipo de documento inválido.");
            }

            Entregador entregador = _repository.ObterEntregador(entregadorId)
                ?? throw new DomainException("Entregador não encontrado.");

            ArquivoValidado tipoArquivo = ValidadorArquivo.Validar(dto.Arquivo, permitirPdf: dto.Tipo != TipoDocumento.Selfie);

            Guid? veiculoId = null;
            string? numero = null;
            string? categoria = null;

            if (dto.Tipo == TipoDocumento.Crlv)
            {
                Guid id = dto.VeiculoId ?? entregador.VeiculoAtualId
                    ?? throw new DomainException("Informe o veículo do CRLV.");
                Veiculo veiculo = _repository.ObterVeiculo(id, entregadorId)
                    ?? throw new DomainException("Veículo não encontrado.");
                veiculoId = veiculo.Id;
            }

            if (dto.Tipo == TipoDocumento.Cnh)
            {
                numero = new string((dto.Numero ?? string.Empty).Where(char.IsDigit).ToArray());
                categoria = dto.Categoria?.Trim().ToUpperInvariant();

                if (numero.Length != 11) throw new DomainException("O número da CNH tem 11 dígitos.");
                if (categoria == null || !CategoriasCnh.Contains(categoria)) throw new DomainException("Categoria da CNH inválida.");
                if (dto.Validade.HasValue && dto.Validade.Value < DateOnly.FromDateTime(DateTime.UtcNow))
                {
                    throw new DomainException("Esta CNH está vencida.");
                }
                if (_repository.CnhEmUsoPorOutro(numero, entregadorId))
                {
                    throw new DomainException("Esta CNH já está cadastrada em outra conta.");
                }
            }

            if (_repository.ExisteDocumentoAtivo(entregadorId, dto.Tipo, veiculoId))
            {
                throw new DomainException("Você já enviou este documento. Aguarde a análise ou o resultado.");
            }

            var documento = new DocumentoEntregador
            {
                Id = Guid.CreateVersion7(),
                EntregadorId = entregadorId,
                VeiculoId = veiculoId,
                Tipo = dto.Tipo,
                ArquivoChave = Salvar(dto.Arquivo!, PastasArquivos.Documentos(entregadorId), tipoArquivo),
                Numero = numero,
                Categoria = categoria,
                Ear = dto.Tipo == TipoDocumento.Cnh ? dto.Ear : null,
                Validade = dto.Validade,
                Status = StatusDocumento.Pendente
            };

            _repository.Adicionar(documento);
            _repository.Salvar();

            return ParaDto(documento);
        }

        public List<LerDocumentoDto> ListarDocumentos(Guid entregadorId)
        {
            return _repository.ListarDocumentos(entregadorId).Select(ParaDto).ToList();
        }

        // ---------- Loja ----------

        public LerArquivoEnviadoDto AtualizarFotoLoja(Guid empresaId, IFormFile? arquivo)
        {
            ArquivoValidado tipo = ValidadorArquivo.Validar(arquivo);
            Empresa empresa = _repository.ObterEmpresa(empresaId)
                ?? throw new DomainException("Empresa não encontrada.");

            string? antiga = empresa.FotoChave;
            empresa.FotoChave = Salvar(arquivo!, PastasArquivos.FotoLoja(empresaId), tipo);
            _repository.Salvar();
            RemoverSeExistir(antiga);

            return new LerArquivoEnviadoDto { Chave = empresa.FotoChave, Url = PastasArquivos.UrlPublica(empresa.FotoChave) };
        }

        public LerArquivoEnviadoDto AtualizarFotoUsuario(Guid usuarioId, IFormFile? arquivo)
        {
            ArquivoValidado tipo = ValidadorArquivo.Validar(arquivo);
            Usuario usuario = _repository.ObterUsuario(usuarioId)
                ?? throw new DomainException("Usuário não encontrado.");

            string? antiga = usuario.FotoChave;
            usuario.FotoChave = Salvar(arquivo!, PastasArquivos.FotoUsuario(usuarioId), tipo);
            _repository.Salvar();
            RemoverSeExistir(antiga);

            return new LerArquivoEnviadoDto { Chave = usuario.FotoChave, Url = PastasArquivos.UrlPublica(usuario.FotoChave) };
        }

        // Só pedidos da loja logada (o filtro por empresa garante)
        public ArquivoParaDownload AbrirComprovante(Guid pedidoId)
        {
            Parada parada = _repository.ObterParadaComComprovante(pedidoId)
                ?? throw new DomainException("Este pedido não tem foto ou assinatura de comprovante.");

            return Abrir(parada.ComprovanteChave!);
        }

        public ArquivoParaDownload AbrirFotoOcorrencia(Guid ocorrenciaId)
        {
            Ocorrencia ocorrencia = _repository.ObterOcorrencia(ocorrenciaId)
                ?? throw new DomainException("Ocorrência não encontrada.");

            if (string.IsNullOrWhiteSpace(ocorrencia.FotoChave))
            {
                throw new DomainException("Esta ocorrência não tem foto.");
            }

            return Abrir(ocorrencia.FotoChave);
        }

        // ---------- Público ----------

        // Fotos de perfil e da loja (pasta "publico/"); documentos e comprovantes nunca saem por aqui
        public ArquivoParaDownload AbrirPublico(string chave)
        {
            if (!chave.StartsWith(PastasArquivos.PrefixoPublico))
            {
                throw new DomainException("Arquivo não encontrado.");
            }

            return Abrir(chave);
        }

        // ---------- apoio ----------

        private string Salvar(IFormFile arquivo, string pasta, ArquivoValidado tipo)
        {
            using Stream conteudo = arquivo.OpenReadStream();
            return _armazenamento.Salvar(conteudo, pasta, tipo.Extensao);
        }

        private ArquivoParaDownload Abrir(string chave)
        {
            Stream? conteudo;
            try
            {
                conteudo = _armazenamento.Abrir(chave);
            }
            catch (ArgumentException)
            {
                conteudo = null;
            }

            if (conteudo == null)
            {
                throw new DomainException("Arquivo não encontrado.");
            }

            return new ArquivoParaDownload(conteudo, ValidadorArquivo.TipoConteudoPelaChave(chave));
        }

        private void RemoverSeExistir(string? chave)
        {
            if (string.IsNullOrWhiteSpace(chave)) return;
            try { _armazenamento.Remover(chave); } catch (ArgumentException) { /* chave antiga fora do padrão */ }
        }

        private static LerDocumentoDto ParaDto(DocumentoEntregador d)
        {
            return new LerDocumentoDto
            {
                Id = d.Id,
                Tipo = d.Tipo,
                Status = d.Status,
                Categoria = d.Categoria,
                Validade = d.Validade,
                MotivoRejeicao = d.MotivoRejeicao,
                CriadoEm = d.CriadoEm,
                AnalisadoEm = d.AnalisadoEm
            };
        }
    }
}
