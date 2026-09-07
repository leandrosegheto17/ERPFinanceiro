# QA-REPORT.md — App de Leitura Bíblica Guiada + Preparo de Estudo

- **Chapéu**: Validador (QA)
- **Lote validado**: Lote 0 — Fundação e CI (TASK-001 a TASK-005)
- **Data**: 2026-09-07 (rodada 1); revalidação de TASK-004 em 2026-09-07 (mesmo
  dia, rodada 2, escopo restrito — ver Seção 7)
- **Base**: `.md/TASK.md` (Seção 3, Lote 0), `.md/PRD-TECNICO.md`, `.md/GUARDRAILS.md`
- **Método**: todo comando abaixo foi executado de forma independente neste
  ambiente (não a partir das notas de implementação do Executor no `TASK.md`,
  que serviram só de referência de onde olhar) — resultado real, não relatado.
- **Consumidores**: executor, coordenador, validador (ele mesmo), gestor

---

## 1. Execução Independente — Evidência

| Comando | Resultado |
|---|---|
| `npm run typecheck` (`tsc -b --noEmit`) | Limpo, sem erro |
| `npm run lint` (eslint + `lint:deps`) | Limpo — `"no dependency violations found (13 modules, 14 dependencies cruised)"` |
| `npm run lint:deps` com violação real injetada (`core/tmp_qa_violation.ts` importando de `features/tmp_qa_probe`) | Falhou com exit code **1** e mensagem `error no-features-from-core: src/core/tmp_qa_violation.ts → ../../features/tmp_qa_probe` — confirma que a regra bloqueia de fato, não é lint permissivo. Arquivos de prova removidos após o teste. |
| `npm run build` (`tsc -b && vite build`) | Limpo — gera `dist/index.html`, `dist/manifest.json`, `dist/assets/index-*.js`, `dist/sw.js`, `dist/workbox-*.js`, `dist/registerSW.js` |
| `npx vitest run` | 2 arquivos de teste, 3 testes, todos verdes (`dependency-rule.test.ts` ×2, `pwa-manifest.test.ts` ×1) |
| `npx playwright test` (após `npx playwright install chromium --with-deps`) | 2/2 verdes: "renderiza o shell do app", "cache limpo + rede desligada bloqueia nova navegação" |
| `npm run test:security` (`node --test tests/security/*.test.mjs`) | 4/4 verdes, incluindo a checagem contra `dist/` real (não mais `skipped`, já que o build foi gerado nesta sessão) |
| `dist/manifest.json` validado como JSON (via leitura direta do arquivo) | Válido — `name`, `short_name`, `start_url`, `scope`, `display: standalone`, `lang: pt-BR`, 3 ícones (192, 512, 512 maskable) |
| Ícones referenciados no manifest (`public/icons/*.png`) | 3 arquivos existem, são PNG válidos, com as dimensões corretas (192×192, 512×512, 512×512) |
| `supabase start` (Docker real, projeto `EstudoBiblico`, portas com offset +100) | Sucesso — API/DB/Studio/Auth/Storage/Realtime/Kong healthy, sem colidir com outro projeto Supabase local já rodando na máquina (portas padrão em uso por outro projeto, confirmado via `docker ps`) |
| `supabase status` | Reporta os mesmos serviços ativos, credenciais consistentes |
| `supabase stop` | Sucesso — nenhum container `*_EstudoBiblico` remanescente após o stop (`docker ps` confirmado vazio) |
| `.gitignore` / `git status --short` | `.env`, `dist/`, `node_modules/`, `supabase/.branches`, `supabase/.temp` corretamente ignorados; `.env.example` só com placeholders vazios, nenhum segredo real committável |

