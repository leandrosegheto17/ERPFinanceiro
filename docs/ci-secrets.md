# Segredos de CI — GitHub Actions

- **Tarefa de origem**: TASK-005 (Lote 0 — Fundação e CI)
- **Regra que este documento serve**: GUARDRAILS.md G-10 / TASK.md DI-09 —
  nenhum segredo de serviço é gravado em código de cliente nem aparece no
  bundle publicado.

Este documento descreve **como** configurar os segredos de CI, nunca **quais
são os valores** — nenhum valor real é commitado neste repositório em
nenhuma circunstância. Os nomes abaixo espelham `.env.example`.

## 1. Onde configurar

GitHub → repositório → **Settings → Secrets and variables → Actions →
New repository secret**. Um segredo por linha da tabela abaixo. Não usar
"Variables" (não criptografadas) para nenhum item desta lista — todos são
segredo, não configuração pública.

| Nome do secret | Usado por | Nunca aparece em |
|---|---|---|
| `SUPABASE_ACCESS_TOKEN` | `supabase link`/`db push` em CI (deploy de migrations) | Bundle do cliente, logs de CI (mascarado automaticamente pelo GitHub Actions) |
| `SUPABASE_DB_URL` (ou `SUPABASE_DB_PASSWORD`, conforme o step) | Migrations/jobs server-side em CI | Bundle do cliente |
| `SUPABASE_SERVICE_ROLE_KEY` | Testes de integração server-side, `jobs/*` | Bundle do cliente, variável `VITE_*` |
| `VAPID_PRIVATE_KEY` | `jobs/reminder` (TASK-057), assinatura de Web Push | Bundle do cliente, variável `VITE_*` |

Variáveis que **podem** ser públicas (não são segredo, mas ainda documentadas
aqui para deixar claro o contraste) ficam como `VITE_SUPABASE_URL`,
`VITE_SUPABASE_ANON_KEY` e `VITE_VAPID_PUBLIC_KEY` — podem ir para
`vars` do ambiente de build ou até hardcoded em config não sensível, porque
a autorização real é RLS (server-side), não o sigilo dessas strings.

## 2. Como o workflow deve consumir (referência para TASK-002)

O pipeline de CI (`TASK-002`) injeta os secrets acima como variáveis de
ambiente do **step do runner** (`env:` no job), nunca como `VITE_*`:

```yaml
# .github/workflows/ci.yml (trecho ilustrativo — implementado na TASK-002)
- name: Rodar migrations contra o projeto remoto (deploy)
  env:
    SUPABASE_ACCESS_TOKEN: ${{ secrets.SUPABASE_ACCESS_TOKEN }}
    SUPABASE_DB_URL: ${{ secrets.SUPABASE_DB_URL }}
  run: supabase db push
```

Nunca:

```yaml
# ERRADO — nunca fazer isso: qualquer env com prefixo VITE_ acaba embutida
# em texto plano no bundle de produção pelo Vite.
env:
  VITE_SERVICE_ROLE_KEY: ${{ secrets.SUPABASE_SERVICE_ROLE_KEY }}
```

## 3. Ambiente local (desenvolvedor)

1. Copiar `.env.example` para `.env` (arquivo já ignorado pelo `.gitignore`
   raiz — nunca commitar `.env`).
2. Rodar `supabase start` (requer Docker Desktop). A CLI imprime
   `ANON_KEY`, `SERVICE_ROLE_KEY`, `API_URL`, `DB_URL` etc. — usar esses
   valores locais para preencher o `.env`, nunca os de produção.
3. `supabase status` reimprime os mesmos valores sem reiniciar os
   containers.
4. `supabase stop` encerra os containers ao final da sessão de trabalho.

Configuração de portas deste projeto (evita colisão quando há outro projeto
Supabase local rodando na mesma máquina): API `54421`, Postgres `54422`,
shadow DB `54420`, pooler `54429`, Studio `54423`, Inbucket/Mailpit `54424`,
Analytics `54427`, inspector de Edge Functions `8183` — ver
`supabase/config.toml`.

## 4. Verificação mecânica (não depende de revisão manual)

- `tests/security/bundle-secrets.test.mjs` (TASK-005): grep de padrões de
  segredo (chave de service role, chave VAPID privada, connection string com
  credencial) contra o diretório de build do cliente. Nesta tarefa o
  diretório `dist/` ainda não existe (nenhuma tarefa de build de app rodou
  ainda) — o teste cobre esse caso explicitamente, sem passar de forma vazia
  por omissão silenciosa (ver comentário no arquivo do teste).
- TASK-042 (Lote 7) estende o mesmo teste para rodar contra o bundle real de
  produção, como gate de release.
