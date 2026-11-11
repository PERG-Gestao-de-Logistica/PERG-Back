-- =====================================================================
--  Perg — modelo do banco de dados (versão simplificada)
--  PostgreSQL 15+ · sem extensões · 27 tabelas + 1 view
-- =====================================================================
--  Convenções
--  * Nomes em português, snake_case, tabelas no plural.
--  * id uuid gerado pela aplicação (Guid.CreateVersion7()). O DEFAULT é só reserva.
--  * Datas em timestamptz (UTC). O "hoje" de cada loja usa empresas.fuso_horario.
--  * Listas fixas: smallint + enum no C# (segmento, status de empresa e de
--    assinatura) ou varchar + CHECK (demais status), que no C# também viram enum.
--  * criado_em usa DEFAULT now(); atualizado_em é preenchido pelo EF Core
--    (interceptor no SaveChanges).
--  * Isolamento entre lojas: filtro global do EF Core (HasQueryFilter) em toda
--    tabela com empresa_id.
--  * E-mails são gravados em minúsculas pelo código.
--  * Localização: latitude e longitude numeric(9,6). Distâncias e trajetos vêm do OSRM.
--  * Única regra em gatilho: o número sequencial do pedido por empresa.
-- =====================================================================


-- =====================================================================
-- 1. CONTA
-- =====================================================================

CREATE TABLE planos (
    id                  uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    nome                varchar(50)   NOT NULL UNIQUE,
    preco_mensal        numeric(10,2) NOT NULL CHECK (preco_mensal >= 0),
    limite_pedidos_mes  integer,                          -- NULL = ilimitado
    ativo               boolean       NOT NULL DEFAULT true,
    criado_em           timestamptz   NOT NULL DEFAULT now()
);

CREATE TABLE empresas (
    id                        uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    cnpj                      varchar(14)  NOT NULL UNIQUE
                              CHECK (cnpj ~ '^[0-9A-Z]{12}[0-9]{2}$'),   -- aceita o CNPJ alfanumérico
    razao_social              varchar(150) NOT NULL,
    nome_fantasia             varchar(100) NOT NULL,
    email                     varchar(255) NOT NULL,
    telefone                  varchar(20),
    foto_chave                text,                               -- caminho no Blob Storage
    -- 1 Alimentação, 2 Mercado e conveniência, 3 Farmácia e saúde, 4 Moda e acessórios,
    -- 5 Eletrônicos e informática, 6 Casa e construção, 7 Pet shop, 8 Outros
    segmento                  smallint     NOT NULL CHECK (segmento BETWEEN 1 AND 8),
    faixa_pedidos_dia         varchar(20)  NOT NULL,              -- valores do menu "Pedidos por dia"
    -- "Como você entrega?"
    entrega_mesmo_dia         boolean      NOT NULL DEFAULT false,
    entrega_agendada          boolean      NOT NULL DEFAULT false,
    retirada_loja             boolean      NOT NULL DEFAULT false,
    -- "Canais de venda"
    vende_site_proprio        boolean      NOT NULL DEFAULT false,
    vende_whatsapp            boolean      NOT NULL DEFAULT false,
    vende_shopify             boolean      NOT NULL DEFAULT false,
    -- "Veículos que serão utilizados"
    usa_carro                 boolean      NOT NULL DEFAULT false,
    usa_moto                  boolean      NOT NULL DEFAULT false,
    usa_van_caminhao          boolean      NOT NULL DEFAULT false,
    somente_favoritos         boolean      NOT NULL DEFAULT false,  -- chave "Só preferenciais"
    tempo_oferta_favoritos_s  integer      NOT NULL DEFAULT 120 CHECK (tempo_oferta_favoritos_s > 0),
    fuso_horario              varchar(40)  NOT NULL DEFAULT 'America/Sao_Paulo',
    meta_no_prazo_pct         numeric(5,2) NOT NULL DEFAULT 95,
    status                    smallint     NOT NULL DEFAULT 1
                              CHECK (status BETWEEN 1 AND 3),     -- 1 Ativa, 2 Suspensa, 3 Cancelada
    criado_em                 timestamptz  NOT NULL DEFAULT now(),
    atualizado_em             timestamptz  NOT NULL DEFAULT now(),
    -- ao menos uma forma de entrega
    CONSTRAINT ck_empresas_modalidade
        CHECK (entrega_mesmo_dia OR entrega_agendada OR retirada_loja),
    -- quem entrega precisa informar ao menos um veículo
    CONSTRAINT ck_empresas_veiculo
        CHECK (NOT (entrega_mesmo_dia OR entrega_agendada) OR usa_carro OR usa_moto OR usa_van_caminhao)
);

