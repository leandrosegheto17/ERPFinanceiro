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
