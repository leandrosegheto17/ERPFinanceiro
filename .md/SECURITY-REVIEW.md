# SECURITY-REVIEW.md — App de Leitura Bíblica Guiada + Preparo de Estudo

- **Chapéu**: Validador (DevSecOps)
- **Lote auditado**: Lote 0 — Fundação e CI (TASK-001 a TASK-005)
- **Data**: 2026-09-07
- **Base**: `.md/SDD.md` §7 (Requisitos de Segurança e Compliance),
  `.md/GUARDRAILS.md` (Seção 3, G-14 a G-19; G-06 a G-13), `.md/TASK.md`
  (DI-09), `.md/QA-REPORT.md` (Aprovado — Lote 0, ver Seção 6/7)
- **Pré-condição verificada**: `QA-REPORT.md` aprovou o Lote 0 (5/5 tarefas)
  antes desta auditoria começar — não se audita build não validado
  funcionalmente.
- **Método**: toda checagem abaixo foi executada de forma independente neste
  ambiente (SAST manual/grep, `npm audit`, leitura direta de config e
  código), não a partir das notas de implementação do Executor.
- **Consumidores**: validador (ele mesmo, chapéu DevOps), gestor

---

## 1. Escopo desta Auditoria

Lote 0 é fundação/CI (scaffold, pipeline, PWA shell placeholder, setup de
teste, Supabase local) — **nenhuma** tabela de dado pessoal, autenticação,
RLS real ou CSP ainda existe neste lote (essas chegam nos Lotes 5/6, TASK-027
a TASK-042). O requisito de arquitetura do SDD.md §7 relevante e verificável
**já agora** é: G-10/DI-09 (segredo nunca no bundle/repo), higiene de
dependências (SAST/SCA), e os requisitos operacionais que o próprio chapéu
DevSecOps precisa deixar prontos para o chapéu DevOps (Seção 4). Requisitos
de §7.2/§7.5/§7.6 (RLS, CSP, headers) são **fora de escopo factual** deste
lote — não é omissão, é o que o SDD.md programou para lotes futuros, e será
auditado quando esses lotes forem concluídos.

## 2. Análise Estática (SAST) e Dependências de Terceiros (SCA)

| Checagem | Resultado |
|---|---|
| `npm audit` (produção, `--omit=dev`) | 0 vulnerabilidades |
| `npm audit` (completo, incl. devDependencies) | 0 vulnerabilidades |
| Superfície de dependências (`package.json`) | Mínima e coerente com o escopo do lote: `react`/`react-dom` em produção; `vite`, `vite-plugin-pwa`, `vitest`, `@playwright/test`, `dependency-cruiser`, `eslint`/`typescript-eslint` em dev. Nenhuma lib antecipada fora do escopo (Dexie/Radix ainda não entraram — corretamente adiados para Lote 3/4, conforme DI-15) |
| Grep por padrões de risco clássico (`dangerouslySetInnerHTML`, `eval(`, `innerHTML`, `document.write`) em `src/` | Nenhuma ocorrência — o único componente de app existente (`src/features/shell/App.tsx`) é um placeholder trivial sem manipulação de HTML dinâmico |
| SDK de analytics/anúncio/streak de terceiro (G-05, G-13, DI-15) | Nenhum presente em `package.json` |
| `.dependency-cruiser.cjs` (regra `no-features-from-core`, DI-02/G-07) | Regra ativa e comprovadamente bloqueante (rodada de QA injetou violação real e o lint falhou com exit 1) — mecanismo de arquitetura funcionando de fato, não é lint permissivo |

## 3. Exposição de Dados Sensíveis (segredos, bundle, repositório)

| Checagem | Resultado |
|---|---|
| `npm run test:security` (`tests/security/bundle-secrets.test.mjs`) contra `dist/` real gerado nesta sessão | **4/4 verdes**, incluindo a checagem contra o bundle real (não `skipped`) — nenhum segredo de service role/VAPID privada/connection string encontrado no `dist/` publicado |
| Detector (`scripts/security/scan-bundle-secrets.mjs`) — revisão de lógica | Cobre 3 classes reais: JWT com claim `role: service_role`, connection string Postgres com credencial embutida, valor literal de `SUPABASE_SERVICE_ROLE_KEY`/`VAPID_PRIVATE_KEY` via `process.env`. Testado positivo (aciona em fixture com segredo) e negativo (não aciona em bundle limpo) — não é apenas uma asserção vazia |
| `git ls-files` + `git grep` por `service_role`, `SUPABASE_SERVICE`, `VAPID_PRIVATE`, blocos `BEGIN PRIVATE KEY` no repositório rastreado | Nenhuma ocorrência de valor real — só documentação de skill (`.claude/skills/cloudflare-deploy/.../secrets-store/*.md`), que são páginas de referência de uma skill de terceiro, não segredo do projeto |
| `.gitignore` | `.env`, `.env.*` (exceto `.env.example`), `dist/`, `node_modules/`, `supabase/.branches`, `supabase/.temp` corretamente ignorados |
| `.env.example` | Só placeholders vazios; comentário explícito alertando que nenhuma variável `VITE_*` pode conter segredo de serviço (reforça DI-09/G-10 no próprio arquivo, para quem for editá-lo depois) |
| `docs/ci-secrets.md` | Documenta **onde** configurar (GitHub Actions Secrets), nunca valores reais; explicita o padrão errado (`VITE_SERVICE_ROLE_KEY`) como anti-exemplo |
| `supabase/config.toml` | Config padrão da CLI, sem credencial hardcoded; portas com offset documentado, sem exposição de rede além do padrão de dev local |
| `vite.config.ts` (manifest do PWA) | Nenhum dado sensível no manifest; `workbox.globPatterns` restrito a `**/*.{js,css,html}` — não pré-cacheia nenhuma resposta de API (coerente com §7.5, "proibido cachear resposta de API autenticada", ainda que não haja API alguma neste lote) |

Nenhuma exposição de dado sensível encontrada em logs, mensagem de erro,
armazenamento local ou payload de API — porque nenhuma dessas superfícies
ainda existe de fato neste lote (não há chamada de rede, não há
`localStorage` de conteúdo de usuário, não há mensagem de erro customizada
ainda). Registrado como escopo futuro (Seção 5), não como achado.

## 4. Validação de Requisitos de Segurança (SDD.md §7) Aplicáveis a Este Lote

| Requisito | Aplicável ao Lote 0? | Status |
|---|---|---|
| §7.1 Autenticação, §7.2 Autorização/RLS, §7.4 Isolamento multi-tenant | Não — chega no Lote 5/6 (TASK-027 a TASK-038) | N/A nesta rodada |
| §7.3 Criptografia (chaves VAPID/service role só em segredo de ambiente) | Sim, parcialmente — nenhuma chave real existe ainda, mas a **documentação e o mecanismo de detecção** que vão impedir a violação futura já precisam existir | Atende: `docs/ci-secrets.md` + `scan-bundle-secrets.mjs` + teste, todos prontos antes de qualquer chave real ser introduzida |
| §7.5 Superfície de exposição — CSP/HSTS/Permissions-Policy/Referrer-Policy | Não — é TASK-034 (Lote 5) | N/A nesta rodada, sem gap: o próprio SDD.md programa para depois |
| §7.5 Service Worker — "proibido cachear resposta de API autenticada" | Sim, verificável desde já | Atende: `globPatterns` restrito a app shell estático, nenhuma resposta de rede é interceptada/cacheada neste lote |
| §7.6 LGPD (consentimento, minimização, exclusão) | Não — chega no Lote 5/6/7 (TASK-027, TASK-032, TASK-039–041) | N/A nesta rodada |
| §7.7 Integridade de conteúdo (strings "Almeida Atualizada"/ARA/ARC) | Não — teste dedicado é TASK-011 (Lote 1); nenhum conteúdo editorial existe ainda neste lote | N/A nesta rodada — confirmado, via grep, que nenhuma dessas strings aparece em nenhum arquivo rastreado do repositório atual |
| GUARDRAILS G-10/DI-09 (segredo nunca no bundle) | Sim, diretamente | Atende — ver Seção 3 |
| GUARDRAILS G-11 (localStorage sem conteúdo de usuário) | Sim, verificável — ainda que não haja conteúdo de usuário no lote | Atende trivialmente: nenhum código deste lote grava em `localStorage` (grep confirmado, nenhuma ocorrência em `src/`) |

## 5. Requisitos de Segurança Operacional para o Chapéu DevOps

Definidos agora para o próprio chapéu DevOps aplicar quando a infraestrutura
de CI/CD/deploy for provisionada (ainda não provisionada neste lote — só o
workflow de CI existe, sem deploy real):

- **Gestão de secrets**: usar GitHub Actions Secrets para
  `SUPABASE_ACCESS_TOKEN`, `SUPABASE_DB_URL`/`SUPABASE_DB_PASSWORD`,
  `SUPABASE_SERVICE_ROLE_KEY`, `VAPID_PRIVATE_KEY` — nunca como `vars` nem
  como env `VITE_*`, conforme já documentado em `docs/ci-secrets.md` (que o
  próprio DevOps deve seguir ao cablear o pipeline real de deploy).
- **Antes do primeiro deploy real**: rodar `node
  scripts/security/scan-bundle-secrets.mjs dist` como gate de CI (hoje é só
  teste local via `npm run test:security`; TASK-042, Lote 7, formaliza isso
  como gate de release — o chapéu DevOps deve antecipar esse gate no pipeline
  de deploy assim que TASK-042 estiver concluída, não depois).
- **Headers de segurança** (CSP/HSTS/Permissions-Policy/Referrer-Policy,
  §7.5): configurar no provedor de hosting/CDN quando TASK-034 (Lote 5)
  entregar a especificação exata — não inventar valores antes disso.
- **Hardening de rede**: nenhuma rotina server-side (`jobs/*`) expõe endpoint
  HTTP público — reforçar isso na config de infraestrutura quando os
  primeiros `jobs/*` chegarem (Lote 7/11).

## 6. Classificação de Achados

Nenhum achado de severidade alta/crítica. Nenhum achado de compliance
obrigatório em aberto (LGPD/§7.6 ainda não é aplicável a este lote — não é
"aberto", é "ainda não chegou", tratado na Seção 4). Nenhum achado de
baixa/média severidade específico de segurança nesta rodada — o único débito
técnico do Lote 0 (REFAT-L0-01, timeout de teste sob carga) já está
registrado em `Refatoração Lote-0` pelo chapéu QA e não é um achado de
segurança.

**Nenhum achado com relevância estratégica a sinalizar ao Gestor nesta
rodada** — a superfície de segurança real do produto (auth, RLS, CSP, LGPD)
ainda não existe neste lote; será auditada novamente, em profundidade, nos
Lotes 5–7.

## 7. Veredito Geral do Lote 0 (chapéu DevSecOps)

**Lote 0: Aprovado, sem débito registrado.** Nenhum bloqueio de deploy.
Definition of Done (chapéu DevSecOps) satisfeita:

- [x] Nenhum achado de severidade alta/crítica em aberto
- [x] Todo achado de compliance obrigatório resolvido (nenhum aplicável ainda)
- [x] Todo achado de baixa/média severidade virou tarefa em `Refatoração
      Lote-X` com prazo (nenhum achado de segurança desta natureza nesta
      rodada — REFAT-L0-01 é do chapéu QA)
- [x] Requisitos de segurança operacional definidos para o chapéu DevOps
      (Seção 5)
- [x] Todo achado de relevância estratégica sinalizado ao Gestor (nenhum
      nesta rodada)

Combinado com a aprovação funcional já registrada em `.md/QA-REPORT.md`
(Seção 6/7 — Aprovado, 5/5 tarefas), o Lote 0 tem agora **dupla aprovação**
(QA + DevSecOps). O chapéu DevOps pode prosseguir com deploy do Lote 0,
sujeito à sua própria Definition of Done (rollback testado, observabilidade
ativa, infraestrutura validada contra requisitos não funcionais do SDD.md).

---

# Lote 1 — Núcleo: Corpus e Importação (TASK-006 a TASK-011)

- **Chapéu**: Validador (DevSecOps)
- **Lote auditado**: Lote 1 — Núcleo: Corpus e Importação (TASK-006 a TASK-011)
- **Data**: 2026-09-07
- **Base**: `.md/SDD.md` §7 (Requisitos de Segurança e Compliance),
  `.md/GUARDRAILS.md` (G-01, G-06, G-07, Seção 3 — G-14 a G-19),
  `.md/QA-REPORT.md` (Aprovado — Lote 1, ver Seção 6), `.md/TASK.md` Seção 3
  (Lote 1) e `Refatoração Lote-1`
- **Pré-condição verificada**: `QA-REPORT.md` aprovou o Lote 1 (6/6 tarefas)
  antes desta auditoria começar — não se audita build não validado
  funcionalmente.
- **Método**: toda checagem abaixo foi executada de forma independente neste
  ambiente (SAST manual/grep, `npm audit`, leitura direta de código e
  `tsconfig`, e uma injeção real de import `core/* → tools/*` seguida de
  reversão, para observar o comportamento real do pipeline em vez de inferir
  a partir da nota do Executor/QA) — não a partir das notas de implementação
  do Executor.
- **Consumidores**: validador (ele mesmo, chapéu DevOps), gestor

---

## 1. Escopo desta Auditoria

Lote 1 é núcleo de corpus/importação/reconhecimento de referência — **sem**
autenticação, RLS ou dado pessoal ainda (chegam nos Lotes 5/6). Requisitos
verificáveis **já agora**: G-01/DI-05 (integridade editorial — nome da
tradução nunca exibido), SAST/SCA sobre o código novo, exposição de segredo
em `tools/corpus-import` (que roda em Node com `node:crypto`), G-07/DI-02
(fronteira `core/*` × `tools/*`, com relevância de segurança porque `tools/*`
tem acesso a `node:crypto`/`node:fs`) e RNF-10/ADR-003 (integridade do corpus
como controle de dado, não só função de QA). Requisitos de §7.1–§7.2/§7.6
(auth, RLS, LGPD) seguem fora de escopo factual — programados para lotes
futuros.

## 2. Análise Estática (SAST) e Dependências de Terceiros (SCA)

| Checagem | Resultado |
|---|---|
| `npm audit` (produção, `--omit=dev`) | 0 vulnerabilidades |
| `npm audit` (completo, incl. devDependencies) | 0 vulnerabilidades |
| Grep por `eval(`, `dangerouslySetInnerHTML`, `innerHTML`, `document.write` em `src/` (repo inteiro, não só o código novo) | Nenhuma ocorrência |
| Superfície de dependências (`package.json`) | Sem mudança em relação ao Lote 0 — nenhuma lib nova introduzida por TASK-006 a TASK-011 (`node:crypto`/`node:fs` são módulos nativos do runtime Node do `tools/*`, não dependência de terceiro) |
| `npm run lint:deps` (regra `no-features-from-core`) | Limpo — `"no dependency violations found (35 modules, 61 dependencies cruised)"` |

## 3. Exposição de Dados Sensíveis (segredos, bundle, repositório)