CREATE TABLE assinaturas (
    id                     uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id             uuid         NOT NULL REFERENCES empresas(id),
    plano_id               uuid         NOT NULL REFERENCES planos(id),
    status                 smallint     NOT NULL DEFAULT 1
                           CHECK (status BETWEEN 1 AND 3),        -- 1 Ativa, 2 Inadimplente, 3 Cancelada
    gateway                varchar(20),                           -- asaas, pagarme, stripe
    gateway_assinatura_id  varchar(100),
    proximo_vencimento     date,
    cancelada_em           timestamptz,
    criado_em              timestamptz  NOT NULL DEFAULT now(),
    atualizado_em          timestamptz  NOT NULL DEFAULT now()
);
-- no máximo uma assinatura não cancelada por empresa
CREATE UNIQUE INDEX ux_assinaturas_vigente ON assinaturas (empresa_id) WHERE status <> 3;

-- Membros da empresa e login do portal (ASP.NET Core Identity).
-- O admin cria o usuário com uma senha inicial; no primeiro login ele troca
-- a senha e aceita os termos. "Esqueci minha senha" usa um link assinado por
-- e-mail, válido por 30 minutos, sem coluna no banco.
CREATE TABLE usuarios (
    id                 uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id         uuid         NOT NULL REFERENCES empresas(id),
    nome               varchar(150) NOT NULL,
    email              varchar(255) NOT NULL UNIQUE,
    telefone           varchar(20),
    papel              varchar(35)  NOT NULL DEFAULT 'funcionario'
                       CHECK (papel IN ('admin','funcionario')),  -- campo "Cargo" da tela
    senha_hash         text,                       -- NULL só para quem entra apenas com Google
    trocar_senha       boolean      NOT NULL DEFAULT false,      -- obriga a troca no próximo login
    email_confirmado   boolean      NOT NULL DEFAULT false,
    foto_chave         text,
    ativo              boolean      NOT NULL DEFAULT true,       -- desativado, nunca apagado
    notif_ocorrencias  boolean      NOT NULL DEFAULT true,
    notif_cobranca     boolean      NOT NULL DEFAULT true,
    termos_versao      varchar(20),
    termos_aceitos_em  timestamptz,
    ultimo_acesso_em   timestamptz,
    criado_em          timestamptz  NOT NULL DEFAULT now(),
    atualizado_em      timestamptz  NOT NULL DEFAULT now()
);
CREATE INDEX ix_usuarios_empresa ON usuarios (empresa_id);

-- "Entrar com Google" / "Cadastrar com Google"
CREATE TABLE usuarios_logins_externos (
    provedor        varchar(20)  NOT NULL CHECK (provedor IN ('google')),
    chave_provedor  varchar(255) NOT NULL,         -- id fixo da conta no Google ("sub"), não o e-mail
    usuario_id      uuid         NOT NULL REFERENCES usuarios(id) ON DELETE CASCADE,
    criado_em       timestamptz  NOT NULL DEFAULT now(),
    PRIMARY KEY (provedor, chave_provedor)
);
CREATE INDEX ix_logins_externos_usuario ON usuarios_logins_externos (usuario_id);

-- Pontos de coleta, com o horário de funcionamento na própria linha
CREATE TABLE origens (
    id              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id      uuid         NOT NULL REFERENCES empresas(id),
    nome            varchar(100) NOT NULL,         -- no cadastro, recebe o nome fantasia
    responsavel_id  uuid         REFERENCES usuarios(id),
    cep             varchar(8)   NOT NULL,
    logradouro      varchar(150) NOT NULL,
    numero          varchar(10)  NOT NULL,         -- aceita "S/N", "120A"
    complemento     varchar(100),
    bairro          varchar(100) NOT NULL,         -- bairro, cidade e UF vêm da consulta do CEP
    cidade          varchar(100) NOT NULL,
    uf              varchar(2)   NOT NULL,
    latitude        numeric(9,6),                  -- preenchidas pela geocodificação
    longitude       numeric(9,6),
    abre            time         NOT NULL,
    fecha           time         NOT NULL,
    abre_sabado     boolean      NOT NULL DEFAULT true,
    abre_domingo    boolean      NOT NULL DEFAULT false,
    padrao          boolean      NOT NULL DEFAULT false,
    ativa           boolean      NOT NULL DEFAULT true,
    criado_em       timestamptz  NOT NULL DEFAULT now(),
    atualizado_em   timestamptz  NOT NULL DEFAULT now(),
    CONSTRAINT ck_origens_horario CHECK (fecha > abre)
);
-- uma única origem padrão por empresa
CREATE UNIQUE INDEX ux_origens_padrao ON origens (empresa_id) WHERE padrao;

-- Sino do topo: uma linha por usuário
CREATE TABLE notificacoes (
    id          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id  uuid         NOT NULL REFERENCES empresas(id),
    usuario_id  uuid         NOT NULL REFERENCES usuarios(id),
    tipo        varchar(30)  NOT NULL
                CHECK (tipo IN ('ocorrencia','rota_atrasada','rota_concluida',
                                'importacao','entregador_aceitou','cobranca','sistema')),
    titulo      varchar(150) NOT NULL,
    mensagem    text,
    link        varchar(255),                      -- tela que abre ao clicar
    lida_em     timestamptz,
    criado_em   timestamptz  NOT NULL DEFAULT now()
);
CREATE INDEX ix_notificacoes_nao_lidas ON notificacoes (usuario_id, criado_em DESC) WHERE lida_em IS NULL;


