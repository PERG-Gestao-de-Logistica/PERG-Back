-- Para quem criou o banco ANTES de 26/11/2026: valores pagos aos entregadores, por loja e tipo de veículo.
-- Quem cria o banco do zero com perg_modelo_banco.sql não precisa rodar este arquivo.
-- Sem linha para um tipo = vale a tabela padrão do Perg (ParametrosVeiculo.ValoresPadrao).

CREATE TABLE empresa_valores_veiculo (
    empresa_id     uuid         NOT NULL REFERENCES empresas(id),
    tipo_veiculo   varchar(20)  NOT NULL
                   CHECK (tipo_veiculo IN ('bike', 'moto', 'carro', 'van', 'caminhao')),
    base_rota      numeric(8,2) NOT NULL CHECK (base_rota >= 0),     -- valor fixo por rota
    por_km         numeric(8,2) NOT NULL CHECK (por_km >= 0),        -- por km rodado
    por_entrega    numeric(8,2) NOT NULL CHECK (por_entrega >= 0),   -- por entrega da rota
    criado_em      timestamptz  NOT NULL DEFAULT now(),
    atualizado_em  timestamptz  NOT NULL DEFAULT now(),
    PRIMARY KEY (empresa_id, tipo_veiculo)
);