| Checagem | Resultado |
|---|---|
| `npm run test:security` (`tests/security/*.test.mjs`) | **11/11 verdes** — as 4 checagens de segredo herdadas do Lote 0 continuam passando (nenhuma regressão) e as 7 checagens novas de `bundle-forbidden-strings` (TASK-011) passam, incluindo a checagem contra `dist/` real |
| Grep por `service_role`/`SUPABASE_SERVICE`/`VAPID_PRIVATE`/`BEGIN PRIVATE KEY`/`process.env`/`apiKey`/`secret` em `src/tools/corpus-import/` e `src/core/{corpus,reference}/` | Nenhuma ocorrência |
| Injeção manual de `"Almeida Atualizada"` em `dist/assets/index-*.js` real + rerun do teste | Falhou com `ERR_ASSERTION`, reason/match exatos reportados — confirma de forma independente (não a partir da nota do Executor) que o detector de TASK-011 funciona de fato contra o bundle publicado, não é asserção vazia. Rebuild restaurou 11/11 verde |
| Grep (case-insensitive) por "Almeida Atualizada"/"Almeida Revista e Atualizada" em todo o repositório rastreado (`git ls-files`) | Só aparece nos documentos de política/spec que **descrevem a proibição** (`GUARDRAILS.md`, `SDD.md`, `PRD.md`, `PRD-TECNICO.md`, `CTO-REVIEW.md`, `TASK.md`, `QA-REPORT.md`, `SECURITY-REVIEW.md`) e no próprio scanner/teste que **detecta** a string (`scripts/security/scan-bundle-forbidden-strings.mjs`, `tests/security/bundle-forbidden-strings.test.mjs`) — nenhuma ocorrência em conteúdo de produto, fixture de "conteúdo real" ou identificador de código. `.md/spikes/SPIKE-03-resultado.md` conferido à parte: nenhuma ocorrência |
| Grep por `\bARA\b`/`\bARC\b` (borda de palavra) em todo o repositório rastreado | Mesmo padrão acima — só em documentos de política e no scanner/teste que a detecta, nenhuma ocorrência em conteúdo/código de produto |
| `.env.example`, `.gitignore`, `docs/ci-secrets.md` | Sem mudança desde o Lote 0 (nenhuma tarefa deste lote toca segredo/config de ambiente) |

## 4. G-07/DI-02 — Fronteira `core/*` × `tools/*` (verificação independente, com evidência empírica)

Confirmei de forma independente, sem confiar na nota do Executor nem só na
nota do QA:

- **Leitura direta de imports**: `src/core/reference/*.ts` só importa de
  `../corpus/*` e de si mesmo; `src/core/corpus/*.ts` só importa de `./types`.
  Nenhum import de `tools/*`/`features/*` em nenhum dos dois módulos.
  `src/tools/corpus-import/*.ts` não importa de `core/*`/`features/*`.
- **`tsconfig.app.json`** (bundle do navegador) exclui explicitamente
  `src/tools/corpus-import/**/*.ts`; **`tsconfig.node.json`** é quem inclui
  esses arquivos (junto de `scripts/`, config files e testes) — reforça a
  separação de camada, mas **é proteção de type-check, não de bundling**.
- **Teste empírico real** (injeção temporária, revertida ao final — ver
  Seção "Método"): criei `src/core/corpus/tmp_devsecops_probe.ts` importando
  `CANONICAL_BOOKS` de `tools/corpus-import/canon.ts` (módulo "puro", sem
  `node:crypto`/`node:fs`) e referenciei esse import a partir de
  `src/core/placeholder.ts` → `App.tsx` (cadeia real até o entrypoint do
  bundle, não um arquivo órfão sem uso).
  - `npm run lint:deps`: **passou limpo** — a regra `no-features-from-core`
    não cobre `core/* → tools/*`, exatamente como o achado REFAT-L1-01
    descreve.
  - `npm run build` (`tsc -b && vite build`): **passou limpo, sem nenhum
    erro ou aviso**, e os dados de `canon.ts` (nomes de livros em português,
    ex. "Gênesis", "Malaquias") **apareceram de fato no bundle publicado**
    (`dist/assets/index-*.js`, confirmado por grep), com o bundle crescendo
    de 190,49 KB para 193,31 KB.
  - Um segundo teste, importando `createHash` de `node:crypto` diretamente
    (em vez de um módulo "puro" de `tools/corpus-import`), **foi pego pelo
    `tsc -b`** (erro `TS2591: Cannot find name 'node:crypto'`) — mas essa
    proteção é **incidental**: decorre de `tsconfig.app.json` não declarar
    `types: ["node"]`, não de uma regra desenhada para bloquear isso; deixa
    de funcionar se qualquer módulo de `core/*` legitimamente vier a
    precisar de outro tipo global de Node por outro motivo, ou se o import
    de `tools/*` não tocar um símbolo exclusivo de Node (como confirmado
    acima com `canon.ts`).
  - Após a observação, os dois arquivos de teste foram revertidos
    (`git status --short` confirmado limpo) e a suíte completa (lint,
    typecheck, 62 testes unitários, 11 testes de segurança, build) foi
    re-executada, toda verde, restaurando o bundle a 190,49 KB.

**Conclusão factual**: hoje, o estado real do repositório **não viola** a
fronteira `core/* → tools/*` (confirmado pela leitura direta). Mas a defesa
mecânica contra uma violação **futura** tem uma lacuna real e demonstrada,
não hipotética: um import "puro" de `tools/corpus-import` para dentro de
`core/*` passa por `lint:deps` **e** por `tsc -b`/`vite build` sem nenhum
erro, e o conteúdo correspondente vaza de fato para o bundle do cliente. Se
o módulo importado um dia tocar `node:crypto`/`node:fs` de forma direta
(ex. `build-manifest.ts`), a chance de pega-lo depende hoje de um efeito
colateral de configuração (ausência de `types: ["node"]`), não de uma regra
desenhada para isso.

## 5. RNF-10/ADR-003 — Integridade do Corpus como Controle de Dado

Confirmado por leitura direta de `verify-integrity.ts`
(`verifyCorpusIntegrity`): reconstrói as linhas do corpus canônico e compara
`!==` byte a byte contra o snapshot original, sem nenhuma normalização;
diverge em contagem de linha ou em qualquer caractere → lança
`CorpusIntegrityError`, interrompendo o pipeline (não loga e segue). Reexecutei
`npx vitest run src/tools/corpus-import/verify-integrity.test.ts` de forma
isolada (3/3 verdes) e confirmei que o caso 2 corrompe exatamente 1 caractere
preservando o comprimento da string (não é truncamento disfarçado). Do ponto
de vista de segurança, isto é, de fato, um controle de integridade de dado
efetivo — impede que um corpus corrompido/adulterado passe silenciosamente
para as camadas seguintes (`core/corpus`, `core/reference`, e eventualmente
a UI). **Atende.**

## 6. Validação de Requisitos de Segurança (SDD.md §7) Aplicáveis a Este Lote

| Requisito | Aplicável ao Lote 1? | Status |
|---|---|---|
| §7.1 Autenticação, §7.2 Autorização/RLS, §7.4 Isolamento multi-tenant, §7.6 LGPD | Não — chegam nos Lotes 5/6/7 | N/A nesta rodada |
| §7.3 Criptografia (SHA-256 do manifesto do corpus) | Sim | Atende: `buildManifest` usa `createHash("sha256")` real de `node:crypto` (TASK-008), validado contra schema por `validateManifestSchema` (rejeita hash malformado, `sha256` vazio) — confirmado por leitura direta, coerente com SDD §7.3 ("Corpus e notas... integridade garantida por SHA-256 no manifesto") |
| §7.5 Service Worker (nenhuma resposta de API autenticada cacheada) | Sim, ainda que trivialmente | Atende — nenhuma tarefa deste lote altera `workbox.globPatterns`, que segue restrito ao app shell estático (herdado do Lote 0) |
| §7.7 / GUARDRAILS G-01 Integridade de conteúdo editorial ("Almeida Atualizada"/ARA/ARC) | Sim, diretamente — primeiro lote com teste dedicado (TASK-011) | Atende — ver Seção 3, com evidência de injeção real e reversão |
| GUARDRAILS G-06/G-07 (regra de negócio única no cliente; `core/*` puro e sem import de `features/*`, dependência unidirecional verificada por lint) | Sim | Atende **parcialmente**: `core/*` está de fato puro e sem import de `tools/*`/`features/*` hoje (confirmado); mas a verificação mecânica por lint (que G-07 exige explicitamente: "verificado por lint no CI, não por revisão manual") **não cobre `core/* → tools/*`** — ver achado da Seção 7 |

## 7. Classificação de Achados

### Achado DEVSEC-L1-01 — Lacuna mecânica na fronteira `core/*` × `tools/*` (mesmo mecanismo do REFAT-L1-01 do QA, severidade reavaliada pelo chapéu DevSecOps)

- **Descrição**: `no-features-from-core` (`.dependency-cruiser.cjs`) não
  cobre `core/* → tools/*`. Demonstrado empiricamente (Seção 4) que um
  import de um módulo "puro" de `tools/corpus-import` para dentro de
  `core/*`, encadeado até o entrypoint real do app, passa por `lint:deps` e
  por `npm run build` sem nenhum erro, e o conteúdo importado aparece de
  fato no bundle publicado. A proteção que hoje impediria vazamento de
  `node:crypto`/`node:fs` especificamente (via `tsc -b` falhando por falta
  de tipos Node em `tsconfig.app.json`) é incidental, não desenhada para
  este propósito, e não cobre módulos "puros" de `tools/*` sem símbolo de
  Node.
- **Estado atual do código**: **sem violação real** — confirmado por leitura
  direta de todos os imports de `core/corpus`/`core/reference`/
  `tools/corpus-import` nesta rodada (Seção 4). Não há segredo, chave nem
  código server-only vazando no bundle publicado hoje (Seção 3, `npm run
  test:security` 11/11 verde contra `dist/` real).
- **Severidade**: **baixa/média** (reavaliada pelo chapéu DevSecOps a partir
  do achado original do QA — o QA classificou como baixa; concordo que não
  é alta/crítica, porque nada está exposto hoje, mas **elevo para o teto
  superior da faixa "baixa/média"** dado que a lacuna foi demonstrada de
  forma real, não hipotética, e o vetor concreto (`build-manifest.ts` já usa
  `node:crypto` hoje) já existe no mesmo diretório que ficaria desprotegido).
  **Não bloqueia deploy** — não há achado presente no build atual, só uma
  lacuna de defesa em profundidade contra um erro futuro.
- **Ação**: mesma ação já registrada pelo QA em `REFAT-L1-01`
  (`.md/TASK.md`, seção `Refatoração Lote-1`) — adicionar regra
  `no-tools-from-core` ao `.dependency-cruiser.cjs`. Este chapéu **não
  duplica a tarefa**: só reforça, com evidência própria, que o prazo deve
  ser tratado como "antes de qualquer nova tarefa em `core/*` deste lote em
  diante" (Lote 2 já tem `TASK-012`/`TASK-015` mexendo em `core/content`) em
  vez de "antes do fechamento do Lote 2" — ver nota de prazo abaixo.
- **Ajuste de prazo solicitado ao Coordenador/Executor** (não é redesenho de
  dependência/decomposição, é ajuste de janela de risco dentro da mesma
  tarefa já criada pelo QA): recomendo antecipar `REFAT-L1-01` para **antes
  de iniciar TASK-012** (primeira tarefa de `core/content` do Lote 2), não
  "antes do fechamento do Lote 2" — a lacuna protege justamente o próximo
  código que vai nascer em `core/*`. Registrado como recomendação de
  sequenciamento, não como bloqueio: a tarefa já existe, só a janela ideal
  muda. Não requer reabertura do `coordenador` (não é redesenho de
  dependência/decomposição — é a mesma tarefa, mesma posição na fila, só
  uma leitura diferente de "quando é seguro esperar").
- **Requisito de compliance obrigatório**: nenhum aplicável (G-07 é
  arquitetura/defesa em profundidade, não compliance regulatório).

Nenhum outro achado de severidade alta/crítica, média ou baixa nesta rodada.
Nenhum achado de compliance obrigatório em aberto (LGPD ainda não aplicável a
este lote).

**Nenhum achado com relevância estratégica a sinalizar ao Gestor nesta
rodada** — DEVSEC-L1-01 é achado técnico de defesa em profundidade, sem
violação real presente, dentro da autoridade normal do Validador para
classificar e prazo.

## 8. Requisitos de Segurança Operacional para o Chapéu DevOps

Sem alteração em relação ao já definido na Seção 5 do Lote 0 — nenhuma
tarefa deste lote toca infraestrutura, pipeline de deploy ou gestão de
segredo. Reforço um ponto específico deste lote: quando o chapéu DevOps
configurar o pipeline de build de produção, garantir que o step de build
rode `tsc -b && vite build` (não só `vite build` isolado) — foi exatamente
essa combinação que, incidentalmente, pegou o vazamento de `node:crypto` no
teste empírico da Seção 4; pular a etapa `tsc -b` removeria também essa
proteção acidental.

## 9. Veredito Geral do Lote 1 (chapéu DevSecOps)

**Lote 1: Aprovado, com 1 débito de severidade baixa/média registrado
(DEVSEC-L1-01, mesma tarefa que `REFAT-L1-01` do QA em
`.md/TASK.md`).** Nenhum bloqueio de deploy. Definition of Done (chapéu
DevSecOps) satisfeita:

- [x] Nenhum achado de severidade alta/crítica em aberto
- [x] Todo achado de compliance obrigatório resolvido (nenhum aplicável ainda)
- [x] Todo achado de baixa/média severidade virou tarefa em `Refatoração
      Lote-X` com prazo — `REFAT-L1-01` já existe (criada pelo QA); este
      chapéu reforça a evidência e recomenda antecipar o prazo (Seção 7),
      sem criar tarefa duplicada
- [x] Requisitos de segurança operacional definidos para o chapéu DevOps
      (Seção 8)
- [x] Todo achado de relevância estratégica sinalizado ao Gestor (nenhum
      nesta rodada)

Combinado com a aprovação funcional já registrada em `.md/QA-REPORT.md`
(Seção "Lote 1", Seção 6 — Aprovado, 6/6 tarefas), o Lote 1 tem agora
**dupla aprovação** (QA + DevSecOps). O chapéu DevOps pode prosseguir com
deploy do Lote 1 (junto do Lote 0, se ainda não implantado), sujeito à sua
própria Definition of Done (rollback testado, observabilidade ativa,
infraestrutura validada contra requisitos não funcionais do SDD.md) — débito
de baixa/média severidade registrado com prazo não pausa o deploy.

---

# Refatoração Lote-1 — Fechamento de DEVSEC-L1-01 (auditoria de segurança)

- **Data**: 2026-09-07
- **Base**: `.md/TASK.md` (seção "Refatoração Lote-1", REFAT-L1-01,
  `Concluída`), `.md/QA-REPORT.md` (seção "Refatoração Lote-1 — REFAT-L1-01",
  Aprovado), `.md/SECURITY-REVIEW.md` (seção "Lote 1", achado original
  DEVSEC-L1-01)
- **Pré-condição verificada**: `QA-REPORT.md` aprovou funcionalmente
  `Refatoração Lote-1`/REFAT-L1-01 antes desta auditoria começar.
- **Método**: toda checagem abaixo foi executada de forma independente nesta
  sessão (leitura direta de `.dependency-cruiser.cjs`, comando de CLI real,
  probe próprio — não o probe já usado e removido pelo Executor nem o
  reaproveitado pelo QA), não a partir das notas de implementação do Executor
  nem da evidência do chapéu QA.

## 1. Execução Independente — Evidência