-- =====================================================================
-- 2. REDE PERG (compartilhada entre as lojas, sem empresa_id)
-- =====================================================================

-- A linha só é criada na última etapa do cadastro do app ("crie sua senha").
-- Até lá, o progresso fica no celular e os códigos de SMS/e-mail no Redis.
CREATE TABLE entregadores (
    id                   uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    nome                 varchar(150) NOT NULL,
    cpf                  varchar(11)  NOT NULL UNIQUE,      -- login: "CPF ou celular"
    telefone             varchar(20)  NOT NULL UNIQUE,      -- login: "CPF ou celular"
    email                varchar(255) NOT NULL UNIQUE,      -- "Esqueci a senha"
    foto_chave           text,
    cep                  varchar(8)   NOT NULL,
    logradouro           varchar(150) NOT NULL,
    numero               varchar(10)  NOT NULL,
    complemento          varchar(100),
    bairro               varchar(100) NOT NULL,
    cidade               varchar(100) NOT NULL,
    uf                   varchar(2)   NOT NULL,
    chave_pix            varchar(100),                      -- destino do "Sacar"
    senha_hash           text         NOT NULL,
    senha_alterada_em    timestamptz,
    telefone_confirmado  boolean      NOT NULL DEFAULT false,
    email_confirmado     boolean      NOT NULL DEFAULT false,
    termos_versao        varchar(20),
    termos_aceitos_em    timestamptz,                       -- inclui o consentimento de localização (LGPD)
    status               varchar(20)  NOT NULL DEFAULT 'em_analise'
                         CHECK (status IN ('em_analise','ativo','suspenso','inativo')),
    aprovado_em          timestamptz,
    criado_em            timestamptz  NOT NULL DEFAULT now(),
    atualizado_em        timestamptz  NOT NULL DEFAULT now(),
    CONSTRAINT ck_entregadores_ativo
        CHECK (status <> 'ativo' OR (termos_aceitos_em IS NOT NULL AND telefone_confirmado))
);

CREATE TABLE veiculos (
    id                  uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    entregador_id       uuid         NOT NULL REFERENCES entregadores(id),
    tipo                varchar(20)  NOT NULL CHECK (tipo IN ('bike','moto','carro','van','caminhao')),
    placa               varchar(8)   UNIQUE,
    modelo              varchar(60),
    capacidade_kg       numeric(8,2) NOT NULL CHECK (capacidade_kg > 0),  -- padrão por tipo
    capacidade_volumes  integer,
    capacidade_litros   integer,                           -- baú ou bag, ex.: 44
    verificado          boolean      NOT NULL DEFAULT false, -- CRLV aprovado
    ativo               boolean      NOT NULL DEFAULT true,
    criado_em           timestamptz  NOT NULL DEFAULT now(),
    atualizado_em       timestamptz  NOT NULL DEFAULT now(),
    CONSTRAINT ck_veiculos_placa CHECK (tipo = 'bike' OR placa IS NOT NULL)
);

-- veículo escolhido ao ficar online (criado aqui por causa da referência circular)
ALTER TABLE entregadores ADD COLUMN veiculo_atual_id uuid REFERENCES veiculos(id);

CREATE TABLE documentos_entregador (
    id               uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    entregador_id    uuid        NOT NULL REFERENCES entregadores(id),
    veiculo_id       uuid        REFERENCES veiculos(id),        -- só no CRLV
    tipo             varchar(30) NOT NULL
                     CHECK (tipo IN ('cnh','crlv','selfie','documento_identidade')),
    arquivo_chave    text,                                       -- foto no Blob Storage criptografado
    numero           varchar(20),                                -- número da CNH
    categoria        varchar(2)  CHECK (categoria IN ('A','B','AB','C','D','E','AC','AD','AE')),
    ear              boolean,                                    -- "Exerce Atividade Remunerada"
    status           varchar(20) NOT NULL DEFAULT 'pendente'
                     CHECK (status IN ('pendente','aprovado','rejeitado')),
    motivo_rejeicao  text,
    validade         date,
    analisado_em     timestamptz,
    criado_em        timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_documentos_crlv  CHECK (tipo <> 'crlv' OR veiculo_id IS NOT NULL),
    CONSTRAINT ck_documentos_cnh   CHECK (tipo <> 'cnh' OR (numero IS NOT NULL AND categoria IS NOT NULL)),
    CONSTRAINT ck_documentos_prova CHECK (arquivo_chave IS NOT NULL OR numero IS NOT NULL)
);
CREATE INDEX ix_documentos_entregador ON documentos_entregador (entregador_id, tipo);
-- a mesma CNH não pode estar em duas contas
CREATE UNIQUE INDEX ux_documentos_cnh ON documentos_entregador (numero)
    WHERE tipo = 'cnh' AND status <> 'rejeitado';


