-- Para quem criou o banco ANTES de 11/11/2026: acrescenta o backoffice.
-- Quem cria o banco do zero com perg_modelo_banco.sql não precisa rodar este arquivo.
CREATE TABLE administradores (
    id                uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    nome              varchar(150) NOT NULL,
    email             varchar(255) NOT NULL UNIQUE,
    senha_hash        text         NOT NULL,
    trocar_senha      boolean      NOT NULL DEFAULT false,
    ativo             boolean      NOT NULL DEFAULT true,
    ultimo_acesso_em  timestamptz,
    criado_em         timestamptz  NOT NULL DEFAULT now(),
    atualizado_em     timestamptz  NOT NULL DEFAULT now()
);

ALTER TABLE documentos_entregador ADD COLUMN analisado_por uuid REFERENCES administradores(id);
ALTER TABLE entregadores          ADD COLUMN aprovado_por  uuid REFERENCES administradores(id);
