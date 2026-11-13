# Banco de dados do Perg

PostgreSQL 15+, sem extensões. 31 tabelas + 1 view.

## Banco novo

1. No pgAdmin, crie o banco `perg`.
2. Abra a Query Tool **no banco `perg`** e rode `perg_modelo_banco.sql` inteiro.

Os planos e o primeiro administrador do backoffice são criados pela API na primeira execução.

## Banco que já existia

Rode, em ordem, só os arquivos de `migracoes/` com data posterior à criação do seu banco:

| Arquivo | O que acrescenta |
| --- | --- |
| `2026-11-11_backoffice.sql` | Tabela `administradores` e as colunas `analisado_por` / `aprovado_por` |
| `2026-11-13_integracao.sql` | Canal `api` nos pedidos, `chaves_api`, `webhooks` e `webhook_entregas` |

Na dúvida, apague o banco `perg` (botão direito → Delete (Force)), crie de novo e rode o script completo.

## Regra do projeto

O EF Core não gera o banco (sem migrations do EF). Toda mudança entra aqui:
atualize `perg_modelo_banco.sql` **e** crie um arquivo em `migracoes/` com a data, para quem já tem o banco.