-- =====================================================================
-- 3. FAVORITOS
-- =====================================================================
-- A existência da linha significa "é favorito". Tocar no coração cria,
-- tocar de novo apaga.

CREATE TABLE empresa_favoritos (
    empresa_id     uuid        NOT NULL REFERENCES empresas(id),
    entregador_id  uuid        NOT NULL REFERENCES entregadores(id),
    criado_em      timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (empresa_id, entregador_id)
);


-- =====================================================================
-- 4. PEDIDOS
-- =====================================================================

CREATE TABLE importacoes (
    id                  uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id          uuid         NOT NULL REFERENCES empresas(id),
    usuario_id          uuid         NOT NULL REFERENCES usuarios(id),
    origem_id           uuid         NOT NULL REFERENCES origens(id),
    arquivo_nome        varchar(255) NOT NULL,
    arquivo_chave       text         NOT NULL,       -- planilha no Blob Storage
    mapeamento_colunas  jsonb,                       -- sugerido pela IA, confirmado pelo lojista
    status              varchar(30)  NOT NULL DEFAULT 'enviada'
                        CHECK (status IN ('enviada','mapeando','aguardando_confirmacao',
                                          'processando','concluida','falhou')),
    total_linhas        integer,
    linhas_importadas   integer      NOT NULL DEFAULT 0,
    linhas_com_erro     integer      NOT NULL DEFAULT 0,
    erros               jsonb,                       -- ex.: [{"linha": 12, "motivo": "CEP inválido"}]
    criado_em           timestamptz  NOT NULL DEFAULT now(),
    concluida_em        timestamptz
);

CREATE TABLE pedidos (
    id                        uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id                uuid         NOT NULL REFERENCES empresas(id),
    origem_id                 uuid         NOT NULL REFERENCES origens(id),
    importacao_id             uuid         REFERENCES importacoes(id),
    canal                     varchar(20)  NOT NULL CHECK (canal IN ('manual','planilha')),
    id_externo                varchar(100),           -- id no sistema do lojista (evita duplicar ao reimportar)
    numero_pedido             integer      NOT NULL,  -- preenchido pelo gatilho; no EF Core: ValueGeneratedOnAdd
    numero_externo            varchar(50),            -- número que o lojista já usa, ex.: #48213
    modalidade                varchar(20)  NOT NULL DEFAULT 'mesmo_dia'
                              CHECK (modalidade IN ('mesmo_dia','agendada','retirada')),
    status                    varchar(20)  NOT NULL DEFAULT 'novo'
                              CHECK (status IN ('novo','roteirizado','em_rota','entregue','ocorrencia','cancelado')),
    cliente_nome              varchar(150) NOT NULL,
    cliente_telefone          varchar(20),
    cliente_email             varchar(255),
    cep                       varchar(8),
    logradouro                varchar(150),
    numero                    varchar(10),
    complemento               varchar(100),
    bairro                    varchar(100),
    cidade                    varchar(100),
    uf                        varchar(2),
    referencia                varchar(255),           -- "portão azul"
    latitude                  numeric(9,6),
    longitude                 numeric(9,6),
    geocodificacao_status     varchar(20)  NOT NULL DEFAULT 'pendente'
                              CHECK (geocodificacao_status IN ('pendente','ok','revisar','falhou')),
    geocodificacao_confianca  numeric(3,2) CHECK (geocodificacao_confianca BETWEEN 0 AND 1),
    janela_inicio             timestamptz,
    janela_fim                timestamptz,
    peso_kg                   numeric(8,2) NOT NULL DEFAULT 0 CHECK (peso_kg >= 0),
    volumes                   integer      NOT NULL DEFAULT 1 CHECK (volumes > 0),
    token_rastreamento        varchar(64)  NOT NULL UNIQUE,   -- código aleatório do link do cliente
    token_expira_em           timestamptz,
    observacoes               text,                           -- "Portaria 24h..."
    dados_originais           jsonb,                          -- linha original da planilha
    entregue_em               timestamptz,
    criado_em                 timestamptz  NOT NULL DEFAULT now(),
    atualizado_em             timestamptz  NOT NULL DEFAULT now(),
    CONSTRAINT uq_pedidos_numero UNIQUE (empresa_id, numero_pedido),
    CONSTRAINT ck_pedidos_endereco
        CHECK (modalidade = 'retirada'
               OR (logradouro IS NOT NULL AND cidade IS NOT NULL AND uf IS NOT NULL)),
    CONSTRAINT ck_pedidos_janela
        CHECK (janela_inicio IS NULL OR janela_fim IS NULL OR janela_fim > janela_inicio),
    -- só sai de "novo" com coordenadas
    CONSTRAINT ck_pedidos_coordenadas
        CHECK (modalidade = 'retirada' OR status IN ('novo','cancelado')
               OR (latitude IS NOT NULL AND longitude IS NOT NULL))
);
CREATE UNIQUE INDEX ux_pedidos_externo ON pedidos (empresa_id, canal, id_externo)
    WHERE id_externo IS NOT NULL;