| Comando | Resultado |
|---|---|
| Leitura direta de `.dependency-cruiser.cjs` | Regra `no-tools-from-core` presente, mesmo padrão estrutural de `no-features-from-core`: mesmo `from: { path: "(^\|[\\\\/])core[\\\\/]" }`, `to` trocado para `(^\|[\\\\/])tools[\\\\/]`, `severity: "error"`; comentário de cabeçalho do arquivo cita ADR-003 e as duas regras |
| Probe próprio e independente: criado `src/core/__devsecops_probe_devsec_l1_01.ts`, importando `CANONICAL_BOOK_IDS` (símbolo distinto do usado pelo Executor/`no-features-from-core` probe e do usado pelo QA) de `src/tools/corpus-import/canon.ts` | `npx depcruise src --config .dependency-cruiser.cjs` (comando direto, não via `npm run lint`) reportou `error no-tools-from-core: src/core/__devsecops_probe_devsec_l1_01.ts → src/tools/corpus-import/canon.ts`, com resumo `1 dependency violations (1 errors, 0 warnings). 36 modules, 62 dependencies cruised.` e **exit code 1** |
| Probe removido (`rm`) | `npm run lint` (eslint + `lint:deps`) voltou a `"no dependency violations found (35 modules, 61 dependencies cruised)"`, exit code 0 |
| `npm run build` (`tsc -b && vite build`) | Limpo — `dist/index.html`, `dist/manifest.json`, `dist/assets/index-*.js`, `dist/sw.js`, `dist/workbox-*.js`, `dist/registerSW.js` gerados sem erro; nenhum efeito colateral da nova regra de lint no build (esperado — é checagem estática, sem código de runtime novo) |
| `npx vitest run src/tools/dependency-rule.test.ts` (isolado) | 4/4 verdes — os 2 casos de `no-features-from-core` e os 2 casos novos de `no-tools-from-core` (`src/` real limpo + fixture `core -> tools` reprovada) |
| `npm run test:security` (SAST rápido de exposição de dado sensível no bundle, contra `dist/` real pós-build) | 11/11 verdes — nenhum segredo de serviço, nenhuma string proibida (DI-05) no bundle publicado |
| `git status --short` após a rodada completa | Confirma ausência de qualquer arquivo de probe remanescente — só as 4 edições legítimas já rastreadas (`.dependency-cruiser.cjs`, `.md/QA-REPORT.md`, `.md/TASK.md`, `src/tools/dependency-rule.test.ts`) aparecem como modificadas; nenhum arquivo novo/untracked |

## 2. Reavaliação do Achado DEVSEC-L1-01

- **Vetor original demonstrado nesta auditoria do Lote 1**: import "puro" de
  `tools/corpus-import` para dentro de `core/*`, encadeado até o entrypoint
  real do app, passava por `lint:deps` e por `npm run build` sem erro, com o
  conteúdo importado aparecendo no bundle publicado.
- **Estado hoje**: a mesma classe de import (`core/* → tools/*`, símbolo
  "puro" sem dependência de Node) agora é pega mecanicamente por
  `lint:deps`, confirmado por um probe próprio e independente desta rodada
  (Seção 1) — exit code 1, mensagem citando exatamente `no-tools-from-core`.
  A checagem não depende de `tsc -b` falhar incidentalmente por falta de
  tipos Node (a limitação original do achado): o probe usado aqui
  (`CANONICAL_BOOK_IDS`, um array de strings) não usa nenhum símbolo Node e
  ainda assim foi bloqueado.
- **Veredito**: o vetor concreto que originou DEVSEC-L1-01 está **fechado**.
  A lacuna mecânica na fronteira `core/*` × `tools/*` não existe mais —
  `core/* → tools/*` agora falha lint do mesmo jeito que `core/* →
  features/*` já falhava.
- **Nova exposição introduzida pela mudança**: nenhuma. É uma regra de lint
  em tempo de análise estática, sem código de runtime novo; `npm run build`
  permanece limpo e o bundle publicado (`dist/`) segue sem segredo nem
  string proibida (Seção 1, `test:security`). Nenhum arquivo de prova
  residual no repositório (`git status --short` limpo de untracked).
- **Compliance obrigatório**: nenhum aplicável (mesma nota da auditoria
  original do Lote 1 — G-07 é arquitetura/defesa em profundidade, não
  compliance regulatório).
- **Relevância estratégica ao Gestor**: nenhuma nesta rodada — fechamento de
  débito técnico já registrado, dentro da autoridade normal do Validador.

## 3. Veredito Geral do Lote Refatoração Lote-1 (chapéu DevSecOps)

