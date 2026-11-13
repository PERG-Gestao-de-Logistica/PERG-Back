-- Para quem criou o banco ANTES de 13/11/2026: acrescenta a integração com sistemas de vendas.
-- Quem cria o banco do zero com perg_modelo_banco.sql não precisa rodar este arquivo.
-- Pedidos podem chegar pela API de integração
ALTER TABLE pedidos DROP CONSTRAINT pedidos_canal_check;
ALTER TABLE pedidos ADD CONSTRAINT pedidos_canal_check CHECK (canal IN ('manual','planilha','api'));

-- Chaves de API da loja (guardamos só o hash; a chave aparece uma única vez ao ser criada)
CREATE TABLE chaves_api (
    id             uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id     uuid         NOT NULL REFERENCES empresas(id),
    nome           varchar(100) NOT NULL,               -- ex.: "ERP Bling", "Site próprio"
    prefixo        varchar(16)  NOT NULL,               -- começo da chave, para a loja reconhecer
    hash_chave     varchar(64)  NOT NULL UNIQUE,        -- SHA-256 em hexadecimal
    criado_por     uuid         REFERENCES usuarios(id),
    criado_em      timestamptz  NOT NULL DEFAULT now(),
    ultimo_uso_em  timestamptz,
    revogada_em    timestamptz
);
CREATE INDEX ix_chaves_api_empresa ON chaves_api (empresa_id);

-- Endereços que recebem os eventos dos pedidos da loja
CREATE TABLE webhooks (
    id               uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id       uuid        NOT NULL REFERENCES empresas(id),
    url              text        NOT NULL,
    segredo          varchar(64) NOT NULL,              -- assina o corpo (HMAC-SHA256)
    ativo            boolean     NOT NULL DEFAULT true,
    falhas_seguidas  integer     NOT NULL DEFAULT 0,
    criado_em        timestamptz NOT NULL DEFAULT now(),
    atualizado_em    timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX ix_webhooks_empresa ON webhooks (empresa_id);

-- Cada envio de evento para um webhook, com novas tentativas em caso de falha
CREATE TABLE webhook_entregas (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    webhook_id            uuid        NOT NULL REFERENCES webhooks(id) ON DELETE CASCADE,
    empresa_id            uuid        NOT NULL REFERENCES empresas(id),
    evento                varchar(50) NOT NULL,          -- ex.: pedido.saiu_para_entrega
    payload               jsonb       NOT NULL,
    status                varchar(20) NOT NULL DEFAULT 'pendente'
                          CHECK (status IN ('pendente','entregue','falhou')),
    tentativas            integer     NOT NULL DEFAULT 0,
    proxima_tentativa_em  timestamptz NOT NULL DEFAULT now(),
    ultimo_status_http    integer,
    ultimo_erro           text,
    entregue_em           timestamptz,
    criado_em             timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX ix_webhook_entregas_pendentes ON webhook_entregas (proxima_tentativa_em) WHERE status = 'pendente';
CREATE INDEX ix_webhook_entregas_webhook ON webhook_entregas (webhook_id, criado_em DESC);