CREATE INDEX ix_pedidos_status ON pedidos (empresa_id, status, criado_em DESC);

-- Número sequencial por empresa (1, 2, 3...). A trava por empresa impede que
-- dois pedidos simultâneos peguem o mesmo número e não afeta outras lojas.
-- Exige o isolamento padrão do PostgreSQL (READ COMMITTED).
CREATE FUNCTION definir_numero_pedido() RETURNS trigger AS $$
BEGIN
    PERFORM pg_advisory_xact_lock(hashtextextended(NEW.empresa_id::text, 0));
    SELECT COALESCE(MAX(numero_pedido), 0) + 1
      INTO NEW.numero_pedido
      FROM pedidos
     WHERE empresa_id = NEW.empresa_id;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_pedidos_numero BEFORE INSERT ON pedidos
    FOR EACH ROW EXECUTE FUNCTION definir_numero_pedido();


-- =====================================================================
-- 5. ROTEIRIZAÇÃO E ENTREGA
-- =====================================================================

CREATE TABLE roteirizacoes (
    id              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id      uuid        NOT NULL REFERENCES empresas(id),
    origem_id       uuid        NOT NULL REFERENCES origens(id),
    solicitada_por  uuid        REFERENCES usuarios(id),        -- NULL quando automática
    objetivo        varchar(20) NOT NULL DEFAULT 'custo'
                    CHECK (objetivo IN ('custo','tempo','km','veiculos')),
    parametros      jsonb       NOT NULL DEFAULT '{}',          -- frota, jornada, restrições
    status          varchar(20) NOT NULL DEFAULT 'pendente'
                    CHECK (status IN ('pendente','processando','concluida','falhou')),
    resultado       jsonb,              -- km total, rota mais longa, custo, % nas janelas, pedidos de fora
    erro            text,
    iniciada_em     timestamptz,
    concluida_em    timestamptz,
    criado_em       timestamptz NOT NULL DEFAULT now()
);

-- O OR-Tools planeja por tipo de veículo; o entregador é definido no aceite da oferta.
CREATE TABLE rotas (
    id                      uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id              uuid          NOT NULL REFERENCES empresas(id),
    roteirizacao_id         uuid          NOT NULL REFERENCES roteirizacoes(id),
    numero                  smallint      NOT NULL,              -- exibido como R01, R02...
    regiao                  varchar(60),                         -- "Zona Oeste"
    tipo_veiculo            varchar(20)   NOT NULL
                            CHECK (tipo_veiculo IN ('bike','moto','carro','van','caminhao')),
    entregador_sugerido_id  uuid          REFERENCES entregadores(id),
    entregador_id           uuid          REFERENCES entregadores(id),
    veiculo_id              uuid          REFERENCES veiculos(id),
    status                  varchar(20)   NOT NULL DEFAULT 'proposta'
                            CHECK (status IN ('proposta','ofertada','aceita','em_andamento','concluida','cancelada')),
    cor                     varchar(7),                          -- hexadecimal, ex.: #E07B39
    data_operacao           date          NOT NULL,
    distancia_prevista_m    integer,
    duracao_prevista_s      integer,
    custo_estimado          numeric(10,2),
    trajeto                 text,                                -- polyline do OSRM
    coleta_prevista_em      timestamptz,
    valor_entregador        numeric(10,2),                       -- repasse ao entregador
    fim_previsto_em         timestamptz,
    atrasada                boolean       NOT NULL DEFAULT false,
    distancia_real_m        integer,
    iniciada_em             timestamptz,
    concluida_em            timestamptz,
    criado_em               timestamptz   NOT NULL DEFAULT now(),
    atualizado_em           timestamptz   NOT NULL DEFAULT now(),
    CONSTRAINT uq_rotas_numero UNIQUE (empresa_id, data_operacao, numero),
    CONSTRAINT ck_rotas_entregador
        CHECK (status IN ('proposta','ofertada','cancelada') OR entregador_id IS NOT NULL),
    CONSTRAINT ck_rotas_valor
        CHECK (status IN ('proposta','cancelada') OR valor_entregador IS NOT NULL)
);
CREATE INDEX ix_rotas_dia ON rotas (empresa_id, data_operacao, status);
CREATE INDEX ix_rotas_entregador ON rotas (entregador_id, status);