**Refatoração Lote-1: Aprovado.** Nenhum achado de severidade alta/crítica
ou compliance obrigatório em aberto. DEVSEC-L1-01 está **fechado**, não
parcialmente — o vetor demonstrado na auditoria original não passa mais por
`lint:deps`, sem efeito colateral em build/bundle. Combinado com a aprovação
funcional já registrada em `.md/QA-REPORT.md` (seção "Refatoração Lote-1 —
REFAT-L1-01", Aprovado), o lote `Refatoração Lote-1` tem **dupla aprovação**
(QA + DevSecOps). O chapéu DevOps pode prosseguir com deploy, sujeito à sua
própria Definition of Done — não há débito de segurança pendente deste
achado para carregar adiante.

---

# Lote 2 — Núcleo: Conteúdo Editorial e Validação (TASK-012 a TASK-015)

- **Chapéu**: Validador (DevSecOps)
- **Lote auditado**: Lote 2 — Núcleo: Conteúdo Editorial e Validação
  (TASK-012 a TASK-015)
- **Data**: 2026-09-08
- **Base**: `.md/SDD.md` §7 (Requisitos de Segurança e Compliance),
  `.md/GUARDRAILS.md` (G-03, G-04, G-07), `.md/TASK.md` (Seção 3, Lote 2),
  `.md/QA-REPORT.md` (Aprovado — Lote 2, 4/4 tarefas, ver seção "Lote 2")
- **Pré-condição verificada**: `QA-REPORT.md` aprovou o Lote 2 (4/4 tarefas)
  antes desta auditoria começar — não se audita build não validado
  funcionalmente.
- **Método**: toda checagem abaixo foi executada de forma independente neste
  ambiente (SAST manual/grep, `npm audit`, leitura direta de código,
  `git diff` entre os commits do Lote 1 e do Lote 2 para `package.json`), não
  a partir das notas de implementação do Executor nem do `QA-REPORT.md`.
- **Consumidores**: validador (ele mesmo, chapéu DevOps), gestor

---

## 1. Escopo desta Auditoria

Lote 2 é código de build/CI (`tools/content-validate`, roda em Node, nunca no
bundle do cliente) e um módulo de runtime somente-leitura (`core/content`,
TypeScript puro, sem I/O de rede/DOM, sem `localStorage`, sem manipulação de
HTML). **Nenhuma** autenticação, RLS, CSP, tela de produto ou dado pessoal em
trânsito existe ainda neste lote — chegam nos Lotes 5/6/8/9. Requisitos
verificáveis **já agora**: dependências de terceiro (DI-15), exposição de
segredo/bundle (DI-05, herdado), G-04 (veto de build do validador de
conteúdo — já confirmado pelo chapéu QA, reforçado aqui do ângulo de
segurança: "veto real, não aviso" é também um controle de integridade de
dado), e G-07/DI-02 (fronteira `core/*` × `tools/*`, já reforçada por
`no-tools-from-core` desde `Refatoração Lote-1`). §7.1/§7.2/§7.4/§7.5/§7.6
(auth, RLS, CSP, LGPD) seguem fora de escopo factual — programados para lotes
futuros, mesma prática das auditorias anteriores.

## 2. Análise Estática (SAST) e Dependências de Terceiros (SCA)

| Checagem | Resultado |
|---|---|
| `npm audit` (produção, `--omit=dev`) | 0 vulnerabilidades |
| `npm audit` (completo, incl. devDependencies) | 0 vulnerabilidades |
| `git diff 5cd93a1 2fb45e7 -- package.json package-lock.json` (commit do Lote 1 → commit do Lote 2, verificação direta, não a nota do Executor) | **Diff vazio** — nenhuma dependência nova entrou no `package.json` no Lote 2, coerente com DI-15 (nenhum SDK de terceiro fora do permitido; TASK-012–015 são TypeScript puro sobre tipos/validação, sem necessidade de lib nova) |
| Grep por `eval(`, `dangerouslySetInnerHTML`, `innerHTML`, `document.write` em `src/core/content/` e `src/tools/content-validate/` | Nenhuma ocorrência — nenhum dos dois módulos renderiza ou manipula HTML (são tipos, lookup por `Map` e validação de dado estruturado) |
| `npm run lint:deps` (regra `no-tools-from-core`/`no-features-from-core`) | Limpo — `"no dependency violations found (109 modules, 261 dependencies cruised)"` |
| Leitura direta de imports (`src/core/content/get-plan-day.ts`, `src/core/content/types.ts`) | Só importam de `./types`/módulos irmãos — nenhum import de `tools/*`/`features/*` |
| Leitura direta de imports (`src/tools/content-validate/validate-content.ts`) | Importa de `../../core/content` (tipos) e de módulos irmãos (`range-utils`, `gospel-versification`) — direção `tools → core`, permitida por `no-tools-from-core` (só o sentido oposto é proibido) |

## 3. Exposição de Dados Sensíveis (segredos, bundle, repositório)

| Checagem | Resultado |
|---|---|
| `npm run test:security` (`tests/security/*.test.mjs`), executado nesta sessão contra `dist/` real | **11/11 verdes**, sem regressão — as 4 checagens de segredo e as 7 de string proibida (DI-05, herdadas dos Lotes 0/1) continuam passando |
| Grep por `process.env`, `localStorage`, `service_role`, `SUPABASE_SERVICE`, `VAPID_PRIVATE` em `src/core/content/` e `src/tools/content-validate/` | Nenhuma ocorrência — nenhum dos dois módulos toca variável de ambiente, armazenamento do navegador ou segredo de serviço |
| Leitura direta de `src/tools/content-validate/__fixtures__/valid-bundle.ts` | Fixture sintética de plano/perícope/nota (90 dias, texto de teste) — nenhum dado pessoal, nenhuma credencial, nenhum conteúdo real do acervo (coerente com RP-02 do `TASK.md`: acervo real ainda não existe, fixture é decisão documentada) |
| `package.json`/`package-lock.json`, `.env.example`, `.gitignore`, `docs/ci-secrets.md` | Sem mudança em relação ao Lote 1 (nenhuma tarefa deste lote toca segredo/config de ambiente) |

## 4. G-04 — Veto de Build do Validador de Conteúdo (ângulo de segurança/integridade de dado)

O chapéu QA já confirmou funcionalmente que `assertContentValid` lança
`ContentValidationError` (não loga e segue) quando `validateContent` reporta
`passed: false`. Do ângulo de segurança, isto é relevante porque §7.7 do
SDD.md trata integridade de conteúdo editorial como requisito de segurança
(dano reputacional/público, não só funcional) — e G-04 é o mecanismo que
impede um plano estruturalmente inválido (ex.: violação de C3, que poderia
expor um dia da janela de ativação sem Evangelho) de chegar ao build
publicado. Confirmado por leitura direta de `validate-content.ts`
(`assertContentValid`, linhas 573–582): lança sempre que `report.passed` for
`false`, sem caminho de bypass. **Atende.**

Não há geração/interpretação de HTML em nenhum dos dois módulos deste lote
(nota/perícope são strings de dado estruturado, não renderizadas aqui) — G-03
(sanitização de HTML de nota/esboço) permanece **fora de escopo factual**
deste lote: a primeira tarefa que renderiza `Note.body` de fato é de tela
(Lote 8/9, ex. TASK-044), não `core/content`/`tools/content-validate`.
Registrado como N/A nesta rodada, não como gap.

## 5. Validação de Requisitos de Segurança (SDD.md §7) Aplicáveis a Este Lote

| Requisito | Aplicável ao Lote 2? | Status |
|---|---|---|
| §7.1 Autenticação, §7.2 Autorização/RLS, §7.4 Isolamento multi-tenant, §7.6 LGPD | Não — chegam nos Lotes 5/6/7 | N/A nesta rodada |
| §7.3 Criptografia | Não diretamente — nenhuma chave/segredo é tocado por este lote; corpus/notas seguem "conteúdo público, integridade por SHA-256 no manifesto" (TASK-008, Lote 1), inalterado aqui | N/A nesta rodada |
| §7.5 Superfície de exposição (CSP/HSTS/Service Worker) | Não — nenhuma tarefa deste lote altera `vite.config.ts`/manifest/SW; confirmado por `npm run build` com tamanho de bundle inalterado frente ao Lote 1 (Seção 1 do `QA-REPORT.md`, Lote 2) | N/A nesta rodada |
| §7.7 Integridade de conteúdo (nunca altera texto de versículo; nenhum HTML injetado sem sanitizar; nunca exibe "Almeida Atualizada"/ARA/ARC) | Parcialmente — a parte "nunca altera texto de versículo" e "veto de build para conteúdo estruturalmente inválido" é diretamente relevante (ver Seção 4); a parte de sanitização de HTML e de string proibida no bundle segue coberta pelo teste herdado (Seção 3), sem mudança deste lote | Atende, na parte aplicável |
| GUARDRAILS G-04 (validador com poder de veto de build, nunca aviso) | Sim, diretamente | Atende — ver Seção 4 |
| GUARDRAILS G-07/DI-02 (fronteira `core/*` × `tools/*`, dependência unidirecional verificada por lint) | Sim | Atende — `tools/content-validate → core/content` é a direção permitida; nenhum import em sentido contrário; `no-tools-from-core` (fechada em `Refatoração Lote-1`) continua ativa e limpa (Seção 2) |

## 6. Requisitos de Segurança Operacional para o Chapéu DevOps

Sem alteração em relação ao já definido nas Seções 5/8 dos Lotes 0/1 —
nenhuma tarefa deste lote toca infraestrutura, pipeline de deploy ou gestão
de segredo. Reforço aplicável a partir de agora: quando o chapéu DevOps
formalizar o pipeline de build de conteúdo editorial (fora do escopo deste
lote, mas nascendo aqui), o step de build deve rodar `assertContentValid`
(ou o script de CLI equivalente) como gate que **falha o job**, não como
etapa informativa — coerente com G-04 confirmado na Seção 4, para que o
mecanismo de veto valha também em CI, não só em teste local.

## 7. Classificação de Achados

Nenhum achado de severidade alta/crítica. Nenhum achado de compliance
obrigatório em aberto (LGPD/§7.6 ainda não aplicável a este lote). Nenhum
achado novo de baixa/média severidade de segurança nesta rodada.

O achado de severidade **baixa** já registrado pelo chapéu QA no
`QA-REPORT.md` (Seção 5, Lote 2 — falso positivo possível de `checkC6` para
porção que termina em fronteira de capítulo comum, sem tabela de
versificação completa) foi revisado do ângulo de segurança: é um erro
conservador (falso positivo — bloqueia um plano potencialmente válido, nunca
deixa passar um inválido), portanto **não é uma lacuna de integridade de
dado** — ao contrário de DEVSEC-L1-01 (Lote 1), que era uma lacuna que
permitia um vazamento passar despercebido. Não requer registro duplicado
como achado de segurança; a tarefa de correção, se criada na checagem
estrutural, é suficiente como está classificada pelo QA.

**Nenhum achado com relevância estratégica a sinalizar ao Gestor nesta
rodada** — nenhuma superfície de segurança real do produto (auth, RLS, CSP,
LGPD, sanitização de HTML renderizado) existe ainda neste lote.

## 8. Veredito Geral do Lote 2 (chapéu DevSecOps)

**Lote 2: Aprovado, sem débito de segurança novo registrado.** Nenhum
bloqueio de deploy. Definition of Done (chapéu DevSecOps) satisfeita:

- [x] Nenhum achado de severidade alta/crítica em aberto
- [x] Todo achado de compliance obrigatório resolvido (nenhum aplicável ainda)
- [x] Todo achado de baixa/média severidade virou tarefa em `Refatoração
      Lote-X` com prazo (nenhum achado de segurança novo nesta rodada; o
      achado do QA sobre `checkC6` é funcional/conservador, não de
      segurança — ver Seção 7)
- [x] Requisitos de segurança operacional definidos para o chapéu DevOps
      (Seção 6)
- [x] Todo achado de relevância estratégica sinalizado ao Gestor (nenhum
      nesta rodada)

Combinado com a aprovação funcional já registrada em `.md/QA-REPORT.md`
(seção "Lote 2", Seção 7 — Aprovado, 4/4 tarefas), o Lote 2 tem agora **dupla
aprovação** (QA + DevSecOps). O chapéu DevOps pode prosseguir com deploy do
Lote 2 (junto dos Lotes 0/1, se ainda não implantados), sujeito à sua própria
Definition of Done (rollback testado, observabilidade ativa, infraestrutura
validada contra requisitos não funcionais do SDD.md).

---

# Lote 3 — Núcleo: Armazenamento e Sincronização (TASK-016 a TASK-022)

- **Chapéu**: Validador (DevSecOps)
- **Lote auditado**: Lote 3 — Núcleo: Armazenamento e Sincronização
  (TASK-016 a TASK-022)
- **Data**: 2026-09-08
- **Base**: `.md/SDD.md` §7 (Requisitos de Segurança e Compliance), §5.2/§5.4
  (schema Dexie, sincronização), `.md/GUARDRAILS.md` (G-07, G-11),
  `.md/TASK.md` (Seção 3, Lote 3), `.md/QA-REPORT.md` (Aprovado — Lote 3,
  7/7 tarefas, ver seção "Lote 3")
- **Pré-condição verificada**: `QA-REPORT.md` aprovou o Lote 3 (7/7 tarefas)
  antes desta auditoria começar — não se audita build não validado
  funcionalmente.
- **Método**: toda checagem abaixo foi executada de forma independente neste
  ambiente (SAST manual/grep, `npm audit`, leitura direta de código,
  `git diff` de `package.json` entre o commit do Lote 2 e o estado atual), não
  a partir das notas de implementação do Executor nem do `QA-REPORT.md`.
- **Consumidores**: validador (ele mesmo, chapéu DevOps), gestor

---

## 1. Escopo desta Auditoria

Lote 3 é a primeira camada de **armazenamento local persistente de dado do
usuário** do projeto (`core/storage`, IndexedDB via Dexie) e o primeiro
mecanismo de **sincronização** (`core/sync`, outbox/conciliação) — ainda
**sem** backend real (Supabase só chega no Lote 5/6) e sem autenticação/RLS.
Requisitos verificáveis **já agora**, com relevância direta e nova (não
herdada) neste lote: G-11/DI-10 (conteúdo do usuário só em IndexedDB, nunca
`localStorage` — primeira vez que o projeto tem, de fato, conteúdo do usuário
persistido localmente para testar isto contra), G-07/DI-02 (fronteira
`core/*` × `features/*`/`tools/*`, já reforçada desde `Refatoração Lote-1`),
SAST/SCA sobre o código novo, e ADR-006/ADR-007 como controles de integridade
de dado (expurgo nunca "no escuro", conciliação nunca perde progresso do
titular, relógio incorreto não corrompe LWW além do aceitável) — mecanismos
funcionais auditados pelo QA, revisitados aqui do ângulo de "o que aconteceria
se o dado fosse adulterado/perdido/exposto", não só "o teste passa".
§7.1/§7.2/§7.4/§7.6 (auth, RLS, isolamento multi-tenant, LGPD) seguem fora de
escopo factual — chegam nos Lotes 5/6/7.

## 2. Análise Estática (SAST) e Dependências de Terceiros (SCA)

| Checagem | Resultado |
|---|---|
| `npm audit` (produção, `--omit=dev`) | 0 vulnerabilidades |
| `npm audit` (completo, incl. devDependencies) | 0 vulnerabilidades |
| `git diff 2fb45e7 HEAD -- package.json` (commit do Lote 2 → estado atual) | Novas dependências de produção: `dexie` (`^4.4.5`, mandatada por DI-15/SDD §3 — não é escolha nova deste Executor) e, do Lote 4 em paralelo (fora de escopo desta auditoria), `@radix-ui/react-dialog`/`@radix-ui/react-toast`. Dev: `fake-indexeddb` (único símbolo real de IndexedDB fora do navegador, padrão do próprio ecossistema Dexie) e, do Lote 4, `@testing-library/*`/`jsdom` |
| Grep por `eval(`, `dangerouslySetInnerHTML`, `innerHTML`, `document.write` em `src/core/storage/` e `src/core/sync/` | Nenhuma ocorrência — os dois módulos são TypeScript puro sobre Dexie/`Map`, sem manipulação de HTML |
| `npm run lint:deps` (`no-features-from-core`/`no-tools-from-core`) | Limpo — `"no dependency violations found (109 modules, 261 dependencies cruised)"` |
| Leitura direta de imports (`src/core/storage/*.ts`, `src/core/sync/*.ts`) | Nenhum import de `features/*`/`tools/*` em nenhum dos dois módulos; `core/sync` só importa de `../storage/*` (direção `core → core`, permitida por DI-02) |

## 3. Exposição de Dados Sensíveis (segredos, bundle, repositório)

| Checagem | Resultado |
|---|---|
| `npm run test:security` (`tests/security/*.test.mjs`), executado nesta sessão contra `dist/` real | **11/11 verdes**, sem regressão |
| Grep (case-insensitive) por `process.env`, `service_role`, `SUPABASE_SERVICE`, `VAPID_PRIVATE`, `apiKey`, `secret` em `src/core/storage/` e `src/core/sync/` | Nenhuma ocorrência — nenhum dos dois módulos toca variável de ambiente ou segredo de serviço, coerente com a natureza puramente local (Dexie/IndexedDB) desta camada |
| Grep por `localStorage` em `src/core/storage/` e `src/core/sync/` | Nenhuma ocorrência real de uso — só menção em comentário de documentação explicando por que DI-10/G-11 são respeitados. **Achado cruzado, fora do código deste lote**: `src/main.tsx`/`src/design-tokens/theme.ts` (Lote 4, TASK-023) gravam a preferência de tema diretamente em `window.localStorage` — ver Seção 4 |
| Leitura direta de `src/core/storage/types.ts` | Nenhum tipo de registro carrega segredo de serviço; `PreferenceRecord`/`ProgressRecord`/`OutboxRecord` são dado do titular (preferência, progresso de leitura, mutação pendente) — dado pessoal de baixa sensibilidade, coerente com a natureza do produto (app de leitura, não dado financeiro/de saúde) |
| `package.json`/`.env.example`/`.gitignore`/`docs/ci-secrets.md` | Sem mudança relevante a segredo em relação ao Lote 2 |

## 4. G-11/DI-10 — Conteúdo do Usuário em IndexedDB, Nunca `localStorage` (achado cruzado)

**Dentro do escopo deste lote** (`core/storage`/`core/sync`, TASK-016 a
TASK-022): **atende integralmente**. Toda a superfície de dado do usuário
introduzida por este lote — progresso de leitura (`progress`/`anonProgress`),
preferências (`preferences`), fila de mutação pendente (`outbox`), eventos
(`eventQueue`) — vive exclusivamente em IndexedDB via Dexie. Confirmado por
grep (nenhuma ocorrência real de `localStorage` em nenhum dos dois módulos) e
por leitura direta de `db.ts`/`types.ts` (Seção 3).

**Achado cruzado, fora do código deste lote (não reprova nenhuma tarefa de
TASK-016 a TASK-022)**: durante a checagem de integração cruzada, confirmei
por leitura direta que `src/main.tsx` chama `initTheme({ ..., storage:
window.localStorage, ... })` (`src/design-tokens/theme.ts`, Lote 4,
TASK-023 — já `Concluída` no `TASK.md`, mas **ainda não auditada por este
chapéu**) para persistir a preferência de tema (`estudobiblico:theme-preference`)
diretamente em `localStorage`. Esse é exatamente o mesmo dado
(`PreferenceRecord.key === "theme"`) que este lote acabou de modelar para
viver em Dexie e sincronizar por LWW (TASK-020/021) — confirmado por
`db.test.ts` (Lote 3), que grava `preferences.put({ key: "theme", ... })`
como parte do próprio smoke test do schema.

- **Classificação preliminar deste chapéu** (a classificação definitiva cabe
  à auditoria do Lote 4, quando ele for submetido a `/validar`): risco real,
  mas **não presente no que este lote controla**. Duas fontes de verdade para
  a mesma preferência (`localStorage` vs. `preferences`/Dexie) significa que
  (a) o tema nunca converge entre dispositivos pela conciliação que TASK-020/
  021 acabaram de construir, e (b) há uma leitura textualmente defensável de
  DI-10/G-11 sendo violada ("localStorage nunca recebe conteúdo do usuário...
  conteúdo do usuário só em IndexedDB") — tema é preferência do usuário, e o
  próprio `SDD.md §5.2` já cita "tema" como exemplo de conteúdo de
  `preferences`.
- **Severidade preliminar**: baixa/média — não é exposição de dado sensível
  (tema não é dado pessoal de risco) nem falha de autenticação/autorização;
  é inconsistência de arquitetura entre dois lotes com efeito prático
  limitado a "tema não sincroniza entre dispositivos" e "leitura literal de
  G-11 tecnicamente violada por dado de baixo risco". **Não bloqueia o deploy
  deste lote** — o código de `core/storage`/`core/sync` (o que está sob
  validação aqui) está correto; o achado é do Lote 4.
- **Ação**: não é atribuição deste Validador criar uma tarefa em
  `Refatoração Lote-4` antes desse lote ter sido submetido a `/validar` —
  registrado aqui e no `QA-REPORT.md` (seção "Lote 3", Seção 3) como
  observação para a validação futura do Lote 4 confirmar/classificar
  formalmente contra DI-10/G-11 quando `TASK-023` for auditada. Não requer
  reabertura do `coordenador` (não é redesenho de dependência/decomposição) e
  não atinge o teto de "relevância estratégica" que justificaria sinalização
  imediata ao Gestor fora do ciclo normal de validação do Lote 4.

## 5. ADR-006/ADR-007 como Controles de Integridade de Dado

Do ângulo de segurança (não só funcional, já confirmado pelo QA), revisitei:

- **Expurgo (TASK-017, ADR-006)**: `purgeIfOverBudget` só remove
  `corpusChapters` (cache reconstituível), nunca `progress`/`preferences`/
  `outbox`/`eventQueue`/`anonProgress`/`contentBundle` — confirmado por
  leitura direta de `purge.ts`, nenhum caminho de código toca essas stores.
  Isto é relevante de segurança porque um expurgo mal desenhado que
  descartasse `outbox` silenciosamente destruiria mutação pendente do titular
  sem aviso — não é o caso aqui.
- **Conciliação (TASK-020, ADR-007)**: `reconcileProgress` é estritamente
  aditivo (união monotônica) — não há caminho de código que remova um dia já
  concluído. Isto é um controle de integridade real: impede que uma
  conciliação malformada apague progresso do titular.
- **RT-06 (TASK-021)**: o clamp de `effectivePreferenceTimestamp` nunca
  reescreve `updatedAt` no registro armazenado — confirmado por leitura
  direta (função não faz `record.updatedAt = ...` em nenhum lugar, só lê) —
  coerente com "nunca corrige dado do usuário em silêncio" (ADR-007), que é
  também um princípio de integridade de dado (o dispositivo do usuário
  mantém a verdade sobre o que ele escreveu, mesmo que o relógio dele esteja
  errado).
- **Idempotência (TASK-019)**: dedução por `id` gerado no cliente
  (`crypto.randomUUID()`, não sequencial/previsível) — não há vetor óbvio de
  colisão/replay malicioso relevante nesta fase sem autenticação real
  (qualquer client pode gerar qualquer `id`, mas não há ainda superfície de
  rede real nem RLS para um ataque ter efeito).

**Atende** — nenhum dos três mecanismos tem caminho de código que descarte,
sobrescreva ou corrompa dado do titular fora do que está declarado nos ADRs.

## 6. Validação de Requisitos de Segurança (SDD.md §7) Aplicáveis a Este Lote

| Requisito | Aplicável ao Lote 3? | Status |
|---|---|---|
| §7.1 Autenticação, §7.2 Autorização/RLS, §7.4 Isolamento multi-tenant, §7.6 LGPD | Não — chegam nos Lotes 5/6/7 (`hasSession`/`core/auth` ainda não existem; `sendPendingMutations` recebe `hasSession` como parâmetro explícito, documentado como lacuna esperada até lá) | N/A nesta rodada, sem gap: o próprio módulo documenta a integração futura |
| §7.3 Criptografia | Não diretamente — nenhuma chave/segredo tocado por este lote | N/A nesta rodada |
| §7.5 Superfície de exposição (CSP/HSTS/Service Worker) | Não — nenhuma tarefa deste lote altera `vite.config.ts`/manifest/SW; bundle inalterado (Seção 1 do `QA-REPORT.md`, Lote 3) | N/A nesta rodada |
| §7.6 LGPD (minimização de dado) | Parcialmente relevante — `PreferenceRecord`/`ProgressRecord`/`OutboxRecord` armazenam só o necessário (chave/valor de preferência, dia/data de conclusão, payload de mutação), nenhum campo supérfluo identificado por leitura direta de `types.ts` | Atende, na parte verificável sem LGPD formal ainda aplicável |
| GUARDRAILS G-07/DI-02 (fronteira `core/*` × `features/*`/`tools/*`) | Sim | Atende — Seção 2 |
| GUARDRAILS G-11/DI-10 (conteúdo do usuário só em IndexedDB) | Sim, diretamente — primeiro lote com conteúdo real do usuário persistido localmente | Atende **dentro do escopo deste lote**; achado cruzado do Lote 4 registrado na Seção 4, não deste lote |

## 7. Requisitos de Segurança Operacional para o Chapéu DevOps

Sem alteração em relação ao já definido nos Lotes 0/1/2 — nenhuma tarefa
deste lote toca infraestrutura, pipeline de deploy ou gestão de segredo.
Reforço específico deste lote: quando o chapéu DevOps/backend real (Lote 5/6)
implementar `SyncServerClient` de fato, garantir que o endpoint de
`sendBatch` valide `entityType`/`operation`/`payload` contra o schema
esperado no servidor (RLS por si só não impede um payload malformado de
tentar gravar) — este lote deixou a porta pronta para essa validação existir
do lado servidor, mas não a implementa (é `core/*` puro, sem backend ainda).

## 8. Classificação de Achados

Nenhum achado de severidade alta/crítica **dentro do escopo deste lote**.
Nenhum achado de compliance obrigatório em aberto (LGPD/§7.6 ainda não
formalmente aplicável). Nenhum achado novo de baixa/média severidade
**atribuível ao código de TASK-016 a TASK-022** nesta rodada — o achado
cruzado da Seção 4 (`localStorage` de tema, Lote 4/TASK-023) é preliminarmente
classificado como baixa/média, mas pertence ao Lote 4, não a este.

**Nenhum achado com relevância estratégica a sinalizar ao Gestor nesta
rodada** — o achado cruzado da Seção 4 é uma inconsistência técnica pontual
entre dois lotes recém-implementados em paralelo, dentro da autoridade normal
do Validador para registrar e direcionar à validação correta; não configura
padrão recorrente nem decisão de negócio/compliance.

## 9. Veredito Geral do Lote 3 (chapéu DevSecOps)

**Lote 3: Aprovado, sem débito de segurança novo atribuível a este lote.**
Nenhum bloqueio de deploy. Definition of Done (chapéu DevSecOps) satisfeita:

- [x] Nenhum achado de severidade alta/crítica em aberto
- [x] Todo achado de compliance obrigatório resolvido (nenhum aplicável ainda)
- [x] Todo achado de baixa/média severidade virou tarefa em `Refatoração
      Lote-X` com prazo (nenhum achado de segurança novo deste lote nesta
      rodada; o achado cruzado da Seção 4 pertence ao Lote 4 e será
      formalizado quando esse lote for auditado)
- [x] Requisitos de segurança operacional definidos para o chapéu DevOps
      (Seção 7)
- [x] Todo achado de relevância estratégica sinalizado ao Gestor (nenhum
      nesta rodada)

Combinado com a aprovação funcional já registrada em `.md/QA-REPORT.md`
(seção "Lote 3", Seção 6 — Aprovado, 7/7 tarefas), o Lote 3 tem agora **dupla
aprovação** (QA + DevSecOps). O chapéu DevOps pode prosseguir com deploy do
Lote 3 (junto dos Lotes 0/1/2, se ainda não implantados), sujeito à sua
própria Definition of Done (rollback testado, observabilidade ativa,
infraestrutura validada contra requisitos não funcionais do SDD.md). O
achado cruzado da Seção 4 não pausa este deploy — é um débito de baixa/média
severidade a resolver no ciclo de validação do Lote 4, não neste.

---

# Lote 4 — Núcleo: Design System (TASK-023 a TASK-026)

- **Chapéu**: Validador (DevSecOps)
- **Lote auditado**: Lote 4 — Núcleo: Design System (TASK-023 a TASK-026)
- **Data**: 2026-09-08
- **Base**: `.md/SDD.md` §5.2 (dado local do dispositivo), §7.3 (criptografia),
  `.md/GUARDRAILS.md` (G-07, G-11, G-13, G-05), `.md/TASK.md` (Seção 3,
  Lote 4, DI-07/DI-08/DI-10/DI-15), `.md/QA-REPORT.md` (Aprovado com ressalva
  — Lote 4, 4/4 tarefas, ver seção "Lote 4"), `.md/SECURITY-REVIEW.md` (seção
  "Lote 3", Seção 4 — achado cruzado preliminar sobre `localStorage`)
- **Pré-condição verificada**: `QA-REPORT.md` aprovou o Lote 4 (4/4 tarefas,
  1 com ressalva) antes desta auditoria começar — não se audita build não
  validado funcionalmente.
- **Método**: toda checagem abaixo foi executada de forma independente neste
  ambiente (SAST manual/grep, `npm audit`, leitura direta de código,
  `git diff` de `package.json`), não a partir das notas de implementação do
  Executor nem do `QA-REPORT.md`. Auditoria rodada em paralelo à auditoria do
  Lote 3 por outra instância do Validador, no mesmo checkout — nenhuma
  alteração de código feita por este Validador.
- **Consumidores**: validador (ele mesmo, chapéu DevOps), gestor

---

## 1. Escopo desta Auditoria

Lote 4 é puramente apresentação (`design-tokens/`, `design-system/`) — CSS
custom properties, componentes React de primitivo/overlay, nenhum dado de
rede, nenhuma chamada a backend, nenhum segredo. Requisitos verificáveis
diretamente: SAST/SCA sobre as 2 dependências novas de produção
(`@radix-ui/react-dialog`/`@radix-ui/react-toast`, mandatadas por DI-15),
G-11/DI-10 (o único ponto deste lote com gravação em armazenamento do
navegador é `theme.ts`/`main.tsx` — já sinalizado como achado cruzado
preliminar pela auditoria paralela do Lote 3, Seção 4 daquela seção, e
confirmado/formalizado aqui), DI-07/DI-08 como requisitos de acessibilidade
com peso de produto (não têm origem em §7, mas são guardrail obrigatório —
auditados aqui do ângulo "o mecanismo de verificação é automatizado e
confiável", complementando a validação funcional do QA). §7.1/§7.2/§7.4/§7.5/
§7.6 (auth, RLS, isolamento multi-tenant, CSP, LGPD) seguem fora de escopo
factual — nenhuma dessas superfícies existe neste lote.

## 2. Análise Estática (SAST) e Dependências de Terceiros (SCA)

| Checagem | Resultado |
|---|---|
| `npm audit` (produção, `--omit=dev`) | 0 vulnerabilidades |
| `npm audit` (completo, incl. devDependencies) | 0 vulnerabilidades |
| `git diff` de `package.json`/`package-lock.json` desde antes do Lote 4 | Únicas dependências de **produção** novas: `@radix-ui/react-dialog@^1.1.23`, `@radix-ui/react-toast@^1.2.23` — exatamente as mandatadas por DI-15 ("Radix Primitives, só em overlays"), nenhuma outra lib de produção introduzida (nenhum SDK de analytics/anúncio/streak — G-05/G-13 seguem satisfeitos). Dev: `@testing-library/jest-dom`/`@testing-library/react`/`@testing-library/user-event`/`jsdom` — tooling de teste padrão do ecossistema React, não vedado por DI-15 |
| `npm run lint:deps` | Limpo — `"no dependency violations found (109 modules, 261 dependencies cruised)"` — nenhuma violação de `no-features-from-core`/`no-tools-from-core` introduzida por `design-tokens/`/`design-system/` |
| Grep por `eval(`, `dangerouslySetInnerHTML`, `innerHTML`, `document.write` em `src/design-tokens/` e `src/design-system/` | Nenhuma ocorrência — os primitivos/overlays renderizam só `children`/props tipadas via JSX, nenhuma injeção de HTML dinâmico |
| Leitura direta do código-fonte instalado (`node_modules/@radix-ui/react-dialog@1.1.23`, `node_modules/@radix-ui/react-toast@1.2.23`) | Confirma os dois achados que o Executor já documentou (Dialog não seta `aria-modal` automaticamente; Toast não restaura foco automaticamente) — não são afirmações não verificadas, batem com o comportamento real do pacote instalado nesta sessão |

## 3. Exposição de Dados Sensíveis (segredos, bundle, repositório)

| Checagem | Resultado |
|---|---|
| `npm run test:security` (`node --test tests/security/*.test.mjs`) contra `dist/` real gerado nesta sessão | **11/11 verdes**, sem regressão — nenhum segredo/string proibida introduzido por este lote |
| Grep (case-insensitive) por `process.env`, `service_role`, `SUPABASE_SERVICE`, `VAPID_PRIVATE`, `apiKey`, `secret` em `src/design-tokens/` e `src/design-system/` | Nenhuma ocorrência |
| Grep por `localStorage` em `src/design-tokens/`, `src/design-system/`, `src/main.tsx` | **2 ocorrências reais**, ambas do mesmo mecanismo: `main.tsx` (`window.localStorage` passado a `initTheme`) e `theme.ts` (`ThemeStorage`/`getStoredPreference`/`setStoredPreference`) — ver Seção 4, formalização do achado cruzado já sinalizado pela auditoria paralela do Lote 3 |
| Leitura direta de `src/design-tokens/tokens.css`/`presentation-theme.css` | Nenhum dado sensível — só valores de cor/tipografia/espaçamento públicos, sem nenhuma referência a segredo ou endpoint |

## 4. G-11/DI-10 — Formalização do Achado Cruzado (`localStorage` de tema)

A auditoria paralela do Lote 3 (`.md/SECURITY-REVIEW.md`, seção "Lote 3",
Seção 4) já havia identificado e classificado preliminarmente este achado, de
fora do código deste lote, antes de TASK-023 ser formalmente auditada. Nesta
rodada, confirmo por leitura direta do próprio código de origem (`theme.ts`/
`main.tsx`, dentro do escopo real deste lote) e **formalizo** a classificação:

- **Fato confirmado**: `initTheme` (`src/design-tokens/theme.ts`), chamado em
  `src/main.tsx` com `storage: window.localStorage`, persiste a preferência
  de tema (`estudobiblico:theme-preference`, valor `"light"`/`"dark"`/
  `"system"`) diretamente em `localStorage`. `core/storage` (Lote 3,
  TASK-016) já define `PreferenceRecord` como o local canônico para esse
  exato dado (`SDD.md` §5.2: "`preferences` | Horário de lembrete, tema,
  `updatedAt` | Sim, LWW"), sincronizado por conciliação LWW (TASK-020/021,
  também Lote 3). `theme.ts` nunca lê/escreve em `core/storage`.
- **Requisito violado, na letra**: `GUARDRAILS.md` G-11 — "Nenhum dado do
  usuário é gravado em `localStorage`... `localStorage` só pode conter o
  necessário de sessão, nunca conteúdo do usuário." Preferência de tema é
  conteúdo do usuário (escolha pessoal, persistida, com intenção de
  sincronizar entre dispositivos conforme o próprio SDD.md), não sessão.
- **Análise de impacto real** (o que distingue este achado de um achado de
  confidencialidade genuíno): `SDD.md` §7.3 já declara que **dado local no
  dispositivo não é criptografado em nenhum caso** ("a proteção é o
  isolamento de origem do navegador e o bloqueio de tela do aparelho") —
  então não há diferença de exposição a um atacante com acesso ao
  dispositivo entre guardar `"dark"` em `localStorage` ou em IndexedDB; as
  duas APIs têm o mesmo isolamento de origem do navegador. O dano real e
  concreto é **funcional, não de confidencialidade**: a preferência de tema
  não participa da sincronização entre dispositivos que TASK-020/021
  acabaram de construir para "preferences" — um usuário que troca de tema no
  celular não verá a mudança no navegador do computador, ao contrário do que
  `SDD.md` §5.2 promete para "preferences" como categoria.
- **Compliance**: não há exigência de LGPD/§7.6 envolvida — tema não é dado
  pessoal sensível (art. 5º, II), e nenhum dado de outra pessoa é exposto.
- **Severidade: baixa-média, débito registrado, não bloqueia deploy.** Não é
  alta/crítica porque (a) não há exposição de dado sensível nem vazamento
  entre titulares, (b) não é falha de autenticação/autorização/RLS, (c) o
  dano concreto (tema não sincroniza) é de baixo impacto no produto (não é
  fluxo crítico, não há perda de dado do usuário, é reversível a qualquer
  momento reconfigurando o tema no novo dispositivo). É débito real, não
  cosmético, porque viola a letra de um guardrail inegociável (G-11) sem
  documentação de exceção — por isso vira tarefa com prazo, não só uma nota.
- **Ação**: confirmo a tarefa `REFAT-L4-01`, já criada pelo chapéu QA em
  `Refatoração Lote-4` (`.md/TASK.md`) durante o fechamento estrutural deste
  mesmo lote (ver `.md/QA-REPORT.md`, seção "Lote 4", Seção 5) — mesma
  tarefa, sem necessidade de uma segunda entrada duplicada de segurança;
  chapéu DevSecOps concorda com a severidade (baixa-média) e o prazo (antes
  de T-11 Configurações consumir/persistir a preferência de tema) já
  atribuídos pelo QA. Não escalo ao Gestor como bloqueio — ver Seção 7.

## 5. DI-07/DI-08 — Verificação do Mecanismo de Acessibilidade (ângulo DevSecOps)

Complementando a validação funcional do QA (que já confirmou caso a caso que
cada primitivo/overlay cumpre DI-07/DI-08), audito aqui só a **confiabilidade
do mecanismo de verificação em si** (DI-14: "testes de acessibilidade e de
contraste são automatizados... nunca inspeção manual isolada"):

- Alvo de toque (DI-08): `TOUCH_TARGET_MIN_STYLE` usa `var(--touch-target-min)`
  — uma única fonte de verdade (o token), não um valor duplicado por
  componente que pudesse divergir silenciosamente entre primitivos. Se o
  token mudar, todo consumidor muda junto — mecanismo estruturalmente correto
  contra deriva futura, não só correto hoje.
- Contraste (implícito em DI-07 quando a informação depende de cor visível):
  o teste de contraste (TASK-023/024) recalcula a razão a partir do CSS real
  a cada execução — não há valor de contraste hardcoded em nenhum teste que
  pudesse ficar desatualizado em relação ao CSS de produção.
- Nenhuma informação de estado só por cor (DI-07): confirmado por leitura
  direta que os 3 padrões usados (ícone `aria-hidden` + texto; `role="alert"`
  + `aria-describedby`; `aria-checked` + rótulo textual) não dependem de
  nenhuma propriedade CSS de cor para comunicar significado a tecnologia
  assistiva — testado via DOM/atributos, não via inspeção visual de cor.

**Atende.** Nenhum achado nesta seção.

## 6. Requisitos de Segurança Operacional para o Chapéu DevOps

Sem alteração em relação ao já definido nos Lotes 0/1/2/3 — nenhuma tarefa
deste lote toca infraestrutura, pipeline de deploy ou gestão de segredo.
Reforço específico deste lote: quando a CSP real for configurada (TASK-034,
Lote 5), confirmar que `style-src`/`font-src` continuam compatíveis com CSS
Modules + custom properties inline (`style` prop usada por
`TOUCH_TARGET_MIN_STYLE`) sem exigir `unsafe-inline` — `style` como atributo
inline de elemento (não `<style>`/`javascript:`) não é bloqueado por CSP
`style-src 'self'` padrão, mas vale confirmar explicitamente quando a CSP for
escrita, não assumir.

## 7. Classificação de Achados

Nenhum achado de severidade alta/crítica. Nenhum achado de compliance
obrigatório em aberto (nenhum aplicável a este lote). **1 achado de baixa-
média severidade, já registrado como débito com prazo** (`REFAT-L4-01`,
Seção 4) — formalização do achado cruzado já preliminarmente identificado
pela auditoria paralela do Lote 3.

**Nenhum achado com relevância estratégica a sinalizar ao Gestor nesta
rodada** — o achado de `localStorage` é uma inconsistência técnica pontual
entre dois lotes implementados em paralelo (Lote 3/Lote 4), com efeito prático
limitado e já dentro da autoridade normal do Validador para registrar como
débito com prazo; não configura padrão recorrente, não é decisão de negócio,
e não é achado de compliance/segurança que exija julgamento do Gestor.

## 8. Veredito Geral do Lote 4 (chapéu DevSecOps)

**Lote 4: Aprovado, com débito de segurança de baixa-média severidade
registrado (`REFAT-L4-01`, prazo definido).** Nenhum bloqueio de deploy.
Definition of Done (chapéu DevSecOps) satisfeita:

- [x] Nenhum achado de severidade alta/crítica em aberto
- [x] Todo achado de compliance obrigatório resolvido (nenhum aplicável ainda)
- [x] Todo achado de baixa/média severidade virou tarefa em `Refatoração
      Lote-4` com prazo (`REFAT-L4-01`, confirmado nesta rodada — ver Seção 4)
- [x] Requisitos de segurança operacional definidos para o chapéu DevOps
      (Seção 6)
- [x] Todo achado de relevância estratégica sinalizado ao Gestor (nenhum
      nesta rodada)

Combinado com a aprovação funcional já registrada em `.md/QA-REPORT.md`
(seção "Lote 4", Seção 6 — Aprovado com ressalva, 4/4 tarefas), o Lote 4 tem
agora **dupla aprovação** (QA + DevSecOps). O chapéu DevOps pode prosseguir
com deploy do Lote 4 (junto dos Lotes 0/1/2/3, se ainda não implantados),
sujeito à sua própria Definition of Done (rollback testado, observabilidade
ativa, infraestrutura validada contra requisitos não funcionais do SDD.md).
O débito `REFAT-L4-01` (severidade baixa-média, prazo definido) **não pausa
este deploy**, conforme a regra de que débito de baixa/média severidade com
prazo registrado segue normalmente para deploy.

---

# Lote 5 — Backend: Schema e Segurança (TASK-027 a TASK-032, TASK-034)

- **Chapéu**: Validador (DevSecOps)
- **Lote auditado**: as **7 tarefas já `Concluída`** do Lote 5 — TASK-027,
  TASK-028, TASK-029, TASK-030, TASK-031, TASK-032, TASK-034. **TASK-033**
  (teste de catálogo de RLS, RT-04/DI-12) segue `Não iniciada` — não faz
  parte desta auditoria; esta é a primeira rodada de backend real do
  projeto, então esta auditoria cobre §7.1/§7.2/§7.5/§7.6 de forma completa
  pela primeira vez.
- **Data**: 2026-09-08
- **Base**: `.md/SDD.md` §5.3 (modelo de dados), §7.1 (autenticação/
  gestão de segredo), §7.2 (RLS/autorização/isolamento por titular), §7.5
  (superfície de exposição), §7.6 (LGPD, RN-11), `.md/GUARDRAILS.md` (G-09,
  G-10, G-17, G-18), `.md/TASK.md` (Seção 3, Lote 5, DI-03/DI-08/DI-09/
  DI-15), `.md/QA-REPORT.md` (Aprovado com ressalva — Lote 5, 7/7 tarefas
  desta rodada, ver seção "Lote 5")
- **Pré-condição verificada**: `QA-REPORT.md` aprovou as 7 tarefas desta
  rodada do Lote 5 antes desta auditoria começar — não se audita build não
  validado funcionalmente. TASK-033 não foi auditada (não implementada).
- **Método**: toda checagem abaixo foi executada de forma independente
  neste ambiente — SAST manual/grep, `npm audit`, leitura direta das 6
  migrations SQL e do código de `src/tools/security/`, consultas SQL diretas
  contra o Postgres local real (`information_schema.role_table_grants`,
  `pg_default_acl`, `pg_class.relrowsecurity`, `pg_policies`) — não a partir
  das notas de implementação do Executor nem do `QA-REPORT.md`.
- **Consumidores**: validador (ele mesmo, chapéu DevOps), gestor

---

## 1. Escopo desta Auditoria

Primeiro lote de backend real do projeto — §7.1 (auth/segredo), §7.2 (RLS),
§7.5 (superfície de exposição/headers) e §7.6 (LGPD/RN-11) se aplicam pela
primeira vez de forma completa, não apenas parcial/preliminar como em lotes
anteriores. Verificáveis diretamente: as 9 tabelas criadas (RLS, política,
GRANT), a função `bump_metric` (`SECURITY DEFINER`), os 2 triggers
(`preference_set_received_at`, `set_erasure_request_due_at`), e os 5 headers
de segurança de TASK-034. §7.4 (isolamento multi-tenant) segue não
aplicável — produto é single-tenant por natureza (SDD §7.4). §7.3
(criptografia de dado local) não é tocado por este lote (é dado local do
Lote 3, não backend).

## 2. Análise Estática (SAST) e Dependências de Terceiros (SCA)

| Checagem | Resultado |
|---|---|
| `npm audit` (produção, `--omit=dev`) | 0 vulnerabilidades |
| `npm audit` (completo, incl. devDependencies) | 0 vulnerabilidades |
| `git diff` de `package.json`/`package-lock.json` desde antes do Lote 5 | Nenhuma dependência de produção nova — este lote é SQL (migrations) + `src/tools/security/` (TypeScript puro, sem dependência nova) |
| `npm run lint:deps` | Limpo — `"no dependency violations found (115 modules, 275 dependencies cruised)"`; `src/tools/security/` sem import de `core/*`/`features/*` fora do previsto |
| Grep por `eval(`, `dangerouslySetInnerHTML`, `innerHTML`, `document.write` em `src/tools/security/` | Nenhuma ocorrência |
| Leitura direta das 6 migrations SQL, procurando SQL dinâmico/concatenação de string em função `plpgsql` | Nenhuma — `bump_metric`, `preference_set_received_at` e `set_erasure_request_due_at` usam apenas comandos estáticos parametrizados (`insert ... values ($1, $2, ...)` via placeholder de função, `new.<coluna> := ...`); nenhum `EXECUTE`/`format()` que pudesse abrir SQL injection via `plpgsql` dinâmico |
| `bump_metric`: `SECURITY DEFINER` + `set search_path = public` | Confirmado — `search_path` fixo é a mitigação padrão contra o vetor clássico de sequestro de `search_path` em função `SECURITY DEFINER` (um chamador malicioso não pode redirecionar `public.analytics_daily_aggregate` para uma tabela de outro schema criada por ele) |

## 3. Exposição de Dados Sensíveis (segredos, bundle, repositório)

| Checagem | Resultado |
|---|---|
| `npm run test:security` (`node --test tests/security/*.test.mjs`) contra `dist/` real gerado nesta sessão | **11/11 verdes**, sem regressão |
| Grep (case-insensitive) por `process.env`, `service_role`, `SUPABASE_SERVICE`, `VAPID_PRIVATE`, `apiKey`, `secret` em `supabase/migrations/` e `src/tools/security/` | Nenhuma ocorrência de segredo hardcoded — `service_role` aparece só como **nome de papel de banco** em GRANT/comentário (`grant ... to service_role`), nunca como valor de credencial |
| `tests/db/*.test.mjs`: fonte das credenciais usadas nos testes | Confirmado (leitura direta) que usam as chaves de demonstração públicas que o próprio `supabase start` imprime para ambiente local (já documentadas como "shared defaults" em `docs/ci-secrets.md`, Lote 0) — não são segredo de produção |
| `analytics_daily_aggregate`: coluna de sujeito | Confirmado por leitura do `create table` — só `date`/`metric`/`count`, nenhuma coluna de `user_id`/sessão/dispositivo/IP/user-agent (CA-09.5, M-06) |
| `analytics_event`/demais tabelas do titular: payload/coluna sensível não prevista | `payload jsonb` de `analytics_event` é campo livre controlado pelo cliente instrumentado (`core/telemetry`, ainda não implementado — Lote 12), fora do escopo de schema deste lote; nenhuma coluna do schema em si armazena segredo |

## 4. Validação de Requisitos de Segurança (SDD.md §7) Aplicáveis a Este Lote

### 4.1 §7.2 — RLS habilitada e isolando de fato (não apenas existindo)

Confirmado via 2 vias independentes desta rodada (não repetindo a validação
funcional do QA, auditando do ângulo de segurança — presunção de
funcionamento não é aceitável, mesmo com `QA-REPORT.md` já aprovado):

| Tabela | RLS habilitada (DI-03) | Nº de políticas | GRANT `anon` | GRANT `authenticated` | GRANT `service_role` (além do default de plataforma) |
|---|---|---|---|---|---|
| `profile` | Sim, mesma migration | 3 | Nenhum | select, insert, update | Nenhum |
| `consent` | Sim, mesma migration | 3 | Nenhum | select, insert, update | Nenhum |
| `plan_progress` | Sim, mesma migration | 2 | Nenhum | select, insert (append-only) | Nenhum |
| `preference` | Sim, mesma migration | 4 | Nenhum | select, insert, update, delete | Nenhum |
| `push_subscription` | Sim, mesma migration | 4 | Nenhum | select, insert, update, delete | Nenhum |
| `reminder_log` | Sim, mesma migration | 1 | Nenhum | select (só leitura) | select, insert, update |
| `analytics_event` | Sim, mesma migration | 2 | Nenhum | select, insert (imutável) | Nenhum |
| `analytics_daily_aggregate` | Sim, mesma migration | **0** | Nenhum | Nenhum | select (só apuração) |
| `erasure_request` | Sim, mesma migration | 2 | Nenhum | select, insert | select, update |

Tabela construída a partir de consulta SQL direta contra o Postgres local
(`pg_class.relrowsecurity`, `pg_policies`, `information_schema.role_table_grants`)
nesta mesma rodada — não transcrita dos comentários das migrations. **Nenhuma
tabela do lote concede a `anon` qualquer privilégio de SELECT/INSERT/UPDATE/
DELETE**; `authenticated` só tem exatamente as operações que cada tabela
pretende, sem excesso (confirmado tabela por tabela: nenhum UPDATE/DELETE em
`plan_progress`/`analytics_event` — append-only real, não só de nome; nenhum
UPDATE/DELETE em `erasure_request`/`reminder_log` para `authenticated` —
escrita de status é exclusiva de serviço).

**Achado de ambiente, não de código (documentado, não uma falha de
configuração deste lote)**: `anon`/`authenticated`/`service_role` têm
`TRUNCATE`/`TRIGGER`/`REFERENCES` em todas as 9 tabelas (confirmado por
consulta a `pg_default_acl`: `defaclrole` correspondente ao papel de
migração `postgres`, ACL `Dxtm` para os 3 papéis de API, em todo o schema
`public`) — este é um `ALTER DEFAULT PRIVILEGES` de **plataforma do próprio
Supabase** (idêntico em qualquer projeto Supabase, local ou hospedado, não
introduzido nem removível por nenhuma das 6 migrations deste lote). Análise
de risco: `TRUNCATE` não é exposto por nenhum verbo HTTP do PostgREST (a API
REST gerada só expõe SELECT/INSERT/UPDATE/DELETE via GET/POST/PATCH/
DELETE) — para um chamador de `anon`/`authenticated` explorar esse
privilégio, precisaria de conexão SQL direta ao Postgres com essas
credenciais de papel, não apenas a chave de API pública, e a porta do
Postgres não deve estar publicamente exposta em produção (requisito
operacional, ver Seção 6). **Não é um achado acionável de código deste
lote** — registrado aqui como nota de risco residual de plataforma para o
chapéu DevOps confirmar que a porta 5432/6543 do Postgres gerenciado não
tem exposição pública direta no provisionamento real (TASK-005 é só local).

### 4.2 §7.5 — Superfície de exposição / headers (TASK-034)

Os 5 headers (CSP, HSTS, Referrer-Policy, Permissions-Policy,
X-Content-Type-Options) confirmados por leitura direta de
`src/tools/security/securityHeaders.ts` com os valores exatos exigidos —
sem repetir o detalhamento já feito pelo QA (`QA-REPORT.md`, seção "Lote 5",
TASK-034), auditando aqui do ângulo de segurança:

- **`script-src 'self'` sem `unsafe-inline`/`unsafe-eval`**: confirmado —
  este é o controle real contra XSS via injeção de script, e não há exceção.
- **`style-src 'self' 'unsafe-inline'`**: concessão avaliada e aceita —
  risco real de `unsafe-inline` em `style-src` é limitado a manipulação
  visual (não execução de código arbitrário), e a alternativa (nonce/hash)
  exigiria infraestrutura de CSS-in-JS com nonce que o projeto não tem;
  confirmado que a concessão **não vaza** para `script-src` (checado por
  teste dedicado e por leitura direta, Seção 2 do QA-REPORT).
- **`connect-src`**: único destino externo é `https://*.supabase.co`
  (wildcard de subdomínio, decisão temporária até o projeto de produção ser
  provisionado) — cumpre G-17 (nenhum destino de terceiro real: é o próprio
  backend do produto). Reforço operacional necessário antes do go-live: o
  chapéu DevOps deve trocar o wildcard pelo domínio exato do projeto
  Supabase de produção assim que provisionado (já registrado como nota no
  próprio código-fonte, TASK-034) — não é um achado de severidade alta
  porque o wildcard já restringe a um único provedor conhecido (não é
  `*` genérico), mas é um item de configuração a fechar antes de produção.
- HSTS/Referrer-Policy/Permissions-Policy: valores exatos conferem com o
  mínimo de §7.5, sem divergência.

**Achado real, severidade baixa-média — teto de inserção por usuário/dia
ausente (§7.5, linha "API de dados")**: confirmado por leitura direta das 6
migrations que nenhuma implementa "teto de inserção de eventos por usuário
por dia, aplicado por gatilho no banco", requisito literal de `SDD.md` §7.5
para a superfície "API de dados". `analytics_event` é a tabela mais exposta
a esse risco (evento identificado, gravável por qualquer `authenticated`
sem limite de volume) — sem o teto, um titular autenticado (ou uma conta
comprometida) pode inserir volume arbitrário de eventos, gerando custo de
armazenamento e possível degradação de performance da apuração agregada;
não é uma falha de confidencialidade/RLS (isolamento por titular continua
correto — um usuário não acessa dado de outro), é um risco de abuso/
disponibilidade. Mesmo achado já identificado e classificado pelo chapéu QA
(`QA-REPORT.md`, seção "Lote 5", TASK-031) — **confirmo a classificação**
(baixa-média, não bloqueia deploy) e a tarefa `REFAT-L5-01`, já criada pelo
QA em `Refatoração Lote-5`, sem necessidade de uma segunda entrada
duplicada. Prazo mantido: antes do go-live de produção.

### 4.3 §7.6 — LGPD / RN-11 (`erasure_request`)

- Base legal de consentimento (RNF-05, CA-10.2): `consent` com `version`/
  `text_sha256`/`accepted_at`/`revoked_at`, confirmado presente. Nota: "nenhuma
  escrita de dado pessoal no servidor ocorre antes de existir a linha de
  consentimento" (§7.6) — este requisito é de **ordem de chamadas do
  cliente** (`features/identity`, ainda não implementado, Lote 6), não do
  schema em si; o schema deste lote não impede nem garante essa ordem por
  constraint de banco (não há FK de `analytics_event`/`profile` para
  `consent` exigindo uma linha de consentimento prévia) — **fora do escopo
  de correção deste lote** (é responsabilidade de `features/identity`,
  TASK-035+), mas registrado aqui como item de atenção para a auditoria do
  Lote 6, que deve confirmar que o cliente de fato respeita essa ordem antes
  de qualquer escrita de dado pessoal.
- Exclusão em ≤15 dias (RN-11): `due_at = requested_at + 15 dias` confirmado
  calculado por trigger no servidor, não aceito do cliente (Seção 2 do
  QA-REPORT, TASK-032) — cumpre a letra do requisito de arquitetura. A
  execução da cascata em si (`jobs/erasure`) é TASK-041, fora deste lote;
  este lote só garante que o pedido é registrado corretamente e que
  `due_at` não é manipulável — auditoria da cascata completa fica para
  quando TASK-041 existir.
- `unique (user_id)` em `erasure_request`: confirmado como controle de
  integridade que impede múltiplos pedidos simultâneos para o mesmo titular
  — coerente com a cardinalidade do ER de §5.3.
- Minimização (§7.6): nenhuma coluna de IP/user-agent/localização/contato/
  identificador de dispositivo estável em nenhuma das 9 tabelas — confirmado
  por leitura direta de todas as 6 migrations.
- Agregado anônimo (§7.6, "Anonimato antes do consentimento"): confirmado
  na Seção 4.1 acima — `analytics_daily_aggregate` sem coluna de sujeito e
  sem GRANT de leitura para `anon`/`authenticated`.

**Nenhum achado de compliance obrigatório em aberto para este lote** — os
dois pontos de atenção acima (ordem de escrita vs. consentimento; cascata
completa de exclusão) são responsabilidade de tarefas futuras (Lote 6,
TASK-041), não deste lote, e já estão registrados aqui para a auditoria
correspondente não perder o fio.

## 5. GRANTs — Checagem Redobrada (item explícito do escopo desta auditoria)

Verificação linha a linha de `information_schema.role_table_grants` contra
a intenção de cada tabela (Seção 4.1), com atenção específica a excesso de
privilégio:

- **Nenhum GRANT a `anon`** em nenhuma das 9 tabelas, em nenhuma operação —
  confirmado. O único ponto de acesso de `anon` a qualquer dado deste lote é
  `EXECUTE` em `bump_metric` (RPC, não tabela), coerente com o soft gate
  pré-consentimento (M-06) e a vitrine (M-04) precisarem funcionar sem conta.
- **Nenhum GRANT de UPDATE/DELETE a `authenticated`** em tabela append-only
  (`plan_progress`, `analytics_event`) ou de escrita exclusiva de serviço
  (`reminder_log`, `erasure_request` além de select/insert) — confirmado
  ausente em todos os 4 casos, nas duas camadas (GRANT de tabela **e**
  ausência de política RLS equivalente, redundância documentada e
  confirmada correta).
- **`service_role` só tem privilégio explícito onde precisa**: `reminder_log`
  (select/insert/update, para TASK-057 gravar desfecho de envio),
  `erasure_request` (select/update, para TASK-041 preencher `completed_at`),
  `analytics_daily_aggregate` (select, só para apuração/teste, nunca usado
  pelo bundle do cliente — GUARDRAILS G-10 proíbe expor essa credencial no
  cliente, confirmado sem ocorrência de `service_role`/chave associada em
  `src/` nesta rodada). Nenhum GRANT de `service_role` em tabela onde não há
  necessidade documentada (`profile`, `consent`, `plan_progress`,
  `preference`, `push_subscription`, `analytics_event` não têm GRANT
  explícito de `service_role` além do `TRUNCATE`/`TRIGGER`/`REFERENCES` de
  plataforma já analisado na Seção 4.1 — nenhuma rotina de serviço
  documentada precisa tocar essas tabelas fora de RLS ainda).
- **`bump_metric`**: `EXECUTE` concedido só a `anon`/`authenticated` (não a
  `service_role`, que não precisa — a apuração lê a tabela diretamente via
  SELECT, não via RPC) — sem excesso.

**Conclusão desta checagem: nenhuma tabela do lote ficou acessível além do
que a política de RLS pretende.** Nenhum GRANT a `anon` onde não deveria,
nenhum GRANT de UPDATE/DELETE onde a tabela é append-only ou de escrita
exclusiva de serviço.

## 6. Requisitos de Segurança Operacional para o Chapéu DevOps

Primeira vez que este lote gera requisitos operacionais reais (lotes
anteriores eram só cliente):

- **Gestão de segredo**: credencial de `service_role` (usada por
  `jobs/erasure`/TASK-041, envio de lembrete/TASK-057, apuração de
  `analytics`) nunca pode ser gravada em variável exposta ao bundle do
  cliente (G-10, DI-09) — confirmado que nenhum código de `src/` fora de
  `tests/db/` referencia `service_role`/chave de serviço nesta rodada;
  quando `jobs/erasure`/TASK-057 forem implementados (rotinas de servidor,
  fora do bundle do navegador), a chave deve viver só em variável de
  ambiente do runtime de servidor (Edge Function/cron do Supabase),
  reforçando o requisito já registrado nos Lotes 0/1.
- **Configuração de rede**: antes do go-live, confirmar que a porta do
  Postgres gerenciado (5432/6543 no provedor real) não tem exposição pública
  direta (ver achado de `TRUNCATE`/`TRIGGER`/`REFERENCES` de plataforma na
  Seção 4.1) — o acesso ao dado deve acontecer exclusivamente via
  PostgREST/GoTrue (API gerada, sob RLS), nunca via conexão SQL direta
  acessível pela internet aberta com as credenciais de `anon`/`authenticated`.
- **CSP `connect-src`**: trocar `https://*.supabase.co` pelo domínio exato
  do projeto de produção assim que provisionado, antes do go-live (Seção
  4.2) — item de configuração, não de código.
- **HSTS `preload`**: se o domínio de produção for submetido à lista de
  preload do HSTS (fora do escopo deste lote, decisão do chapéu DevOps),
  confirmar que HTTPS está disponível em 100% das rotas antes de submeter —
  reforço, não bloqueio desta auditoria.
- **`bump_metric`/`reminder_log`/`erasure_request` via `service_role`**:
  quando as rotinas de servidor (TASK-041, TASK-057) forem implantadas,
  confirmar que rodam em ambiente de rede interna do provedor (SDD §7.5,
  linha "Rotinas agendadas": "sem endpoint HTTP público") — nenhuma delas
  deve expor endpoint HTTP acessível diretamente pela internet.

## 7. Classificação de Achados

Nenhum achado de severidade alta/crítica. Nenhum achado de compliance
obrigatório em aberto (os dois pontos de atenção de §7.6, Seção 4.3, são
responsabilidade de tarefas futuras, não deste lote). **1 achado de
baixa-média severidade, já registrado como débito com prazo**
(`REFAT-L5-01`, Seção 4.2 — teto de inserção diário ausente em
`analytics_event`, confirmando a classificação já feita pelo chapéu QA). 1
nota de risco residual de plataforma (Seção 4.1, `TRUNCATE`/`TRIGGER`/
`REFERENCES` de default ACL do Supabase) registrada como requisito
operacional para o chapéu DevOps (Seção 6), não como achado de código deste
lote — não exploitável via a superfície real do cliente (PostgREST não
expõe TRUNCATE por nenhum verbo HTTP).

**Nenhum achado com relevância estratégica a sinalizar ao Gestor nesta
rodada** — o débito de `REFAT-L5-01` é uma lacuna de decomposição pontual e
de baixo impacto (proteção de abuso, não de confidencialidade), já dentro da
autoridade normal do Validador para registrar como débito com prazo; não
configura padrão recorrente, não é decisão de negócio, e não é achado de
compliance que exija julgamento do Gestor. O item de rede/porta do Postgres
(Seção 6) é requisito operacional padrão de qualquer deploy Supabase, não
uma decisão estratégica nova.

## 8. Veredito Geral do Lote 5 (rodada TASK-027 a TASK-032, TASK-034; chapéu DevSecOps)

**7 de 7 tarefas desta rodada: Aprovadas, com débito de segurança de
baixa-média severidade registrado (`REFAT-L5-01`, prazo definido).** Nenhum
bloqueio de deploy. TASK-033 segue fora desta auditoria (não implementada).
Definition of Done (chapéu DevSecOps) satisfeita para as 7 tarefas desta
rodada:

- [x] Nenhum achado de severidade alta/crítica em aberto
- [x] Todo achado de compliance obrigatório resolvido (os 2 pontos de
      atenção de §7.6 são de tarefas futuras, não deste lote — Seção 4.3)
- [x] Todo achado de baixa/média severidade virou tarefa em `Refatoração
      Lote-5` com prazo (`REFAT-L5-01`, confirmado nesta rodada — Seção 4.2)
- [x] Requisitos de segurança operacional definidos para o chapéu DevOps
      (Seção 6)
- [x] Todo achado de relevância estratégica sinalizado ao Gestor (nenhum
      nesta rodada)

Combinado com a aprovação funcional já registrada em `.md/QA-REPORT.md`
(seção "Lote 5", Seção 6 — Aprovado com ressalva, 7/7 tarefas desta rodada),
as 7 tarefas do Lote 5 desta rodada têm agora **dupla aprovação** (QA +
DevSecOps). O chapéu DevOps pode prosseguir com deploy dessas 7 tarefas
(junto dos Lotes 0-4, se ainda não implantados), sujeito à sua própria
Definition of Done (rollback testado, observabilidade ativa, infraestrutura
validada contra requisitos não funcionais do SDD.md) e aos requisitos
operacionais registrados na Seção 6 acima (rede/porta do Postgres, gestão de
segredo de `service_role`, `connect-src` exato antes do go-live). O débito
`REFAT-L5-01` (severidade baixa-média, prazo definido) **não pausa este
deploy**, conforme a regra de que débito de baixa/média severidade com prazo
registrado segue normalmente para deploy. **O Lote 5 como um todo (incluindo
TASK-033) não está pronto para fechamento final** — quando TASK-033 for
implementada, precisa de validação funcional (QA) e auditoria de segurança
(DevSecOps) próprias antes do Lote 5 ser considerado integralmente
encerrado.

## 9. TASK-033 e Fechamento do Lote 5 (chapéu DevSecOps)

**Contexto**: a Seção 8 acima aprovou 7/7 tarefas da rodada anterior com
débito registrado (`REFAT-L5-01`), deixando TASK-033 explicitamente fora,
por só ter sido auditado o build que o chapéu QA já tinha validado
funcionalmente até então. TASK-033 agora está `Concluída` e aprovada pelo
chapéu QA sem ressalva (`.md/QA-REPORT.md`, seção "Lote 5", Seção 7.1) —
auditando aqui, depois da aprovação funcional, não antes.

### 9.1 Achado potencial investigado a fundo: `SUPABASE_SERVICE_ROLE_KEY` hardcoded em `ci.yml`

Este é o ponto que exige mais rigor desta tarefa, por tocar diretamente
DI-09/GUARDRAILS.md G-10 (inegociáveis: nenhum segredo de serviço aparece em
código de cliente nem, aqui, potencialmente num arquivo versionado do
repositório). Tratado como achado a investigar, não descartado por
suposição.

**Investigação**:

1. Chave em `.github/workflows/ci.yml`, linha do step "DB tests":
   `eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZS1kZW1vIiwicm9sZSI6InNlcnZpY2Vfcm9sZSIsImV4cCI6MTk4MzgxMjk5Nn0.EGIM96RAZx35lJzdJsyH-qQwv8Hdp7fsn3W0YpN81IU`.
   Decodifiquei o JWT (header + payload, base64) de forma independente:
   `{"alg":"HS256","typ":"JWT"}` / `{"iss":"supabase-demo","role":"service_role","exp":1983812996}`.
2. `iss: "supabase-demo"` é o identificador fixo que o Supabase usa **só**
   para as chaves de demonstração/desenvolvimento local que `supabase
   start`/`supabase status` imprimem em qualquer instalação — uma chave de
   projeto real sempre tem `iss` apontando para uma URL/ref de projeto
   específica (`https://<project-ref>.supabase.co/auth/v1`), nunca a string
   literal `supabase-demo`. Isso por si só já é forte evidência de que não é
   credencial real.
3. **Confirmação empírica, não só leitura de documentação**: rodei `npx
   supabase status` contra a instância local deste projeto
   (`supabase_db_EstudoBiblico`, container Docker já em execução no momento
   desta auditoria) e o campo `SERVICE_ROLE_KEY` retornado é **byte-a-byte
   idêntico** ao valor hardcoded em `ci.yml`. Essa chave é derivada do
   `JWT_SECRET` fixo e público de desenvolvimento local (`super-secret-jwt-
   token-with-at-least-32-characters-long`, também impresso por `supabase
   status`, também documentado publicamente pelo Supabase como valor padrão
   de qualquer ambiente local não configurado com segredo próprio) — não do
   segredo de nenhum projeto de produção real, que teria um `JWT_SECRET`
   gerado e nunca impresso em texto claro por ferramenta nenhuma.
4. Já existia precedente aprovado no próprio repositório: a mesma chave
   já aparece como fallback em 6 arquivos de teste pré-existentes
   (`tests/db/rls-{profile-consent,plan-progress,preference,push-
   subscription-reminder-log,analytics,erasure-request}.test.mjs`),
   auditados e aprovados nas rodadas anteriores do Lote 5 (TASK-027 a
   TASK-032) sem ressalva sobre este ponto — `ci.yml` usa exatamente o mesmo
   valor, pela mesma razão (credencial pública fixa de ambiente local, não
   segredo real).
5. `docs/ci-secrets.md` §3 já documenta esse exato padrão como aceitável:
   "a CLI imprime `ANON_KEY`, `SERVICE_ROLE_KEY`, `API_URL`, `DB_URL` etc. —
   usar esses valores locais (...), nunca os de produção" — e a Seção 4 do
   mesmo documento já previa `tests/security/bundle-secrets.test.mjs` como o
   gate mecânico contra vazamento real (ver item 6 abaixo). `ci.yml` não usa
   `secrets.SUPABASE_SERVICE_ROLE_KEY` do GitHub Actions para este valor —
   correto, porque não é segredo de produção que precise estar no cofre de
   secrets do repositório; é a mesma constante pública que qualquer pessoa
   obtém rodando `supabase start` localmente.
6. **Confirmação de que não vaza para o bundle publicado**: `npm run
   test:security` rodado (11/11 verdes), incluindo "bundle real do cliente
   (dist/) não contém segredo de serviço" — não pulado (`dist/` já existe
   neste ambiente) — `scripts/security/scan-bundle-secrets.mjs` roda contra
   o bundle real e não encontra nenhum JWT com claim `role: service_role`
   nem esta chave especificamente. Coerente com o desenho: a chave só é
   usada em `env:` de um step de runner de CI para uma conexão Postgres
   server-side de teste (`tests/db/rls-catalog.test.mjs`), nunca com prefixo
   `VITE_*`, nunca embutida em código do cliente.

**Conclusão**: não é uma credencial real, é a chave pública fixa de
demonstração/desenvolvimento local que o próprio Supabase documenta e
imprime en clair em qualquer instalação local — confirmado por decodificação
do JWT, por comparação byte-a-byte com a saída real de `supabase status`
desta instância, por precedente já aprovado no repositório, pela própria
`docs/ci-secrets.md`, e por ausência confirmada no bundle publicado. **Não é
achado de severidade alguma** — não vira item de `Refatoração Lote-5`, não é
débito, não bloqueia nada. Nenhuma dúvida real restante sobre este ponto;
DI-09/G-10 permanecem cumpridos.

### 9.2 Demais pontos de TASK-033

- Nenhum dado sensível novo é exposto — o teste só lê metadado de catálogo
  (`pg_class`/`information_schema`), nunca dado de aplicação/linha de
  tabela.
- Nenhuma dependência de terceiro nova introduzida por TASK-033 além de
  `pg`/`@types/pg` (já auditados como parte do escopo desta tarefa — `pg` é
  o driver oficial de Postgres para Node, mantido ativamente, sem CVE
  conhecido relevante na versão instalada; `@types/pg` é `devDependency`
  puro, não entra no bundle de produção).
- Requisito de segurança operacional para o chapéu DevOps: nenhum novo além
  dos já registrados na Seção 6 (rede/porta do Postgres local não se aplica
  a produção; gestão do segredo real de `service_role` de produção via
  `secrets.*` do GitHub, nunca hardcoded — distinto do valor de
  demonstração local tratado acima) — TASK-033 não introduz infraestrutura
  nova, só um gate de CI.
- Nenhum achado de relevância estratégica a sinalizar ao Gestor nesta
  subseção.

**Veredito TASK-033 (chapéu DevSecOps): Aprovada, sem ressalva, sem débito.**

### 9.3 Veredito Geral do Lote 5 — Fechamento Completo (8/8 tarefas, chapéu DevSecOps)

**8 de 8 tarefas do Lote 5: Aprovadas, com débito de segurança de
baixa-média severidade registrado (`REFAT-L5-01`, prazo definido, herdado
da rodada anterior — TASK-031). Nenhum bloqueio de deploy.**

Definition of Done (chapéu DevSecOps) satisfeita para o Lote 5 completo:

- [x] Nenhum achado de severidade alta/crítica em aberto
- [x] Todo achado de compliance obrigatório resolvido
- [x] Todo achado de baixa/média severidade virou tarefa em `Refatoração
      Lote-5` com prazo (`REFAT-L5-01`)
- [x] Requisitos de segurança operacional definidos para o chapéu DevOps
- [x] Todo achado de relevância estratégica sinalizado ao Gestor (nenhum)

Combinado com a aprovação funcional completa já registrada em
`.md/QA-REPORT.md` (seção "Lote 5", Seção 8 — Validado com ressalva, 8/8
tarefas), **o Lote 5 tem agora dupla aprovação (QA + DevSecOps) para as 8
tarefas.** O chapéu DevOps pode prosseguir com deploy do Lote 5 completo
(junto dos Lotes 0-4, se ainda não implantados), sujeito à sua própria
Definition of Done e aos requisitos operacionais já registrados na Seção 6.
O débito `REFAT-L5-01` não pausa este deploy. **O Lote 5 está integralmente
encerrado** — fechamento estrutural confirmado pelo Validador em
`.md/QA-REPORT.md`, seção "Lote 5", Seção 7.3, sem dispatch ao
`coordenador`.

---

# Lote 6 — Identidade (TASK-035 a TASK-040)

- **Data**: 2026-09-08
- **Escopo desta rodada**: auditoria de segurança **parcial** — o chapéu QA
  reprovou 2 das 6 tarefas do lote com severidade crítica (`.md/QA-REPORT.md`,
  seção "Lote 6"), então a auditoria completa (SAST/SCA/compliance) sobre o
  lote inteiro fica pendente até a correção e revalidação. Esta seção registra
  formalmente, do ângulo de segurança/compliance, os 2 achados que já
  motivaram a reprovação — porque ambos são, ao mesmo tempo, achados de QA e
  achados de segurança/compliance — e audita de forma independente as 4
  tarefas já aprovadas pelo QA (TASK-037 a TASK-040), que não dependem da
  correção pendente.
- **Base**: `.md/SDD.md` §7.1 (autenticação), §7.2 (autorização/RLS), §7.5
  (superfície de exposição), §7.6 (LGPD — art. 5º II), `.md/GUARDRAILS.md`
  (G-09, G-10, G-11, G-17, G-18).
- **Método**: mesmo padrão de leitura direta de código/migration e execução
  independente de comando já registrado em `.md/QA-REPORT.md`, seção "Lote
  6", Seção 1 — não repetido aqui em detalhe.

## 1. Achados Críticos (bloqueiam deploy) — registro formal

### DEVSEC-L6-01 — Consentimento não é "garantido por política no banco" (LGPD art. 5º II)

**Severidade: crítica. Compliance obrigatório em aberto. Bloqueia deploy por
poder próprio deste chapéu (DevSecOps).**

Já detalhado em `.md/QA-REPORT.md`, seção "Lote 6", Seção 3 — reproduzido
aqui como registro formal de segurança/compliance, sem repetir a análise
completa:

- `SDD.md` §7.6 exige que "nenhuma escrita de dado pessoal no servidor ocorre
  antes de existir a linha de consentimento" seja **"garantido por política
  no banco, não por ordem de chamadas no cliente"**.
- A implementação (TASK-035/TASK-036) garante essa ordem **só** por
  sequência de chamadas no cliente (`signUpWithConsent`: `signUp` seguido de
  `recordConsent`), e o gatilho de banco de TASK-035
  (`handle_new_auth_user`) cria `profile` **sem nenhuma condição** ligada à
  existência de `consent` — não há política de banco (trigger/constraint)
  que impeça uma conta existir sem consentimento associado.
- Cenário de falha real (não hipotético): `signUp` bem-sucedido seguido de
  falha de rede/JS antes de `recordConsent` completar deixa uma conta
  persistida no servidor sem `consent` — nada no banco impede ou repara isso.

**Base legal violada**: LGPD art. 5º, II (dado pessoal sensível) via RNF-05 —
o requisito de arquitetura que opera essa proteção (SDD §7.6) não está
implementado como especificado. Este chapéu classifica isso como achado de
**compliance obrigatório**, não um débito de baixa/média severidade
registrável em `Refatoração Lote-6` — por definição, compliance obrigatório
nunca vira débito, é resolvido antes de aprovação (ver Guardrails deste
agente).

**Ação**: volta para o `executor` (junto de `.md/QA-REPORT.md`), sem
alternativa de aprovação condicional. Direção de correção sugerida: mover a
gravação de `consent` para dentro do mesmo gatilho `security definer` que já
cria `profile`, lendo a aceitação do consentimento como metadata do próprio
`signUp` — torna a garantia estrutural (transação única no banco), em vez de
depender de duas chamadas HTTP separadas do cliente.

### DEVSEC-L6-02 — Regressão determinística no gate de CI de segurança/RLS (`npm run test:db`)

**Severidade: crítica (integridade de build/gate compartilhado).**

Já detalhado em `.md/QA-REPORT.md`, seção "Lote 6", Seção 2. Do ângulo
DevSecOps: o teste quebrado (`rls-profile-consent.test.mjs`) é parte do
**gate de RLS** que este chapéu depende para confirmar isolamento de titular
em toda auditoria futura — TASK-033 (Lote 5) tornou esse teste um gate de CI
justamente para que uma futura tabela/gatilho sem RLS/GRANT correto nunca
passe despercebido. Um teste desse gate falhando de forma determinística
(mesmo que por uma asserção obsoleta, não por RLS de fato quebrada — as
asserções de isolamento entre titulares continuam verdes) reduz a confiança
do gate como um todo até ser corrigido: qualquer PR futura já chega com um
step vermelho, criando risco de a equipe se acostumar a ignorar falha de CI
("alarm fatigue"), o que é precisamente o tipo de erosão de gate que RT-04
foi desenhado para evitar.

**Ação**: resolvido junto de DEVSEC-L6-01 (mesma correção de gatilho exige
reescrever a asserção obsoleta do teste). Não é achado de RLS quebrada em si
— reclassificado aqui, para clareza, como achado de **integridade de
pipeline**, não de exposição de dado.

## 2. Auditoria Independente das 4 Tarefas Já Aprovadas pelo QA

### SAST/SCA

- `npm audit --omit=dev`: **0 vulnerabilidades** nas dependências de
  produção.
- `@supabase/supabase-js` (`^2.116.0`, `dependencies`): mandatada pelo
  ADR-008 (Supabase como backend gerenciado) — não é uma escolha nova deste
  lote, é a peça central da stack já decidida; coerente.
- `pg`/`@types/pg` (Lote 5, `devDependencies`): confirmado que continuam só
  em `devDependencies` (não bundladas ao cliente) — usadas só por
  `tests/db/*.mjs`, nunca por código de `src/`.
- Nenhuma dependência nova de runtime introduzida por TASK-036/037/038/039/040
  além de `@supabase/supabase-js` (já auditada em TASK-035).

### Exposição de Segredo

- `npm run test:security`: 11/11 verdes, sem regressão — bundle de produção
  (`dist/`) sem string proibida (DI-05) e sem segredo de serviço (DI-09).
- Leitura direta de `create-identity-supabase-client.ts` confirma:
  `resolveSupabaseClientEnvConfig` só lê `VITE_SUPABASE_URL`/
  `VITE_SUPABASE_ANON_KEY` — nenhuma leitura de `service_role`/`SECRET_KEY`
  em nenhum arquivo de `src/features/identity` (grep confirmado). O
  `service_role` só aparece nos arquivos de teste (`e2e/*.spec.ts`,
  `tests/db/*.mjs`), nunca em código que compõe o bundle publicado — mesmo
  padrão já auditado em lotes anteriores.

### DI-10/G-11 — Sessão fora de `localStorage` (auditoria independente)

- Confirmado por leitura direta (não só confiança no teste do Executor):
  `createIdentityAuthGateway` (`create-identity-supabase-client.ts`) passa
  `storage: createDexieAuthStorage(db)` ao `createClient` — o adaptador
  grava em `db.authSession` (IndexedDB via Dexie), nunca em
  `globalThis.localStorage`.
- Confirmado que nenhum outro ponto do módulo (`sign-up.ts`,
  `sign-in-with-password.ts`, `sign-in-with-magic-link.ts`,
  `request-password-reset.ts`, `confirm-signup.ts`) cria um `createClient`
  paralelo sem esse `storage` customizado — todo o módulo passa pelo mesmo
  ponto único de configuração (`create-identity-supabase-client.ts`), sem
  caminho alternativo que reintroduziria `localStorage`.
- Os testes de TASK-038 exercitam o SDK genuíno (não um fake de storage) via
  `jsdom` (implementação real de `localStorage`) contra login real e refresh
  automático real do SDK — considerado evidência equivalente a uma inspeção
  de DevTools/navegador real para este ponto do projeto (nenhuma tela ainda
  está montada em rota real, ver `.md/QA-REPORT.md` Seção 4/TASK-038).

**Veredito DI-10/G-11: cumprido, sem achado.**

### SDD.md §7.1 (Autenticação) — Requisitos Aplicáveis

| Requisito | Status |
|---|---|
| E-mail+senha com verificação; link mágico como alternativa | Cumprido (TASK-035/TASK-037) |
| Sessão com token de vida curta (≤ 1h) com refresh rotativo | `jwt_expiry = 3600` (`supabase/config.toml`) confirmado; `autoRefreshToken: true` explicitado (TASK-038); mecanismo é o do próprio SDK, não reimplementado |
| Nenhum segredo além do token de sessão gravado no navegador; nenhum conteúdo do usuário em `localStorage` | Cumprido (ver acima) |
| Sem conta: navegação/leitura não exigem identidade | Fora do escopo deste lote (Lote 1/2, já validado) — sem regressão observada |
| Serviço a serviço: credencial de serviço só no backend, nunca no bundle | Cumprido (`npm run test:security`) |
| Recuperação de acesso: token de uso único, expiração curta, nunca revela existência do e-mail | Cumprido (TASK-037, confirmado por requisição HTTP direta, `.md/QA-REPORT.md` Seção 1) |
| **Senha mínima e verificação de vazamento conhecido** | **Parcialmente cumprido** — ver `REFAT-L6-01` abaixo |
| Sem CAPTCHA de quebra-cabeça (WCAG 3.3.8) | Cumprido (TASK-037, confirmado por grep no DOM) |

**Achado de severidade baixa-média (não bloqueia)**: `minimum_password_length
= 6` em `supabase/config.toml` é o piso mínimo aceito pela própria
ferramenta ("Minimum 6, recommended 8 or more", comentário do arquivo), não
um valor deliberadamente escolhido; `password_requirements = ""` (sem
exigência de complexidade — decisão razoável por si só, alinhada a
NIST 800-63B, que desaconselha regra de complexidade forçada em favor de
comprimento maior). Verificação de vazamento de senha conhecido
(leaked-password protection) não está disponível na config local desta
versão da CLI do Supabase — é tipicamente um recurso do projeto hospedado
(dashboard), não do `supabase/config.toml` local; portanto é um requisito de
**segurança operacional para o chapéu DevOps** confirmar/habilitar no
projeto hospedado antes do go-live (mesmo padrão já usado para o domínio
exato de `connect-src`, TASK-034), não um defeito de código deste lote.
Registrado como `REFAT-L6-01` (Seção 4).

### SDD.md §7.6 (LGPD) — Requisitos Aplicáveis, Além do Achado Crítico

| Requisito | Status |
|---|---|
| Base legal: consentimento específico, versionado | **Ver DEVSEC-L6-01** — mecanismo de garantia não cumpre o texto do SDD |
| Recusa do consentimento: usuário permanece sem conta, dado local | Não testado nesta rodada (fluxo de recusa não é acionado por nenhuma tela do Lote 6 — `BlocoConsentimento` desmarcado é o estado inicial, mas "recusar e prosseguir sem conta" é o comportamento natural de simplesmente não completar o cadastro, não uma ação de UI própria; sem achado, nada a testar além do que já existe) |
| Minimização | Cumprido — `export-data.ts`/`erasure_request` não introduzem nenhum campo além do já modelado no `SDD.md` §5.3 |
| Exclusão em ≤ 15 dias (RN-11) | `due_at` calculado pelo servidor, confirmado (TASK-040, `.md/QA-REPORT.md` Seção 4) — cliente não consegue forjar, confirmado por teste de guardrail extra |
| Portabilidade (CA-10.4) | Exportação em JSON com progresso/preferências/esboços (vazio, documentado), autenticada — cumprido (TASK-039) |
| Telemetria como dado sensível | Fora do escopo deste lote (Lote 5, `analytics_event`, já validado) |

## 3. Requisitos de Segurança Operacional para o Chapéu DevOps

1. **Domínio exato de `connect-src`** (herdado de TASK-034/Lote 5, ainda
   pendente): apertar o wildcard `https://*.supabase.co` para o domínio
   exato do projeto Supabase provisionado, antes do go-live.
2. **Proteção de senha vazada (leaked-password protection)**: habilitar no
   painel do projeto Supabase hospedado (recurso não disponível via
   `supabase/config.toml` local nesta versão da CLI), antes do go-live —
   ver `REFAT-L6-01`.
3. **`minimum_password_length`**: considerar elevar de 6 para 8+ no
   `supabase/config.toml` (ambiente local) e replicar no projeto hospedado —
   mudança de configuração de baixo risco, sem dependência de infraestrutura
   ainda não provisionada, pode ser feita já pelo `executor`.
4. Nenhum requisito operacional novo de rede/firewall/hardening além dos já
   registrados no Lote 5 (Seção 6 daquela seção) — este lote não introduz
   superfície de exposição nova (mesmo domínio Supabase, mesmo CDN estático).

## 4. Classificação de Achados

| ID | Achado | Severidade | Status |
|---|---|---|---|
| DEVSEC-L6-01 | Consentimento garantido só por ordem de chamadas no cliente, não por política no banco (SDD §7.6, LGPD art. 5º II) | Crítica — compliance obrigatório | **Bloqueia deploy.** Volta para `executor` |
| DEVSEC-L6-02 | Regressão determinística em `npm run test:db` (gate de CI de RLS) | Crítica — integridade de pipeline | **Bloqueia deploy** (resolvida junto de DEVSEC-L6-01). Volta para `executor` |
| REFAT-L6-01 | `minimum_password_length = 6` (piso mínimo, não escolha deliberada); leaked-password protection não habilitada (recurso do projeto hospedado) | Baixa-média | Débito registrado, `Refatoração Lote-6`, prazo antes do go-live (parte DevOps/hospedado, parte config local de baixo risco) |
| REFAT-L6-02 | `axe`/`jest-axe` ainda não integrado ao projeto — telas de identidade (T-08/T-09/T-13) validadas só por checagem estrutural via Testing Library, não por auditor de acessibilidade automatizado (DI-14) | Baixa | Débito registrado, `Refatoração Lote-6` |

## 5. Veredito Geral do Lote 6 (chapéu DevSecOps)

**Build não aprovado. 2 achados de severidade crítica em aberto — 1 deles
compliance obrigatório (LGPD).** Este chapéu exerce o poder de bloquear
deploy sozinho por DEVSEC-L6-01, e reforça o bloqueio com DEVSEC-L6-02.

Definition of Done (chapéu DevSecOps) — **não satisfeita** para o Lote 6:

- [ ] Nenhum achado de severidade alta/crítica em aberto — **2 em aberto**
- [ ] Todo achado de compliance obrigatório resolvido, não registrado como
      débito — **não**, DEVSEC-L6-01 em aberto
- [x] Todo achado de baixa/média severidade virou tarefa em `Refatoração
      Lote-6`, com prazo (`REFAT-L6-01`, `REFAT-L6-02`)
- [x] Requisitos de segurança operacional definidos para o chapéu DevOps
      (Seção 3)
- [x] Todo achado de relevância estratégica sinalizado ao `gestor` (Seção 6)

**O chapéu DevOps não deve fazer deploy do Lote 6** (nem isoladamente nem
combinado com um deploy futuro que inclua estas 6 tarefas) até DEVSEC-L6-01/
DEVSEC-L6-02 serem corrigidos pelo `executor` e revalidados por este
Validador (QA + DevSecOps). Isto não afeta o deploy dos Lotes 0-5, já com
dupla aprovação e sem dependência do Lote 6.

## 6. Escalonamento ao Gestor (paralelo, não pré-requisito do bloqueio)

Achado de relevância estratégica: DEVSEC-L6-01 é um desvio de compliance
obrigatório (LGPD art. 5º II) relativo a um requisito de arquitetura já
decidido e documentado no `SDD.md` (não uma interpretação nova) — o Gestor é
notificado em paralelo ao registro deste bloqueio, para visibilidade de
governança sobre um achado de compliance, não como condição para o
`executor` já começar a correção.
