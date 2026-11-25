-- Para quem criou o banco ANTES de 25/11/2026: campos novos das origens (tela Configurações > Origens).
-- Quem cria o banco do zero com perg_modelo_banco.sql não precisa rodar este arquivo.

ALTER TABLE origens
    ADD COLUMN tipo                 smallint     NOT NULL DEFAULT 1 CHECK (tipo IN (1, 2)),  -- 1 Loja, 2 Depósito
    ADD COLUMN responsavel_nome     varchar(150),
    ADD COLUMN responsavel_telefone varchar(20),
    -- dias abertos em bits: 1 dom, 2 seg, 4 ter, 8 qua, 16 qui, 32 sex, 64 sáb (62 = seg a sex)
    ADD COLUMN dias_funcionamento   smallint     NOT NULL DEFAULT 62
                                    CHECK (dias_funcionamento BETWEEN 1 AND 127),
    ADD COLUMN instrucoes_coleta    varchar(300);

-- Dias a partir dos campos antigos (seg a sex sempre abertos)
UPDATE origens
SET dias_funcionamento = 62
    + CASE WHEN abre_sabado  THEN 64 ELSE 0 END
    + CASE WHEN abre_domingo THEN 1  ELSE 0 END;

-- Responsável no local: começa com o usuário que cadastrou a origem
UPDATE origens o
SET responsavel_nome = u.nome,
    responsavel_telefone = u.telefone
FROM usuarios u
WHERE u.id = o.responsavel_id
  AND o.responsavel_nome IS NULL;