-- Favoritos primeiro; depois do tempo configurado, a rede
CREATE TABLE ofertas_rota (
    id             uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id     uuid        NOT NULL REFERENCES empresas(id),
    rota_id        uuid        NOT NULL REFERENCES rotas(id) ON DELETE CASCADE,
    entregador_id  uuid        NOT NULL REFERENCES entregadores(id),
    rodada         varchar(20) NOT NULL CHECK (rodada IN ('favoritos','rede')),
    status         varchar(20) NOT NULL DEFAULT 'enviada'
                   CHECK (status IN ('enviada','aceita','recusada','expirada','cancelada')),
    enviada_em     timestamptz NOT NULL DEFAULT now(),
    expira_em      timestamptz NOT NULL,
    respondida_em  timestamptz,
    UNIQUE (rota_id, entregador_id)
);
-- dois aceites ao mesmo tempo: só o primeiro vale
CREATE UNIQUE INDEX ux_ofertas_uma_aceita ON ofertas_rota (rota_id) WHERE status = 'aceita';
CREATE INDEX ix_ofertas_entregador ON ofertas_rota (entregador_id, status);

CREATE TABLE paradas (
    id                   uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id           uuid         NOT NULL REFERENCES empresas(id),
    rota_id              uuid         NOT NULL REFERENCES rotas(id) ON DELETE CASCADE,
    pedido_id            uuid         NOT NULL REFERENCES pedidos(id),
    sequencia            smallint     NOT NULL CHECK (sequencia > 0),   -- unicidade garantida no código
    status               varchar(20)  NOT NULL DEFAULT 'pendente'
                         CHECK (status IN ('pendente','a_caminho','entregue','falhou','cancelada')),
    chegada_prevista     timestamptz,
    chegada_real         timestamptz,
    concluida_em         timestamptz,
    comprovante_tipo     varchar(20)  CHECK (comprovante_tipo IN ('pin','foto','assinatura')),
    comprovante_chave    text,
    recebedor_nome       varchar(150),
    latitude_conclusao   numeric(9,6),          -- GPS no "Confirmar"
    longitude_conclusao  numeric(9,6),
    criado_em            timestamptz  NOT NULL DEFAULT now(),
    atualizado_em        timestamptz  NOT NULL DEFAULT now(),
    CONSTRAINT ck_paradas_comprovante CHECK (status <> 'entregue' OR comprovante_tipo IS NOT NULL)
);
CREATE INDEX ix_paradas_rota ON paradas (rota_id, sequencia);
-- um pedido só fica em uma parada ativa por vez
CREATE UNIQUE INDEX ux_paradas_pedido_ativo ON paradas (pedido_id)
    WHERE status IN ('pendente','a_caminho');

CREATE TABLE ocorrencias (
    id             uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id     uuid         NOT NULL REFERENCES empresas(id),
    parada_id      uuid         NOT NULL REFERENCES paradas(id),
    entregador_id  uuid         REFERENCES entregadores(id),
    tipo           varchar(30)  NOT NULL
                   CHECK (tipo IN ('cliente_ausente','endereco_nao_encontrado','recusado','avaria','outro')),
    descricao      text,
    foto_chave     text,
    latitude       numeric(9,6),                -- anexadas automaticamente
    longitude      numeric(9,6),
    status         varchar(20)  NOT NULL DEFAULT 'aberta' CHECK (status IN ('aberta','resolvida')),
    resolvida_por  uuid         REFERENCES usuarios(id),
    resolvida_em   timestamptz,
    registrada_em  timestamptz  NOT NULL DEFAULT now()
);
CREATE INDEX ix_ocorrencias_parada ON ocorrencias (parada_id);
CREATE INDEX ix_ocorrencias_abertas ON ocorrencias (empresa_id) WHERE status = 'aberta';