Achado colateral (não bloqueia nenhuma tarefa deste lote): o container
`supabase_vector_EstudoBiblico` (analytics/log) entra em crash-loop no Windows
(`ConnectionRefused` ao tentar acessar o daemon Docker via TCP) — a própria CLI
avisa isso no `supabase start` ("Analytics on Windows requires Docker daemon
exposed on tcp://localhost:2375"). É limitação documentada da CLI/Docker
Desktop no Windows, não um defeito introduzido pela TASK-005; os serviços
essenciais (Postgres, Auth, REST, Storage, Studio, Kong) sobem saudáveis e
`supabase start`/`status`/`stop` funcionam de ponta a ponta como pede o
critério de aceite. Registrado como observação operacional, sem ação
corretiva necessária neste lote.

---

## 2. Veredito por Tarefa

### TASK-001 — Scaffold + lint de dependência unidirecional (DI-02)

**Critério de aceite**: "Build limpo; lint falha se `core` importar de
`features`."

- Build limpo: confirmado (`npm run build` sem erro).
- Lint falha na violação real: confirmado com injeção de violação real (não
  só a fixture do teste automatizado) — ver Seção 1.
- Regex da regra é cross-plataforma (`[\\/]`), testado neste ambiente Windows
  de fato.

**Veredito: Aprovado.**

### TASK-002 — Pipeline CI (GitHub Actions)

**Critério de aceite**: "Workflow verde em PR de exemplo."

- `.github/workflows/ci.yml` existe, roda `npm ci` → `lint` → `typecheck` →
  `test` → `build`, gatilho `push`/`pull_request` em `main`, `concurrency`
  por ref (cancela execução obsoleta), Node 24 com cache npm. Sintaxe YAML
  válida.
- Os 4 comandos do workflow foram confirmados de forma independente nesta
  sessão (Seção 1) contra o estado atual do repositório — todos limpos.
- Sem PR de exemplo real aberto no GitHub para observar o badge verde do
  Actions diretamente (ambiente local, sem push) — mas a reprodução local dos
  mesmos comandos, na mesma ordem do workflow, é evidência equivalente de que
  o workflow passaria.

**Veredito: Aprovado.**

Achado **simples** (não bloqueia, não reprova a tarefa, mas é lacuna real do
critério de um lote inteiro — ver TASK-004 abaixo para o achado que sim
reprova): nenhuma revisão aqui, o achado central deste lote está registrado
na TASK-004, cujo critério de aceite é o que fica descumprido.

### TASK-003 — `vite-plugin-pwa` + Workbox: shell instalável

**Critério de aceite**: "App instala localmente; manifest.json válido."

- `manifest.json` gerado no build é JSON válido com todos os campos mínimos
  de instalabilidade (`name`, `short_name`, `start_url`, `display:
  standalone`, `scope`, ícones 192/512/512-maskable, cada um existindo de
  fato em `public/icons/` como PNG válido nas dimensões corretas).
- `<link rel="manifest">` presente, Service Worker real gerado (`sw.js` não
  vazio, contém Workbox — DI-15).
- "Instala localmente" não foi confirmado via instalação de fato num
  navegador real neste ambiente (headless/CLI, sem sessão de browser
  interativa) — mas todos os pré-requisitos técnicos de instalabilidade
  (manifest válido + ícones + SW registrável) estão presentes e eu confirmei
  cada peça individualmente. Ressalva de cobertura, não motivo de reprovação.
- `devOptions.enabled: false` confirmado no `vite.config.ts`: SW não registra
  em modo dev, coerente com a suposição do teste e2e de TASK-004 (que testa
  cenário "sem SW").

**Veredito: Aprovado com ressalva** — instalação real em navegador não
verificada interativamente neste ambiente; todos os requisitos técnicos
verificáveis por CLI/arquivo estão corretos. Não é reprovação: não há indício
de que o app *não* instalaria, só ausência de confirmação visual direta.
Recomendação: confirmar manualmente (Chrome desktop ou Android) na próxima
janela de revisão manual de PWA (TASK-066/068, Lote 13), sem necessidade de
nova tarefa dedicada agora.

### TASK-004 — Setup Vitest + Playwright, rede desligada/cache limpo

**Critério de aceite**: "Teste unitário e e2e placeholder passam em CI."

- Testes unitário e e2e passam **localmente** (verificado de forma
  independente nesta sessão — Seção 1).
- **Em CI**: `.github/workflows/ci.yml` (TASK-002) roda `npm run test`
  (Vitest) mas **nunca invoca `npm run test:e2e` nem `npx playwright test`**.
  Não existe segundo workflow, nem step de e2e no workflow único. Confirmado
  por leitura direta de `.github/workflows/ci.yml` e listagem de
  `.github/workflows/` (só há um arquivo).
- Consequência prática: o teste e2e placeholder passa quando rodado manual,
  mas **nunca roda de fato dentro do CI** — a metade "e2e" do critério de
  aceite ("...passam **em CI**") está literalmente descumprida. Isso não é
  detalhe cosmético: é a própria função do critério (garantir que uma
  regressão de e2e seja pega automaticamente) que fica sem cobertura, e o
  mesmo buraco se propaga silenciosamente para os e2e futuros do Lote 13/15
  (TASK-068, TASK-088) se não for corrigido agora, na tarefa que introduziu o
  runner de e2e.

**Reprodução**:
1. Abrir `.github/workflows/ci.yml`.
2. Observar que os únicos `run:` do job `build` são `npm ci`, `npm run lint`,
   `npm run typecheck`, `npm run test`, `npm run build`.
3. Confirmar em `.github/workflows/` que não há outro arquivo de workflow.
4. Resultado obtido: nenhum step executa Playwright. Resultado esperado pelo
   critério de aceite: e2e placeholder passando **em CI**.

**Severidade: crítica.** Compromete o critério de aceite central da própria
tarefa (não é edge case nem mensagem de erro — é a ausência total de
execução do e2e no ambiente que o critério exige) e a lacuna se propagaria
para tarefas futuras que dependem do mesmo pipeline (TASK-068, TASK-088,
Lote 16 — TASK-089/090).

**Veredito (rodada 1): Reprovado — crítica.** Ação: `TASK-004` volta de
`Concluída` para `Em andamento` no `TASK.md`, nota apontando para esta seção.
Retorna ao `executor` para adicionar um step de e2e ao workflow de CI
(executando contra `vite build` + `vite preview`, ou reaproveitando o
`webServer` de dev já configurado — decisão de implementação do Executor) e
confirmar workflow verde de ponta a ponta, incluindo o e2e, antes de nova
submissão a este Validador. TASK-002 não precisa reabrir (seu próprio
critério de aceite não menciona e2e), mas o arquivo que TASK-002 introduziu é
o que precisa do novo step — nota deixada para o Executor decidir em qual das
duas tarefas registra o commit; QA revalida ambas as pontas na próxima
rodada.

**Revalidação (rodada 2, 2026-09-07) — evidência independente**:

| Comando | Resultado |
|---|---|
| Leitura de `.github/workflows/ci.yml` atualizado | Job `build` recebeu 2 novos steps após `Build`: `Install Playwright browsers (Chromium)` (`npx playwright install --with-deps chromium`) e `E2E tests` (`npm run test:e2e`); nenhum step pré-existente removido/reescrito |
| `python -c "import yaml; yaml.safe_load(...)"` sobre o YAML atualizado | Válido, sem erro de sintaxe |
| `ls .github/workflows/` | Confirma um único arquivo de workflow (sem duplicação/segundo workflow) |
| `npx playwright install --with-deps chromium` | Sem saída (browser já em cache neste ambiente) — sem erro |
| `npx playwright test` | **2/2 verdes**: "renderiza o shell do app", "cache limpo + rede desligada bloqueia nova navegação" — exatamente os mesmos comandos agora presentes no CI |
| `npm run lint` | Limpo (eslint + `lint:deps`, "no dependency violations found") |
| `npm run typecheck` | Limpo |
| `npx vitest run` | 3/3 testes verdes |
| `npm run build` | Limpo, gera `dist/` com `sw.js`/`workbox-*.js`/`manifest.json` |

Coerência do pipeline como um todo: a ordem dos steps
(`checkout` → `setup node` → `npm ci` → `lint` → `typecheck` → `test unitário`
→ `build` → `install Playwright` → `e2e`) é sequencial e sem duplicação. O
`playwright.config.ts` já define `webServer` (servidor de dev do Vite,
`reuseExistingServer: !process.env.CI`), então o próprio Playwright sobe o
servidor dentro do runner — não há dependência quebrada nem step órfão entre
`Build` e os dois novos steps de e2e (o `Build` não é pré-requisito técnico
do e2e, mas sua posição anterior não introduz nenhum efeito colateral;
pipeline seguiria correto mesmo que o e2e viesse antes do build de produção).

**Veredito (rodada 2): Aprovado.** Ambas as metades do critério de aceite
("teste unitário e e2e placeholder passam **em CI**") estão cobertas de fato
pelo workflow, confirmado por reprodução local independente dos mesmos
comandos, na mesma ordem. TASK-002 revalidada em conjunto (integração
cruzada TASK-002×TASK-004): o workflow como um todo é coerente, nenhum step
duplicado ou quebrado pela edição. `TASK.md` mantém `TASK-004` como
`Concluída` (o Executor já havia atualizado o status ao registrar a
correção); nenhuma reversão necessária nesta rodada.

### TASK-005 — Setup Supabase local + segredos de CI

**Critério de aceite**: "`supabase start` local funciona; teste de grep do
bundle vazio ainda passa (placeholder)."

- `supabase start`/`status`/`stop` confirmados de ponta a ponta com Docker
  real nesta sessão, sem conflito de porta com outro projeto Supabase local
  já em execução na máquina (prova de que o offset de portas funciona de
  fato, não só na configuração).
- `npm run test:security`: 4/4 testes verdes, incluindo a checagem contra
  `dist/` real (que já não está mais em modo `skipped`, porque esta sessão
  gerou um build de verdade) — comportamento coerente com o que a tarefa
  prometeu ("nunca um pass silencioso").
- Segredos de CI documentados em `docs/ci-secrets.md`; `.env.example` só com
  placeholders vazios; `.gitignore` cobre `.env`/`.env.*` corretamente.
- Achado colateral do container `vector`/analytics em crash-loop no Windows —
  ver Seção 1: não é defeito da tarefa, é limitação conhecida da CLI/Docker
  Desktop no Windows, documentada pela própria ferramenta, e não impede
  nenhuma parte do critério de aceite (API, DB, Auth, Storage, Studio, Kong
  sobem saudáveis).

**Veredito: Aprovado.**

---

## 3. Testes de Integração Cruzada do Lote

- TASK-003 (PWA/`devOptions.enabled: false`) × TASK-004 (e2e placeholder
  assume "sem SW" em dev): confirmado sem dessincronia — o SW de fato não
  registra em `vite dev`, então o teste "cache limpo + rede desligada bloqueia
  navegação" continua testando o cenário correto.
- TASK-001 (regra de dependência) × TASK-004 (timeout do teste): o Executor de
  TASK-004 sinalizou que `dependency-rule.test.ts` (TASK-001) levou 6–19s sob
  carga concorrente de múltiplas instâncias de Executor no mesmo checkout,
  ultrapassando o timeout padrão de 5000ms do Vitest. Reproduzido nesta sessão
  **sem** carga concorrente: os 3 testes (`dependency-rule.test.ts` ×2 +
  `pwa-manifest.test.ts` ×1) passaram juntos em 5.03s totais, dentro do
  timeout padrão, sem necessidade de `--testTimeout` estendido. Como o CI real
  roda em runner isolado por job (sem outras instâncias de Executor
  disputando o mesmo `node_modules`/CPU), a condição que causou a lentidão
  local não deve se repetir em CI — mas como o teste chama o `dependency-cruiser`
  via API real (não mockado), é sensível a I/O/CPU do runner. **Classificado
  como débito de severidade baixa**: monitorar tempo de execução deste teste
  específico nas primeiras execuções reais do workflow (depois que TASK-004
  for corrigida e o workflow rodar em CI de verdade) e, se recorrer, aumentar
  o timeout do teste ou isolar a chamada do dependency-cruiser. Vira tarefa em
  `Refatoração Lote-0` (ver Seção 4) com prazo "antes do fechamento do Lote 1".
- TASK-002 × TASK-005: `npm run test:security` não está no workflow de CI —
  isso é esperado e correto: o critério de aceite de TASK-005 não exige
  execução em CI (só "teste... ainda passa", verificado localmente/placeholder),
  e DI-09 liga a exigência de "todo PR que tocar credenciais roda o teste de
  grep" à TASK-042 (Lote 7), ainda não chegada. Sem gap aqui.

## 4. Requisitos Não Funcionais Relevantes ao Lote

- Build gera bundle de tamanho razoável para um shell inicial (~190 KB JS não
  comprimido, ~60 KB gzip) — sem alerta de performance nesta fase.
- `supabase start` local não exige rede externa após as imagens Docker já
  estarem em cache local (`Skipped - Image is already present locally` para
  todos os serviços) — coerente com o requisito de ambiente local-first do
  projeto.
- Nenhum requisito de acessibilidade/responsividade aplicável ainda neste
  lote (sem tela de produto implementada — só shell `App.tsx` placeholder).

---

## 5. Fechamento Estrutural do Lote (checagem do próprio Validador)

- Status antes da rodada 1: as 5 tarefas do Lote 0 estavam `Concluída` no
  `TASK.md`.
- Após a rodada 1: **4 de 5 aprovadas** (TASK-001, TASK-002, TASK-003 com
  ressalva, TASK-005); **1 reprovada — crítica** (TASK-004), que voltou a
  `Em andamento`.
- Após a rodada 2 (revalidação de escopo restrito, Seção 7): **5 de 5
  aprovadas** (TASK-004 corrigida e reaprovada). `TASK-004` permanece
  `Concluída` no `TASK.md` (o Executor já havia atualizado o status ao
  registrar a correção; nenhuma reversão foi necessária nesta rodada porque a
  reprovação era da rodada anterior, já sanada).
- Dependências da Seção 4 do `TASK.md` relativas ao Lote 0: TASK-002/003/004
  dependem de TASK-001 (satisfeita); TASK-005 é paralela a TASK-001 sem
  dependência bloqueante. Nenhuma dependência órfã ou inconsistente
  encontrada.
- Nenhuma tarefa `Bloqueada` sem resolução neste lote.
- Nenhuma inconsistência encontrada que exija redesenho de dependência ou
  decomposição — não há escalonamento ao `coordenador`.
- Achado simples/débito criado em `Refatoração Lote-0` (Seção 3 do
  `TASK.md`), mantido desde a rodada 1: 1 item —
  - **REFAT-L0-01**: revisar o timeout de `src/tools/dependency-rule.test.ts`
    (ou isolar a chamada real ao `dependency-cruiser`) depois que o CI real
    rodar algumas vezes, se o tempo de execução recorrer perto do limite.
    Prazo: antes do fechamento do Lote 1. Severidade: baixa (débito
    monitorado, não bloqueia nada agora; a dependência declarada da própria
    tarefa — "TASK-004 corrigida e rodando de verdade" — está agora
    satisfeita, então a observação real em CI pode começar).

---

## 6. Veredito Geral do Lote 0

**Lote 0: Aprovado — 5 de 5 tarefas concluídas e validadas** (TASK-001,
TASK-002, TASK-003 com ressalva registrada, TASK-004 aprovada na revalidação
de rodada 2, TASK-005). Nenhuma reprovação crítica em aberto. Definition of
Done por lote (chapéu QA) satisfeita:

- [x] Todo critério de aceite de cada tarefa do lote foi testado e está passando
- [x] Nenhuma reprovação crítica em aberto
- [x] Toda reprovação simples virou tarefa em `Refatoração Lote-0` (nenhuma
      reprovação simples nesta rodada; REFAT-L0-01 é débito de integração
      cruzada, não reprovação de tarefa)
- [x] Testes de integração cruzada executados e passando (Seção 3; TASK-002×
      TASK-004 revalidado na rodada 2, Seção 7)
- [x] Requisito não funcional relevante ao lote validado (Seção 4)

O lote **está pronto** para a auditoria de segurança do chapéu DevSecOps — ver
`.md/SECURITY-REVIEW.md` — e, após dupla aprovação (QA + DevSecOps), para o
chapéu DevOps prosseguir com deploy.

Nenhum padrão recorrente de bug que sugira problema de decomposição ou
diretriz de implementação foi observado neste lote — o achado de TASK-004 foi
isolado (config de workflow incompleta na primeira submissão), corrigido e
confirmado; não escala ao `coordenador`.

---

## 7. Revalidação de TASK-004 — Escopo e Handoff

Escopo desta rodada, conforme delimitado na rodada 1 (Seção 2, TASK-004):
apenas TASK-004 e a integração cruzada TASK-002×TASK-004 (o workflow de CI
como um todo). TASK-001, TASK-003 e TASK-005 não foram reabertas — seus
vereditos da rodada 1 permanecem válidos e não foram reexecutados nesta
rodada.

Evidência completa da revalidação está na Seção 2 (dentro do veredito de
TASK-004, "Revalidação (rodada 2)"). Resultado: **Aprovado**. Lote 0 fechado
com aprovação plena do chapéu QA nesta data. Prossegue para a auditoria do
chapéu DevSecOps sobre o Lote 0 inteiro (TASK-001 a TASK-005), registrada em
`.md/SECURITY-REVIEW.md`.

---

# Lote 1 — Núcleo: Corpus e Importação (TASK-006 a TASK-011)

- **Data**: 2026-09-07
- **Base**: `.md/TASK.md` (Seção 3, Lote 1), `.md/spikes/SPIKE-03-resultado.md`,
  `.md/adr/003-...verificado.md`, `.md/adr/005-...modulo-puro.md`,
  `.md/PRD-TECNICO.md` (RN-05, RN-06), `.md/TASK.md` Seção 1 (DI-01 a DI-15),
  `.md/GUARDRAILS.md`
- **Método**: todo comando abaixo foi executado de forma independente neste
  ambiente; leitura direta do código de `src/tools/corpus-import/`,
  `src/core/corpus/` e `src/core/reference/` para confirmar que cada critério
  de aceite é coberto de fato pelo teste correspondente — as notas de
  implementação do Executor no `TASK.md` serviram só de referência de onde
  olhar, não de evidência.

## 1. Execução Independente — Evidência

| Comando | Resultado |
|---|---|
| `npm run lint` (eslint + `lint:deps`) | Limpo — `"no dependency violations found (35 modules, 61 dependencies cruised)"` |
| `npm run lint:deps` isolado | Limpo, mesmo resultado |
| `npm run typecheck` (`tsc -b --noEmit`) | Limpo, sem erro |
| `npm run build` (`tsc -b && vite build`) | Limpo — gera `dist/index.html`, `dist/manifest.json`, `dist/assets/index-*.js`, `dist/sw.js`, `dist/workbox-*.js` |
| `npx vitest run` (1ª execução) | 7 arquivos, **62/62 testes verdes** |
| `npx vitest run` (execuções 2, 3 e 4, para observar REFAT-L0-01) | 62/62 verdes nas 3 repetições adicionais, sem nenhum timeout — durações 4.31s/4.95s/5.23s/3.93s, todas dentro do timeout padrão do Vitest, sem carga concorrente |
| `npm run test:security` (`node --test tests/security/*.test.mjs`) | 11/11 verdes, incluindo as 7 checagens de `bundle-forbidden-strings` (TASK-011) contra `dist/` real |
| `npx playwright test` | 2/2 verdes (suíte placeholder de TASK-004, sem regressão) |
| Injeção manual da string `"Almeida Atualizada"` em `dist/assets/index-*.js` real + `node --test tests/security/bundle-forbidden-strings.test.mjs` | O teste `"bundle real do cliente (dist/) não contém nenhuma string proibida por DI-05"` **falhou** com `ERR_ASSERTION`, reportando `filePath`/`reason`/`match` exatos (`match: "Almeida Atualizada"`). Rebuild (`npm run build`) restaurou `dist/` limpo; suíte voltou a 7/7 verde. Confirma o critério de aceite de TASK-011 de forma independente, não a partir da nota do Executor. |
| Leitura direta de `src/core/reference/*.ts` e `src/core/corpus/*.ts` (imports) | `core/reference` só importa de `../corpus/*` e de si mesmo; `core/corpus` só importa de `./types`. Nenhum import de `tools/*` ou `features/*` em nenhum dos dois módulos — confirmado por grep, não só pela regra de lint (ver achado simples, Seção 5) |
| Leitura direta de `src/tools/corpus-import/*.ts` (imports) | Nenhum import de `core/*`/`features/*`; único acoplamento com `core/reference`/`core/corpus` é comentário de documentação (não import de código) — separação build vs. runtime confirmada |

## 2. Veredito por Tarefa

### TASK-006 — `tools/corpus-import`: parser do snapshot bruto → modelo canônico

**Critério de aceite**: "Fixture gera estrutura com 66 livros."

- `src/tools/corpus-import/parse-snapshot.test.ts` roda `parseSnapshot` sobre
  a fixture real em disco (`__fixtures__/porbr2018-sample.txt`) e confirma
  `toHaveLength(66)`, a ordem canônica exata (índices 0/38/39/65 =
  Gênesis/Malaquias/Mateus/Apocalipse, comparado contra `CANONICAL_BOOK_IDS`
  de `canon.ts`) e o split AT=39/NT=27 — não é uma contagem solta, é
  verificação estrutural completa contra o cânon.
- `canon.ts` contém exatamente 39+27=66 entradas na ordem correta (conferido
  por leitura direta).
- Preservação de texto sem normalização confirmada por teste dedicado
  (`toBe` exato, sem transformação).
- 3 cenários de falha (livro do cânon ausente, livro fora do cânon presente,
  linha malformada) cobertos e lançam `CorpusParseError`.

**Veredito: Aprovado.**

### TASK-007 — `tools/corpus-import`: verificação byte a byte (RNF-10)

**Critério de aceite**: "Teste que corrompe 1 caractere confirma falha do
pipeline."

- `verify-integrity.test.ts`, caso 2: localiza o texto de um versículo real
  na fixture, troca exatamente **1 caractere** por `"X"` preservando o
  comprimento (`corruptedContent).toHaveLength(fixtureContent.length)`,
  confirmando que é troca de 1 byte, não truncamento disfarçado), e confirma
  `CorpusIntegrityError` com mensagem "Divergência de integridade byte a
  byte". `verifyCorpusIntegrity` de fato reconstrói linha a linha e compara
  `!==` sem nenhuma normalização (leitura direta de `verify-integrity.ts`).
- Caso de truncamento (divergência de contagem de linhas) também coberto.
- Caso de snapshot íntegro não lança — confirma que o teste não é
  permanentemente vermelho/verde por acidente.

**Veredito: Aprovado.**

### TASK-008 — `tools/corpus-import`: manifesto (SHA-256, licença)

**Critério de aceite**: "`manifest.json` validado contra schema."

- `validate-manifest-schema.ts`: assertion function real, campo a campo —
  `sourceId`, `sourceVersionDate`/`importedAt` (data ISO válida via
  `new Date(...).getTime()`), `license`, `licenseUrl` (URL válida via
  `new URL(...)`), `holders` (array não vazio de strings), `modifications`
  (literal `"none"` exato), `sha256` (mapa não vazio, valores validados por
  regex de 64 hex chars). Lança `ManifestSchemaError` com o campo e motivo
  exatos — **não** é um `return true` vazio, confirmado por leitura direta.
- `build-manifest.test.ts` (7 casos): manifesto real gerado por
  `buildManifest` passa na validação; hash determinístico; rejeição correta
  para cada tipo de campo inválido (obrigatório faltando, hash malformado,
  data malformada, `sha256` vazio, `modifications` fora do literal).

**Veredito: Aprovado.**

### TASK-009 — `core/corpus`: acesso tipado ao corpus local

**Critério de aceite**: "Dado o bundle, retorna capítulo por referência
estruturada."

- `get-chapter.ts`: `getChapter(bundle, { bookId, chapter })` faz lookup real
  via índice `Map<bookId, Book>` e retorna o `Chapter` correspondente ou
  `undefined`.
- `get-chapter.test.ts` (5 casos): retorna capítulo correto por referência
  estruturada (incluindo segundo capítulo do mesmo livro e livro diferente
  no mesmo bundle — não é um bundle de um único livro disfarçando o teste),
  `undefined` para `bookId` inexistente, `undefined` para capítulo
  inexistente, equivalência entre `getChapter` e o par
  `buildCorpusIndex`/`getChapterFromIndex`.
- Módulo sem import de `features/*` nem `tools/*` (confirmado por leitura
  direta e por `lint:deps`).

**Veredito: Aprovado.**

### TASK-010 — `core/reference`: reconhecimento de referência bíblica (ADR-005)

**Critério de aceite**: "Suíte simétrica (resolve/não resolve) 100% verde."

- Tabela negativa de `resolve-reference.test.ts` transcreve **literalmente os
  10 casos obrigatórios** da Seção 4 do `SPIKE-03-resultado.md` (`Romanos
  8:28-9:2`, `Rm 8:28; 12:2`, `v. 28`, `Rm8:28`, `Romamos 8:28`, `Gálatas 7`,
  `Romanos 8:99`, `Salmo 151`, `Jo 3:16`, `Livro Fantasma 1:1`), com o
  `reason` exato de cada um confirmado via `it.each` — conferido item a item
  contra o spike, nenhum caso omitido, nenhum `reason` divergente.
- Tabela positiva cobre todos os formatos da Seção 1 do spike (livro+cap:vers
  com `:`/`.`, intervalo com `-`/`–`, capítulo inteiro, numerado
  arábico espaçado/colado e romano, segundo livro numerado, alias "Salmo",
  insensibilidade a maiúsculas/acentos).
- `book-names.ts` confere, campo a campo, com a tabela fechada da Seção 2 do
  spike, incluindo a decisão de ambiguidade da Seção 3: `JOB` (Jó) sem
  `abbreviation`, `JHN` (João) sem `abbreviation`, e os três `1/2/3JN` com
  `abbreviation: "Jo"` (só nos numerados, que nunca colidem com Jó) —
  confirmado por leitura direta linha a linha.
- `resolveReference` nunca lança exceção (teste dedicado com 8 entradas
  malformadas, incluindo string vazia e só espaços).
- `ambiguo` testado via tabela sintética injetada por parâmetro, coerente com
  a nota do spike de que a tabela real não tem colisão nenhuma (confirmado
  também por teste dedicado).
- `resolveReference` só importa de `../corpus/*` e de si mesmo — nenhum
  import de `tools/*` (confirmado por leitura direta, Seção 1).

**Veredito: Aprovado.**

### TASK-011 — Teste anti-"Almeida Atualizada/ARA/ARC" no bundle final (DI-05)

**Critério de aceite**: "CI falha ao inserir a string de propósito."

- Reproduzido de forma independente nesta sessão (não a partir da nota do
  Executor): injetei manualmente `"Almeida Atualizada"` num arquivo real de
  `dist/assets/`, rodei
  `node --test tests/security/bundle-forbidden-strings.test.mjs` e o teste
  contra `dist/` real falhou com `ERR_ASSERTION`, reportando o `match` exato
  — ver Seção 1. Rebuild restaurou 7/7 verde.
- Sem falso positivo grosseiro em siglas curtas: teste dedicado com "ARARA",
  "arco-íris", "arca de Noé", "Marcos", `pararExecucao` — todos passam sem
  disparar o detector (confirmado por leitura do scanner: `ARA`/`ARC`
  exigem borda de token sem letra flanqueando **e** a forma exatamente
  maiúscula — `ara`/`arc` minúsculo em outra palavra não aciona).
- Scanner cobre as 2 frases longas (`Almeida Atualizada`,
  `Almeida Revista e Atualizada`) case-insensitive e as 2 siglas (`ARA`,
  `ARC`) com lookaround de borda de palavra — todas as 4 strings de DI-05/G-01
  cobertas.
- Observação (não é reprovação, já registrada pelo próprio Executor e
  coerente com o escopo desta tarefa): o scanner de TASK-011 não está
  incluído em `.github/workflows/ci.yml` — o gate formal de CI é TASK-091
  (Lote 16), que consolida os 3 checks (strings proibidas, segredos,
  headers). O critério de aceite de TASK-011 ("CI falha ao inserir a string
  de propósito") foi interpretado e verificado como "o teste falha quando a
  string está presente no bundle real", que é o mecanismo que TASK-091
  reusa — não exige, por si, o wiring de workflow.

**Veredito: Aprovado.**

## 3. Testes de Integração Cruzada do Lote

- **`core/reference` (TASK-010) consome `core/corpus` (TASK-009) sem
  importar de `tools/*`** (DI-02/G-07): confirmado de duas formas
  independentes — `npm run lint:deps` limpo (a regra `no-features-from-core`
  não cobre `tools/*`, ver achado simples abaixo) e leitura direta de todos
  os imports de `src/core/reference/*.ts` e `src/core/corpus/*.ts` (Seção 1):
  nenhum import de `tools/*` ou `features/*` em nenhum dos dois módulos. A
  função `buildCorpusIndex` de `core/corpus` é reaproveitada por
  `core/reference` sem duplicação de lógica de índice.
- **`tools/corpus-import` (TASK-006/007/008) não é importado por nenhum
  código de `core/*` ou `features/*`** (separação build vs. runtime, ADR-003):
  confirmado por grep — as únicas menções a `tools/corpus-import` dentro de
  `src/core/*` são comentários de documentação (ex. `types.ts` explicando por
  que `core/corpus` não reimporta os tipos de `tools/corpus-import`), nunca
  um `import` de código. `tools/corpus-import/*.ts` (exceto testes) está sob
  `tsconfig.node.json`, não `tsconfig.app.json` — reforça que não pode
  compilar para o bundle do navegador mesmo que alguém tentasse importar.
- **TASK-006→007→008 (pipeline de build) × TASK-009/010 (runtime)**: os
  tipos de `core/corpus/types.ts` (`Book`/`Chapter`/`Verse`) são
  deliberadamente distintos de `CanonicalBook`/`CanonicalChapter`/
  `CanonicalVerse` de `tools/corpus-import/types.ts` (mesmo shape, camadas
  diferentes) — não há acoplamento estrutural que quebraria se um dos dois
  lados mudar de forma independente, decisão documentada e confirmada por
  leitura direta dos dois arquivos de tipos.
- **TASK-010 (suíte negativa) × `SPIKE-03-resultado.md` Seção 4**: os 10
  casos do spike foram conferidos um a um contra `resolve-reference.test.ts`
  — presentes todos os 10, com o `reason` exato, nenhum a mais nem a menos.
- **REFAT-L0-01 (débito herdado do Lote 0)**: rodei `npx vitest run` 4 vezes
  nesta sessão (Seção 1) — nenhuma recorrência do timeout de
  `dependency-rule.test.ts`. Combinado com a confirmação já feita na
  revalidação de TASK-004 (Lote 0, rodada 2) de que o CI real dispara o
  Playwright sem problema, e sem nenhuma nova ocorrência de lentidão neste
  ambiente em 4 execuções consecutivas: **débito fechado sem alteração de
  código**, conforme a própria prescrição da tarefa ("se recorrer, aumentar o
  timeout... senão, fechar sem alteração"). Ver Seção 5.

## 4. Requisitos Não Funcionais Relevantes ao Lote

- **RNF-10 (byte-idêntico)**: TASK-007 verificado de forma independente
  (Seção 2) — a verificação de integridade é real, não decorativa.
- **RNF-08 (sem terceiro em runtime)**: confirmado por ADR-003 e pela
  separação de import Seção 3 — `core/corpus`/`core/reference` não dependem
  de `tools/corpus-import` (que é o único lugar que tocaria rede/disco fora
  do navegador).
- Tamanho de bundle seguiu estável (~190 KB JS não comprimido, ~60 KB gzip)
  — nenhuma das 6 tarefas deste lote adiciona peso ao bundle do runtime além
  do já existente (o parser/manifesto/verificação de `tools/corpus-import`
  roda em Node, fora do bundle do cliente, confirmado pela divisão de
  `tsconfig`).
- Nenhum requisito de acessibilidade/responsividade aplicável ainda neste
  lote (sem tela de produto implementada).

## 5. Fechamento Estrutural do Lote (checagem do próprio Validador)

- Status antes desta rodada: as 6 tarefas do Lote 1 (TASK-006 a TASK-011)
  estavam `Concluída` no `TASK.md`.
- Após esta rodada: **6 de 6 aprovadas**, nenhuma reprovação (nem crítica nem
  simples).
- Dependências da Seção 4 do `TASK.md` relativas ao Lote 1: TASK-006→
  TASK-001 (satisfeita, Lote 0 aprovado); TASK-007→006; TASK-008→007;
  TASK-009→008; TASK-010→SPIKE-03 (resolvido) + TASK-001; TASK-011→TASK-009.
  Cadeia linear conferida uma a uma, nenhuma órfã ou inconsistente.
- Nenhuma tarefa `Bloqueada` sem resolução neste lote.
- **REFAT-L0-01 fechado** (era débito do Lote 0, com prazo "antes do
  fechamento do Lote 1"): 4 execuções consecutivas de `npx vitest run` nesta
  sessão, sem nenhuma recorrência do timeout. Conforme a prescrição da
  própria tarefa, fechado **sem alteração de código**. Ação no `TASK.md`:
  status de `REFAT-L0-01` atualizado de "Pendente" para "Concluído — fechado
  sem alteração, sem recorrência observada" (ver diff aplicado nesta rodada).
- **Achado simples/débito novo, criado em `Refatoração Lote-1`** (severidade
  baixa, não bloqueia nenhuma tarefa deste lote):
  - **REFAT-L1-01**: a regra de `dependency-cruiser`
    (`.dependency-cruiser.cjs`, `no-features-from-core`) só proíbe
    mecanicamente `core/* → features/*`, não `core/* → tools/*`. A separação
    build vs. runtime (ADR-003, DI-02) está correta hoje (confirmado por
    leitura direta nesta rodada, Seção 1 e 3), mas depende de checagem manual
    — uma futura tarefa em `core/*` poderia importar de `tools/corpus-import`
    por engano sem que o lint acuse. Ação: adicionar uma segunda regra
    `no-tools-from-core` (mesmo padrão da regra existente, trocando
    `features` por `tools`) ao `.dependency-cruiser.cjs`. Prazo: antes do
    fechamento do Lote 2 (não bloqueia o Lote 1 nem o Lote 2 em andamento,
    mas fecha a lacuna de defesa mecânica antes que mais módulos de `core/*`
    sejam adicionados).
- Nenhuma inconsistência encontrada que exija redesenho de dependência ou
  decomposição — não há escalonamento ao `coordenador`.

## 6. Veredito Geral do Lote 1

**Lote 1: Aprovado — 6 de 6 tarefas concluídas e validadas** (TASK-006,
TASK-007, TASK-008, TASK-009, TASK-010, TASK-011). Nenhuma reprovação em
aberto, crítica ou simples. Definition of Done por lote (chapéu QA)
satisfeita:

- [x] Todo critério de aceite de cada tarefa do lote foi testado e está
      passando
- [x] Nenhuma reprovação crítica em aberto
- [x] Toda reprovação simples virou tarefa em `Refatoração Lote-1` (nenhuma
      reprovação simples nesta rodada; REFAT-L1-01 é achado de defesa em
      profundidade da checagem estrutural, não reprovação de tarefa)
- [x] Testes de integração cruzada executados e passando (Seção 3)
- [x] Requisito não funcional relevante ao lote validado (Seção 4)

O lote **está pronto** para a auditoria de segurança do chapéu DevSecOps —
ver `.md/SECURITY-REVIEW.md` — e, após dupla aprovação (QA + DevSecOps), para
o chapéu DevOps prosseguir com deploy.

Nenhum padrão recorrente de bug que sugira problema de decomposição ou
diretriz de implementação foi observado neste lote — as 6 tarefas passaram
na primeira submissão, com evidência de teste genuína (não superficial) em
cada uma; não escala ao `coordenador`.