CREATE TABLE avaliacoes (
    id             uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id     uuid        NOT NULL REFERENCES empresas(id),
    entregador_id  uuid        NOT NULL REFERENCES entregadores(id),
    rota_id        uuid        NOT NULL UNIQUE REFERENCES rotas(id),   -- uma por rota
    nota           smallint    NOT NULL CHECK (nota BETWEEN 1 AND 5),
    comentario     text,
    avaliado_por   uuid        REFERENCES usuarios(id),
    criado_em      timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX ix_avaliacoes_entregador ON avaliacoes (entregador_id);

-- Histórico de cada pedido (grupo Pedidos; fica aqui porque referencia rotas).
-- Gravado na mesma transação que muda o pedido. Linhas nunca são alteradas.
CREATE TABLE pedido_eventos (
    id           uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id   uuid         NOT NULL REFERENCES empresas(id),
    pedido_id    uuid         NOT NULL REFERENCES pedidos(id),
    tipo_evento  varchar(50)  NOT NULL,          -- mesmo nome do evento no Kafka
    status_novo  varchar(20),                    -- status do pedido depois do evento, se mudou
    descricao    varchar(255) NOT NULL,          -- texto da linha do tempo
    autor_tipo   varchar(20)  NOT NULL CHECK (autor_tipo IN ('usuario','entregador','sistema')),
    autor_id     uuid,                           -- usuário ou entregador, conforme autor_tipo
    rota_id      uuid         REFERENCES rotas(id),
    dados        jsonb,
    ocorrido_em  timestamptz  NOT NULL DEFAULT now(),
    CONSTRAINT ck_pedido_eventos_autor CHECK (autor_tipo = 'sistema' OR autor_id IS NOT NULL)
);
CREATE INDEX ix_pedido_eventos_pedido ON pedido_eventos (pedido_id, ocorrido_em);

-- Chat loja ↔ entregador e suporte. A conversa é o par (empresa_id, entregador_id);
-- empresa_id NULL = chat de suporte, invisível para os lojistas.
CREATE TABLE mensagens (
    id                uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id        uuid        REFERENCES empresas(id),
    entregador_id     uuid        NOT NULL REFERENCES entregadores(id),
    rota_id           uuid        REFERENCES rotas(id),
    autor_tipo        varchar(20) NOT NULL CHECK (autor_tipo IN ('usuario','entregador','suporte','sistema')),
    autor_usuario_id  uuid        REFERENCES usuarios(id),
    texto             text        NOT NULL,
    enviada_em        timestamptz NOT NULL DEFAULT now(),
    lida_em           timestamptz,
    CONSTRAINT ck_mensagens_autor CHECK (autor_tipo <> 'usuario' OR autor_usuario_id IS NOT NULL)
);
CREATE INDEX ix_mensagens_conversa ON mensagens (empresa_id, entregador_id, enviada_em DESC);


-- =====================================================================
-- 6. APP DO ENTREGADOR (só a API do app usa)
-- =====================================================================

-- Chave "Fora do expediente / Iniciar jornada"
CREATE TABLE jornadas (
    id             uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    entregador_id  uuid        NOT NULL REFERENCES entregadores(id),
    veiculo_id     uuid        REFERENCES veiculos(id),
    iniciada_em    timestamptz NOT NULL DEFAULT now(),
    encerrada_em   timestamptz,
    distancia_m    integer,
    CONSTRAINT ck_jornadas_fim CHECK (encerrada_em IS NULL OR encerrada_em > iniciada_em)
);
-- no máximo uma jornada aberta por entregador
CREATE UNIQUE INDEX ux_jornadas_aberta ON jornadas (entregador_id) WHERE encerrada_em IS NULL;

-- Botão "Sacar"
CREATE TABLE saques (
    id                        uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    entregador_id             uuid          NOT NULL REFERENCES entregadores(id),
    valor                     numeric(12,2) NOT NULL CHECK (valor > 0),
    chave_pix                 varchar(100)  NOT NULL,       -- cópia no momento do saque
    status                    varchar(20)   NOT NULL DEFAULT 'solicitado'
                              CHECK (status IN ('solicitado','processando','pago','falhou')),
    gateway_transferencia_id  varchar(100),
    erro                      text,
    solicitado_em             timestamptz   NOT NULL DEFAULT now(),
    pago_em                   timestamptz
);

-- Livro-razão: saldo = soma dos lançamentos. Nunca alterar nem apagar linhas.
-- Ao sacar: travar o entregador (SELECT ... FOR UPDATE), conferir o saldo e
-- gravar saque + débito na mesma transação.
CREATE TABLE carteira_lancamentos (
    id             uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    entregador_id  uuid          NOT NULL REFERENCES entregadores(id),
    tipo           varchar(20)   NOT NULL
                   CHECK (tipo IN ('rota','bonus','ajuste','saque','estorno_saque')),
    valor          numeric(12,2) NOT NULL CHECK (valor <> 0),   -- positivo crédito, negativo débito
    rota_id        uuid          REFERENCES rotas(id),
    saque_id       uuid          REFERENCES saques(id),
    descricao      varchar(255),
    criado_em      timestamptz   NOT NULL DEFAULT now(),
    CONSTRAINT ck_lancamentos_rota  CHECK (tipo <> 'rota' OR (rota_id IS NOT NULL AND valor > 0)),
    CONSTRAINT ck_lancamentos_saque CHECK (tipo NOT IN ('saque','estorno_saque') OR saque_id IS NOT NULL)
);
-- cada rota é paga uma única vez
CREATE UNIQUE INDEX ux_lancamentos_rota ON carteira_lancamentos (rota_id) WHERE tipo = 'rota';
CREATE INDEX ix_lancamentos_entregador ON carteira_lancamentos (entregador_id, criado_em DESC);


-- =====================================================================
-- 7. RASTREAMENTO E SISTEMA
-- =====================================================================

-- Histórico de GPS, gravado em lote a partir do tópico "posicoes" do Kafka.
-- A posição atual fica no Redis. Um job diário apaga o que passou do prazo
-- de retenção. Sem chaves estrangeiras de propósito (alto volume).
CREATE TABLE posicoes_entregador (
    entregador_id   uuid         NOT NULL,
    registrada_em   timestamptz  NOT NULL,       -- horário do GPS no celular
    empresa_id      uuid,                        -- loja da rota em andamento; NULL fora de rota
    rota_id         uuid,
    latitude        numeric(9,6) NOT NULL,
    longitude       numeric(9,6) NOT NULL,
    precisao_m      numeric(6,1),
    velocidade_kmh  numeric(5,1),
    recebida_em     timestamptz  NOT NULL DEFAULT now(),
    -- chave natural: posição reenviada pelo app é ignorada (ON CONFLICT DO NOTHING)
    PRIMARY KEY (entregador_id, registrada_em)
);
CREATE INDEX ix_posicoes_rota ON posicoes_entregador (rota_id, registrada_em) WHERE rota_id IS NOT NULL;
CREATE INDEX ix_posicoes_data ON posicoes_entregador (registrada_em);   -- usado pelo job de limpeza

-- Outbox: o evento é gravado na mesma transação que muda o dado. Um serviço
-- em segundo plano (.NET BackgroundService) publica os pendentes no Kafka.
CREATE TABLE outbox (
    id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    topico        varchar(50) NOT NULL CHECK (topico IN ('pedido','roteirizacao','rota')),
    chave         varchar(50) NOT NULL,          -- id do pedido/rota: mantém a ordem no Kafka
    tipo_evento   varchar(50) NOT NULL,          -- PedidoCriado, RotaAceita, ParadaConcluida...
    payload       jsonb       NOT NULL,
    criado_em     timestamptz NOT NULL DEFAULT now(),
    publicado_em  timestamptz                    -- NULL = ainda não enviado
);
CREATE INDEX ix_outbox_pendentes ON outbox (criado_em) WHERE publicado_em IS NULL;

-- Idempotência: o Kafka pode entregar a mesma mensagem mais de uma vez
CREATE TABLE mensagens_processadas (
    consumidor     varchar(50) NOT NULL,         -- ex.: worker-geocodificacao
    mensagem_id    uuid        NOT NULL,
    processada_em  timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (consumidor, mensagem_id)
);


-- =====================================================================
-- 8. VIEW: O QUE O LOJISTA VÊ DA REDE
-- =====================================================================
-- Sem CPF, telefone, e-mail, endereço ou documentos. Os indicadores são
-- calculados aqui; a taxa de sucesso (entregas ÷ (entregas + falhas)) fica
-- no código. Falha = parada que falhou sem culpa do cliente.

CREATE VIEW vw_entregadores_rede AS
SELECT e.id,
       e.nome,
       e.foto_chave,
       v.tipo       AS veiculo_tipo,
       v.placa      AS veiculo_placa,
       v.verificado AS veiculo_verificado,
       EXISTS (SELECT 1
                 FROM documentos_entregador d
                WHERE d.entregador_id = e.id
                  AND d.tipo = 'cnh'
                  AND d.status = 'aprovado'
                  AND (d.validade IS NULL OR d.validade >= current_date)) AS cnh_verificada,
       (SELECT count(*)
          FROM paradas p
          JOIN rotas r ON r.id = p.rota_id
         WHERE r.entregador_id = e.id
           AND p.status = 'entregue') AS total_entregas,
       (SELECT count(*)
          FROM paradas p
          JOIN rotas r ON r.id = p.rota_id
         WHERE r.entregador_id = e.id
           AND p.status = 'falhou'
           AND NOT EXISTS (SELECT 1
                             FROM ocorrencias oc
                            WHERE oc.parada_id = p.id
                              AND oc.tipo IN ('cliente_ausente','endereco_nao_encontrado','recusado'))
       ) AS total_falhas,
       (SELECT round(avg(a.nota), 2)
          FROM avaliacoes a
         WHERE a.entregador_id = e.id) AS nota_media,
       (SELECT count(*)
          FROM avaliacoes a
         WHERE a.entregador_id = e.id) AS total_avaliacoes,
       (SELECT round(avg(extract(epoch FROM o.respondida_em - o.enviada_em)))
          FROM ofertas_rota o
         WHERE o.entregador_id = e.id
           AND o.status = 'aceita') AS tempo_aceite_medio_s
  FROM entregadores e
  LEFT JOIN veiculos v ON v.id = e.veiculo_atual_id
 WHERE e.status = 'ativo';

-- =====================================================================
-- 9. BACKOFFICE (equipe interna do Perg)
-- =====================================================================
-- Quem aprova cadastros e documentos de entregadores. Não pertence a nenhuma empresa.
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

-- Quem analisou cada documento e quem aprovou o cadastro
ALTER TABLE documentos_entregador ADD COLUMN analisado_por uuid REFERENCES administradores(id);
ALTER TABLE entregadores          ADD COLUMN aprovado_por  uuid REFERENCES administradores(id);
