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

---

# Refatoração Lote-1 — REFAT-L1-01 (débito de defesa em profundidade, Lote 1)

- **Data**: 2026-09-07
- **Base**: `.md/TASK.md` (seção "Refatoração Lote-1"), `.md/QA-REPORT.md`
  (seção "Lote 1", Seção 5, achado original REFAT-L1-01),
  `.md/SECURITY-REVIEW.md` (seção "Lote 1", achado DEVSEC-L1-01)
- **Método**: todo comando abaixo foi executado de forma independente nesta
  sessão (não a partir da nota de implementação do Executor no `TASK.md`, que
  serviu só de referência de onde olhar) — incluindo um probe real e
  autônomo em `src/`, distinto do probe que o próprio Executor já havia usado
  e removido durante a implementação.

## 1. Execução Independente — Evidência

| Comando | Resultado |
|---|---|
| Leitura direta de `.dependency-cruiser.cjs` | Confirma a nova regra `no-tools-from-core` no mesmo padrão estrutural de `no-features-from-core`: mesmo `from` (`core/*`), `to` trocado para `tools/*`, `severity: "error"`; comentário de cabeçalho do arquivo atualizado citando ADR-003 e as duas regras |
| `npm run lint` (eslint + `lint:deps`) | Limpo — `"no dependency violations found (35 modules, 61 dependencies cruised)"` |
| Probe real e independente: criado `src/core/__probe_refat_l1_01.ts` importando `CANONICAL_BOOKS` de `src/tools/corpus-import/canon.ts` | `npx depcruise src --config .dependency-cruiser.cjs` reportou `error no-tools-from-core: src/core/__probe_refat_l1_01.ts → src/tools/corpus-import/canon.ts`, exit code **1** — confirma que a regra bloqueia de fato uma violação real em `src/`, não só a fixture isolada do teste automatizado |
| Probe removido | `npm run lint` voltou a `"no dependency violations found"`, exit code 0 — sem resíduo |
| `npx vitest run` | 7 arquivos, **64/64 testes verdes**, incluindo os 2 casos novos de `src/tools/dependency-rule.test.ts` (`describe("DI-02 / ADR-003 — dependência unidirecional core -> tools")`: `src/` real limpo pela nova regra + fixture `core -> tools` reprovada pela regra) — sem regressão nos 62 testes pré-existentes |
| `npm run typecheck` (`tsc -b --noEmit`) | Limpo, sem erro |
| `npm run build` (`tsc -b && vite build`) | Limpo — gera `dist/index.html`, `dist/manifest.json`, `dist/assets/index-*.js`, `dist/sw.js`, `dist/workbox-*.js`, `dist/registerSW.js` |
| `git status --short` após a rodada completa | Confirma ausência de arquivo de probe remanescente; só as edições legítimas (`.dependency-cruiser.cjs`, `.md/TASK.md`, `src/tools/dependency-rule.test.ts`) aparecem como modificadas |

## 2. Veredito por Tarefa

### REFAT-L1-01 — Adicionar regra `no-tools-from-core` ao `.dependency-cruiser.cjs`

**Critério de aceite**: "`npm run lint:deps` falha se `core/*` importar de
`tools/*`, do mesmo jeito que já falha para `features/*`."

- Regra presente, no mesmo padrão estrutural da regra irmã (confirmado por
  leitura direta, Seção 1).
- `npm run lint:deps` passa limpo hoje (nenhuma violação real no `src/`
  atual) — coerente com o achado original, que era lacuna de cobertura
  mecânica, não violação já existente.
- Falha de fato quando há violação real: confirmado com um probe **próprio**
  desta rodada (distinto do probe que o Executor já havia usado e removido),
  criado, testado e removido de forma independente — exit code 1, mensagem
  de erro exata citando a regra `no-tools-from-core`.
- Teste automatizado irmão (`dependency-rule.test.ts`) cobre o mesmo cenário
  via fixture isolada (`core -> tools`), sem depender de spawnar processo
  CLI — os dois métodos (probe real em `src/` + fixture do teste automatizado)
  convergem para o mesmo resultado.
- Nenhuma regressão: suíte completa de testes, typecheck e build permanecem
  limpos após a mudança.
- `cross-platform-integration-testing`: **N/A** — não há múltiplas
  plataformas envolvidas nesta tarefa (mudança de config de lint + teste
  unitário, sem superfície mobile/web distinta a cruzar).
- `non-functional-validation`: sem requisito não funcional novo introduzido;
  a regra adicionada é puramente de defesa em profundidade em tempo de lint,
  sem impacto em runtime/bundle (confirmado pelo tamanho de bundle inalterado
  no `npm run build`).
- Nenhum bug encontrado — `bug-documentation`: N/A.

**Veredito: Aprovado.**

## 3. Fechamento Estrutural do Lote (checagem do próprio Validador)

- Status antes desta rodada: REFAT-L1-01 estava `Concluída` no `TASK.md`,
  com nota de implementação do Executor detalhando a regra adicionada e a
  verificação empírica que ele mesmo já havia feito.
- Após esta rodada: **1 de 1 aprovada**, nenhuma reprovação (nem crítica nem
  simples).
- Dependência declarada na Seção 4 do `TASK.md` (REFAT-L1-01 → TASK-001):
  satisfeita (TASK-001, que introduziu a regra irmã `no-features-from-core`,
  está `Concluída` e aprovada desde o Lote 0). Nenhuma dependência órfã ou
  inconsistente.
- Nenhuma tarefa `Bloqueada` sem resolução neste lote.
- Nenhum achado novo de severidade simples/débito nesta rodada — não há
  necessidade de criar nova entrada em `Refatoração Lote-1` ou em qualquer
  outro lote de refatoração.
- Nenhuma inconsistência encontrada que exija redesenho de dependência ou
  decomposição — não há escalonamento ao `coordenador`.

## 4. Veredito Geral do Lote Refatoração Lote-1

**Refatoração Lote-1: Aprovado — 1 de 1 tarefa concluída e validada**
(REFAT-L1-01). Nenhuma reprovação em aberto, crítica ou simples. Definition
of Done por lote (chapéu QA) satisfeita:

- [x] Todo critério de aceite de cada tarefa do lote foi testado e está
      passando
- [x] Nenhuma reprovação crítica em aberto
- [x] Toda reprovação simples virou tarefa em `Refatoração Lote-X` (nenhuma
      reprovação nesta rodada)
- [x] Testes de integração cruzada executados e passando (N/A — tarefa única
      de config de lint/teste, sem superfície cruzada nova neste lote)
- [x] Requisito não funcional relevante ao lote validado (N/A — sem impacto
      em runtime/bundle)

O lote **está pronto** para a auditoria de segurança do chapéu DevSecOps
confirmar o fechamento de DEVSEC-L1-01 — ver `.md/SECURITY-REVIEW.md` — e,
após dupla aprovação (QA + DevSecOps), para o chapéu DevOps prosseguir com
deploy do lote consolidado.

Nenhum padrão recorrente de bug que sugira problema de decomposição ou
diretriz de implementação foi observado — achado isolado de defesa em
profundidade, fechado na primeira submissão com evidência de verificação
empírica independente; não escala ao `coordenador`.

---

# Lote 2 — Núcleo: Conteúdo Editorial e Validação (TASK-012 a TASK-015)

- **Data**: 2026-09-08
- **Base**: `.md/TASK.md` (Seção 3, Lote 2), `.md/PRD-TECNICO.md` (RN-01 C1–C11,
  RN-04, RN-13), `.md/SDD.md` §5.1/§5.3, `.md/GUARDRAILS.md` (G-04)
- **Método**: todo comando abaixo foi executado de forma independente neste
  ambiente; leitura direta de `src/core/content/*.ts` e
  `src/tools/content-validate/*.ts` para confirmar que cada critério de
  aceite é coberto de fato pelo teste correspondente e pelo próprio código —
  as notas de implementação do Executor no `TASK.md` serviram só de
  referência de onde olhar, não de evidência.

## 1. Execução Independente — Evidência

| Comando | Resultado |
|---|---|
| `npm run lint` (eslint + `lint:deps`) | Limpo — `"no dependency violations found (109 modules, 261 dependencies cruised)"` |
| `npm run typecheck` (`tsc -b --noEmit`) | Limpo, sem erro |
| `npm run build` (`tsc -b && vite build`) | Limpo — gera `dist/index.html`, `dist/manifest.json`, `dist/assets/index-*.js` (190.97 kB / 60.19 kB gzip, tamanho estável frente ao Lote 1), `dist/sw.js`, `dist/workbox-*.js` |
| `npx vitest run` (suíte completa, 1ª execução) | 1 falha: `src/tools/dependency-rule.test.ts` — timeout de 5000ms em "não reporta violação para o src real do projeto"; 221/222 demais testes verdes |
| `npx vitest run src/tools/dependency-rule.test.ts` (isolado, conforme instrução de reprodução da recorrência conhecida) | **4/4 verdes**, 6.03s — confirma que é a mesma intermitência já rastreada (REFAT-L0-01, fechada no Lote 1; recorrência informal do Executor do Lote 4) e não regressão deste lote |
| `npx vitest run` (suíte completa, 2ª execução) | Mesma intermitência isolada reproduzida de novo (1 falha, mesmo teste, mesmo timeout); nenhuma outra falha em nenhuma execução — nenhum teste de TASK-012/013/014/015 jamais falhou em nenhuma das rodadas |
| `npx vitest run src/core/content src/tools/content-validate` (escopo do lote, isolado) | **3 arquivos, 43/43 testes verdes** |
| `npm run test:security` (`node --test tests/security/*.test.mjs`) | 11/11 verdes, sem regressão (DI-05/segredos, herdados do Lote 0/1) |
| `npx playwright test` | 2/2 verdes, sem regressão |
| Leitura direta de `src/core/content/types.ts`/`index.ts` | `Pericope`/`Note`/`AtCategory`/`AtPortion`/`PlanDay`/`ValidationReport`/`Plan` conferidos campo a campo contra `SDD.md` §5.3 (linha 356–359): `Pericope{id,bookId,startChapter/Verse,endChapter/Verse,título}`✓; `Note{id,pericopeId,body,author,reviewer,reviewedAt}`✓ (reviewer/reviewedAt opcionais, coerente com RN-04 "quando houver"); `PlanDay{dayNumber,atPortion,ntPortion,noteIds,hook,wordCount}`✓ (`atWordCount`/`ntWordCount` são extensão aditiva documentada para C5, não do SDD.md original — não quebra o shape base); `Plan{id,name,dayCount,validationReport}`✓, sem array de dias embutido, coerente com a nota do SDD.md de que `days` não é campo essencial de `Plan` |
| Leitura direta de `src/tools/content-validate/validate-content.ts` (imports) | Importa só de `../../core/content` (tipos) e módulos irmãos (`range-utils`, `gospel-versification`) — nenhum import de `features/*`; direção `tools → core` confirmada permitida por `lint:deps` (`no-tools-from-core` só proíbe o sentido oposto, REFAT-L1-01) |
| Leitura direta de `src/core/content/get-plan-day.ts` (imports) | Importa só de `./types` — nenhum import de `tools/*`/`features/*`, confirmado também por `lint:deps` limpo |
| Injeção manual: fixture com dia 3 do plano positivo trocado de AT `narrativo` para AT `legal-ritual` (dentro da janela de ativação 1–7), rodado `validateContent` diretamente via um script ad hoc no REPL do Node contra o bundle de fixture do teste | Reportou violação com prefixo exato `"C3:"` citando o dia e a categoria — reproduz de forma independente o próprio critério de aceite da TASK-013 ("fixture violando C3 falha o build com mensagem específica"), sem depender só do teste automatizado já escrito pelo Executor |
| Injeção manual: nota da fixture positiva com `body` reduzido para 50 palavras, rodado `validateContent` do mesmo jeito | Reportou violação com prefixo `"RN-13:"` citando a nota, a contagem (50) e a faixa "120–200" — reproduz de forma independente o critério de aceite da TASK-014 ("fixture com nota fora do range falha") |

## 2. Veredito por Tarefa

### TASK-012 — Tipos compartilhados: `CorpusManifest`/`Pericope`/`Note`/`PlanDay`/`Plan`

**Critério de aceite**: "Tipos exportados e usados por importer, validador e
`core/content`."

- `Pericope`/`Note`/`AtCategory`/`AtPortion`/`PlanDay`/`ValidationReport`/`Plan`
  exportados em `src/core/content/types.ts`, reexportados pelo barrel
  `src/core/content/index.ts` — confirmado por leitura direta, campo a campo
  contra `SDD.md` §5.3 (Seção 1).
- "Usados por... `core/content`": `src/core/content/get-plan-day.ts` (TASK-015)
  importa `ContentBundle`/`Note`/`Pericope`/`PlanDay` diretamente de `./types`
  — confirmado.
- "Usados por... validador": `src/tools/content-validate/validate-content.ts`
  (TASK-013/014) importa `ContentBundle`/`PlanDay`/`ScriptureRange` de
  `../../core/content` — confirmado.
- "Usados por... importer": aqui há uma leitura deliberada e documentada, não
  uma lacuna — `CorpusManifest` (a única entidade da lista do próprio ID da
  tarefa associada a "importer", já que `tools/corpus-import` é quem lida com
  manifesto/proveniência) **não** foi duplicado para `core/content`: já existe
  em `src/tools/corpus-import/manifest-types.ts` desde a TASK-008 (Lote 1,
  aprovada), é a mesma entidade do diagrama ER do SDD.md §5.1, e a regra
  `no-tools-from-core` (DI-02/REFAT-L1-01) proibiria `core/content` de
  reimportá-lo de `tools/corpus-import` de qualquer forma. Nenhum critério de
  aceite de TASK-013/014/015 depende de `CorpusManifest` (C1–C11 e RN-13 não
  referenciam proveniência/licença do corpus bruto) — confirmado por leitura
  de `PRD-TECNICO.md` RN-01/RN-13 (Seção acima) e por leitura direta de
  `validate-content.ts` (nenhuma menção a `CorpusManifest`/licença). Decisão
  documentada no cabeçalho do próprio `types.ts`, não escondida. Interpretação
  aceita: o critério de aceite fala de tipos "usados por" essas três partes
  coletivamente, não que cada tipo individual precise ser usado por todas as
  três — e a alternativa (duplicar `CorpusManifest`) violaria DI-01 (regra
  existe uma vez) sem necessidade real.
- Teste de smoke `src/core/content/types.test.ts` (6 casos) confirmado
  passando isoladamente (Seção 1).

**Veredito: Aprovado.**

### TASK-013 — `tools/content-validate`: regras C1–C11 (RN-01) com veto de build

**Critério de aceite**: "Fixture violando C3 falha o build com mensagem
específica."

- Reproduzido de forma independente nesta sessão (não a partir da suíte já
  escrita pelo Executor): injeção manual de violação de C3 via script ad hoc
  contra `validateContent`, confirmando mensagem `"C3:"` específica citando
  dia e categoria (Seção 1). `assertContentValid` (a função de veto de build)
  lança `ContentValidationError` com a mesma mensagem — confirmado por
  leitura direta e pelo teste `"assertContentValid lança
  ContentValidationError citando C3"`.
- **G-04 (GUARDRAILS.md)** — "o validador tem poder de veto de build, nunca é
  aviso": confirmado — `assertContentValid` lança exceção (interrompe),
  não loga um aviso e segue. Coerente com o padrão já usado por
  `CorpusParseError`/`CorpusIntegrityError` em `tools/corpus-import`.
- As 9 regras mecanicamente completas (C1, C2, C3, C4, C7, C8, C9, C11, e a
  parte "perícope" de C6) conferidas por leitura direta do código contra a
  tabela de restrições de `PRD-TECNICO.md` §"RN-01" (linhas 482–492) — cada
  `checkCx` implementa exatamente a restrição descrita, sem reinterpretação.
- 3 limitações documentadas no próprio cabeçalho do módulo (não escondidas
  atrás de checagem vazia), avaliadas uma a uma:
  - **C5** (proporção AT/NT): pulada quando `atWordCount`/`ntWordCount`
    faltam em qualquer dia — decisão correta (ausência de dado não é, por si,
    violação de RN-01); testada nos dois sentidos (Seção "demais regras",
    `validate-content.test.ts`).
  - **C6** (fronteira de perícope OU capítulo): só a fronteira de capítulo
    dos 4 Evangelhos (último capítulo) é mecanicamente verificável; fora
    disso, só a fronteira de perícope é checada. **Achado simples**: isso
    significa que uma porção que termina de fato no fim de um capítulo comum
    (não-Evangelho, não-final) sem coincidir com fronteira de perícope seria
    **falsamente reportada como violação de C6**, mesmo estando
    estruturalmente correta pela própria regra do PRD. Não é ausência de
    checagem (que seria pior — "passar" sem verificar) — é o lado
    conservador do erro (falso positivo, não falso negativo), e a fixture
    real de produção provavelmente sempre termina em fronteira de perícope
    (curadoria editorial real), então o risco prático de bloquear um plano
    válido por esse motivo é baixo, mas existe. Registrado como observação;
    não compromete o critério de aceite central da tarefa (que é
    especificamente sobre C3), não bloqueia nenhuma outra tarefa do lote, e é
    de baixo esforço para endereçar (tabela de versificação completa, se algum
    dia necessária). Ver Seção 5.
  - **C10** (nome do plano não sugere cobertura integral): checagem de guarda
    por léxico de frases proibidas sobre `plan.name`, documentada como não
    substituindo verificação de copy de UI real (fora de escopo desta
    tarefa, que valida o bundle, não a tela). Aceitável — o critério de
    aceite da tarefa não exige a parte de UI.
- Fixture positiva de 90 dias (`__fixtures__/valid-bundle.ts`) confirmada
  passando sem violação (`passed: true`) — testada isoladamente (Seção 1).
- **Correção pós-revisão inline registrada pelo próprio Executor** (C6 não
  checava fronteira de capítulo, só perícope, apesar do PRD definir as duas):
  a correção aplicada (reaproveitar `GOSPEL_FINAL_VERSE`) é real e testada
  (2 casos novos confirmados presentes e passando), não apenas anotada —
  confirmado por leitura direta do código atual, não só da nota.

**Veredito: Aprovado.**

### TASK-014 — `tools/content-validate`: cobertura de RN-13 + validação de nota

**Critério de aceite**: "Fixture com nota fora do range falha."

- Reproduzido de forma independente (Seção 1): injeção manual de nota com 50
  palavras no `body`, `validateContent` reporta `"RN-13:"` citando a nota, a
  contagem exata e a faixa "120–200" — critério de aceite confirmado sem
  depender só da suíte já escrita.
- Limites inclusivos (120 e 200 exatos não violam) confirmados por teste
  dedicado e por leitura de `checkNoteValidity`
  (`wordCount < NOTE_MIN_WORD_COUNT || wordCount > NOTE_MAX_WORD_COUNT`,
  comparação estritamente `<`/`>`, não `<=`/`>=`) — coerente com
  `PRD-TECNICO.md`/`SDD.md` §5.3 ("corpo 120–200 palavras", faixa fechada).
- Autoria obrigatória (`author` vazio/só espaço falha) e revisor opcional
  (`reviewer` ausente não falha; presente-mas-vazio falha) conferidos por
  leitura direta contra RN-04 ("toda nota tem autor identificado e, quando
  houver, revisor identificado") — a leitura de "quando houver" como "nunca
  obrigatório, mas se declarado não pode ser vazio" é razoável e não
  reinterpreta o requisito para menos rigor.
- **RN-13, segunda metade de cobertura** (`checkRN13Coverage`): a leitura de
  `PRD-TECNICO.md` linhas 725–733 confirma que a regra tem duas metades
  distintas (dias áridos exigem nota da porção específica, já coberta por
  C8; "nos demais dias, uma nota cobrindo qualquer uma das duas porções
  satisfaz a regra") — a correção pós-revisão inline do próprio Executor
  (que inicialmente havia implementado só a primeira metade) está de fato
  presente no código atual e testada (3 casos, `describe
  "RN-13-cobertura"`), não é só uma nota textual não implementada — confirmado
  por leitura direta de `checkRN13Coverage` e da fixture ajustada.
- `CA-06.6` ("se algum dia não tiver nota associada, falha a validação e não
  publica") coberta pela combinação de C8 (dias áridos) + `checkRN13Coverage`
  (demais dias) — nenhum dia fica sem checagem de cobertura de nota.

**Veredito: Aprovado.**

### TASK-015 — `core/content`: loader do bundle editorial

**Critério de aceite**: "Dado o bundle fixture, retorna `PlanDay` por
número."

- `getPlanDay(bundle, dayNumber)` e `getPlanDayFromIndex` confirmados por
  leitura direta: lookup real via `Map<number, PlanDay>`, `undefined` para
  número inexistente (não exceção) — coerente com o padrão já estabelecido
  por `core/corpus/get-chapter.ts` (TASK-009, Lote 1, aprovado).
- Teste automatizado (`get-plan-day.test.ts`, 6 casos) confirmado passando
  isoladamente (Seção 1): retorno correto por número, dia 90 sem `hook`
  (C11/RF-20), `undefined` para inexistente, equivalência entre a função
  direta e o par índice+lookup, e os dois helpers de perícope/nota.
- Corretamente **não** depende de TASK-013/014 rodarem antes — consome um
  `ContentBundle` já pronto como dado estático, coerente com a coluna
  "Paralelizável-com" do `TASK.md` (TASK-013, TASK-014).
- `core/content` sem import de `tools/*`/`features/*` — confirmado por
  leitura direta e por `lint:deps` limpo (Seção 1).

**Veredito: Aprovado.**

## 3. Testes de Integração Cruzada do Lote

- **TASK-012 (tipos) × TASK-013/014 (validador) × TASK-015 (loader) — mesmo
  shape, sem divergência**: os três consumidores importam os mesmos tipos do
  mesmo barrel (`../../core/content` de `tools/content-validate`, `./types`
  de `core/content/get-plan-day.ts`) — nenhuma cópia paralela de tipo, nenhum
  campo renomeado ou reinterpretado entre os três lados, confirmado por
  leitura direta dos três arquivos de import (Seção 1). O agregado
  `ContentBundle` (conveniência introduzida por TASK-012, não do SDD.md
  original, mas composição direta do diagrama ER §5.1) é consumido de forma
  idêntica pelos dois lados (validador lê `bundle.days`/`.pericopes`/`.notes`
  para validar o conjunto; loader constrói índice sobre os mesmos três
  campos) — sem dessincronia de shape.
- **TASK-013 (C1–C11) × TASK-014 (RN-13)**: ambas operam na mesma função
  `validateContent`, mesma lista `violations: string[]`, mesmo padrão de tag
  (`"C3:"`, `"RN-13:"`, `"RN-13-cobertura:"`) — confirmado que não há
  duplicação de checagem entre C8 (RN-04, nota específica para dia árido) e
  `checkRN13Coverage` (nota genérica para os demais dias): são mutuamente
  exclusivas por `NOTE_REQUIRED_CATEGORIES`, testado explicitamente
  ("dia árido (já coberto por C8) não é duplicado em RN-13-cobertura").
- **TASK-011 (Lote 1, DI-05) × Lote 2**: sem regressão — `npm run
  test:security` 11/11 verde, nenhuma das 4 novas tarefas introduziu string
  proibida no bundle nem alterou o scanner.
- **REFAT-L1-01 (Lote 1, `no-tools-from-core`) × TASK-013/014**: a nova
  direção de import `tools/content-validate → core/content` é exatamente o
  sentido permitido pela regra (só `core → tools` é proibida) — confirmado
  por `lint:deps` limpo com o import real presente, não uma ausência de
  violação por acaso.
- **Débito herdado do Lote 0 (REFAT-L0-01/dependency-rule timeout)**: já
  fechado formalmente no Lote 1 (sem alteração de código, "se recorrer,
  aumentar timeout... senão, fechar sem alteração"). Reapareceu de forma
  intermitente nesta sessão (Seção 1) sob a suíte completa (222 testes,
  mais carga de I/O que a suíte do Lote 1 tinha), mas **não reproduziu
  isoladamente** (4/4 verde, 6.03s) — mesmo padrão observado nas rodadas
  anteriores: sensível a I/O/CPU do runner sob suíte grande, não uma
  regressão de nenhuma tarefa deste lote. Como o item já foi formalmente
  fechado (não é mais um débito em aberto no `TASK.md`) e o CI real roda em
  runner isolado por job (Lote 0, revalidação de rodada 2), não reabro o
  item — registro aqui só como evidência de reprodução, não como novo
  achado.

## 4. Requisitos Não Funcionais Relevantes ao Lote

- Nenhum requisito não funcional de runtime de usuário aplicável — as 4
  tarefas deste lote são tipos compartilhados (sem lógica) e ferramenta de
  validação de build/CI (`tools/content-validate`, roda em Node/CI, não no
  bundle do navegador em produção — confirmado por ausência de import de
  qualquer código de `features/*`, Seção 1).
- Tamanho de bundle do cliente inalterado frente ao Lote 1 (~190.97 kB JS não
  comprimido, ~60.19 kB gzip) — nenhuma das 4 tarefas adiciona peso ao
  runtime do navegador, confirmado por `npm run build` (Seção 1).
- **G-04 (GUARDRAILS.md)**: validado explicitamente acima (TASK-013) — veto
  de build real, não aviso.
- Nenhum requisito de acessibilidade/responsividade aplicável ainda (sem tela
  de produto implementada neste lote).

## 5. Achados

Nenhuma reprovação, crítica ou simples, nas 4 tarefas do lote — todos os
critérios de aceite foram cumpridos e reproduzidos de forma independente.

Um achado de severidade **baixa** (observação, não reprovação) identificado
nesta rodada, para consideração do próprio Validador na checagem estrutural
do fechamento do lote (fora do escopo desta rodada de validação funcional):
a limitação documentada de `checkC6` (TASK-013) pode gerar **falso positivo**
de violação de C6 para uma porção que termina de fato em fronteira de
capítulo de um livro que não é um dos 4 Evangelhos em seu capítulo final —
já que a única tabela de versificação disponível no projeto
(`GOSPEL_FINAL_VERSE`) cobre só esse caso. Não compromete o critério de
aceite central de TASK-013 (que é sobre C3), não bloqueia TASK-014/015, e é
de baixo esforço para endereçar caso um plano real de produção algum dia
esbarre nele. Fica registrado aqui para a checagem estrutural do lote
avaliar se vira item em `Refatoração Lote-2`.

## 6. Fechamento Estrutural do Lote (checagem do próprio Validador)

- Status confirmado: as 4 tarefas do Lote 2 (TASK-012 a TASK-015) estão
  `Concluída` no `TASK.md` — igual ao estado observado nesta rodada, nenhuma
  regressão desde a validação funcional (Seção 2/3).
- Dependências da Seção 3 do `TASK.md` relativas ao Lote 2: TASK-012 → TASK-001
  (satisfeita, Lote 0 validado); TASK-013 → TASK-012 (satisfeita, mesmo lote);
  TASK-014 → TASK-013 (satisfeita); TASK-015 → TASK-012, paralelizável com
  TASK-013/TASK-014 (satisfeita) — nenhuma dependência órfã ou inconsistente.
- Nenhuma tarefa `Bloqueada` sem resolução neste lote; nenhuma entrada `Aberto`
  em `BLOCKERS.md` afetando o Lote 2.
- Nenhuma inconsistência encontrada que exija redesenho de dependência ou
  decomposição — não há escalonamento ao `coordenador`.
- Achado simples/débito criado em `Refatoração Lote-2` (Seção 3 do
  `TASK.md`), a partir do achado de severidade baixa registrado na Seção 5
  acima (`checkC6`, TASK-013): 1 item —
  - **REFAT-L2-01**: ampliar a tabela de versificação usada por `checkC6`
    (ou confirmar que nenhum dia real do plano esbarra no caso) quando o
    acervo editorial real estiver disponível. Prazo: quando o acervo
    editorial real substituir a fixture sintética (não bloqueante até lá).
    Severidade: baixa (falso positivo conservador — nunca deixa passar uma
    violação real, só pode reportar uma inexistente em um caso não coberto
    ainda pela tabela).
- O achado de segurança correspondente (auditado pelo chapéu DevSecOps, ver
  `.md/SECURITY-REVIEW.md`, seção "Lote 2") confirmou não haver achado de
  segurança/integridade duplicado — mesmo item, já coberto por REFAT-L2-01.

**Lote 2 fecha como `Validado (com ressalvas)`** — a única ressalva é
REFAT-L2-01, de severidade baixa e não bloqueante, já agendada.

## 7. Veredito Geral do Lote 2

**Lote 2: Aprovado — 4 de 4 tarefas concluídas e validadas** (TASK-012,
TASK-013, TASK-014, TASK-015). Nenhuma reprovação em aberto, crítica ou
simples. Definition of Done por lote (chapéu QA) satisfeita:

- [x] Todo critério de aceite de cada tarefa do lote foi testado e está
      passando
- [x] Nenhuma reprovação crítica em aberto
- [x] Toda reprovação simples virou tarefa em `Refatoração Lote-2` (nenhuma
      reprovação nesta rodada; o achado de C6, Seção 5, é observação de baixo
      esforço para a checagem estrutural avaliar, não reprovação de tarefa)
- [x] Testes de integração cruzada executados e passando (Seção 3)
- [x] Requisito não funcional relevante ao lote validado (Seção 4)

O lote **está pronto** para a auditoria de segurança do chapéu DevSecOps —
ver `.md/SECURITY-REVIEW.md` — e, após dupla aprovação (QA + DevSecOps), para
o chapéu DevOps prosseguir com deploy.

Nenhum padrão recorrente de bug que sugira problema de decomposição ou
diretriz de implementação foi observado neste lote — as 4 tarefas passaram
com evidência de teste genuína (não superficial) em cada uma, incluindo
reprodução independente dos dois critérios de aceite mais específicos (C3 em
TASK-013, faixa de palavras em TASK-014) fora da suíte já escrita pelo
Executor; não escala ao `coordenador`.

---

# Lote 3 — Núcleo: Armazenamento e Sincronização (TASK-016 a TASK-022)

- **Data**: 2026-09-08
- **Base**: `.md/TASK.md` (Seção 3, Lote 3), `.md/SDD.md` §5.2/§5.4 (schema
  Dexie, diagrama de sequência de sincronização), `.md/adr/006-...` (orçamento
  e expurgo local), `.md/adr/007-...` (idempotência/conciliação), `PRD-TECNICO.md`
  (RNF-03, RT-06, RT-08, RN-07, RN-08), `.md/GUARDRAILS.md` (G-07, G-11)
- **Método**: todo comando abaixo foi executado de forma independente neste
  ambiente; leitura direta de `src/core/storage/*.ts` e `src/core/sync/*.ts`
  (código e testes) para confirmar que cada critério de aceite é cumprido de
  fato — as notas de implementação do Executor no `TASK.md` serviram só de
  referência de onde olhar, não de evidência.

## 1. Execução Independente — Evidência

| Comando | Resultado |
|---|---|
| `npm run lint` (eslint + `lint:deps`) | Limpo — `"no dependency violations found (109 modules, 261 dependencies cruised)"`, incluindo os 19 módulos novos de `core/storage`/`core/sync` (`no-features-from-core` e `no-tools-from-core` ativas e limpas) |
| `npm run typecheck` (`tsc -b --noEmit`) | Limpo, sem erro |
| `npm run build` (`tsc -b && vite build`) | Limpo — `dist/assets/index-*.js` 190,97 kB / 60,19 kB gzip, **tamanho estável** frente ao Lote 2 (190,97 kB) — coerente com `core/storage`/`core/sync` ainda sem consumidor em `features/*`/entrypoint, como esperado nesta fase |
| `npx vitest run` (suíte completa, 1ª execução) | 1 falha: `src/tools/dependency-rule.test.ts` (timeout de 5000ms num dos 4 casos), 220/222 demais verdes |
| `npx vitest run` (suíte completa, 2ª execução) | Mesma falha isolada reproduzida de novo (1/222), nenhuma outra — nenhum teste de TASK-016 a TASK-022 falhou em nenhuma das duas rodadas |
| `npx vitest run src/tools/dependency-rule.test.ts` (isolado, 4 execuções) | 1ª: falhou (1 timeout); 2ª/3ª/4ª: 4/4 verdes, durações 5.54s/6.60s/5.89s — perto do limite de 5000ms por caso individual. Ver achado REFAT-L3-01 (Seção 5) — mesma classe de intermitência já fechada em `REFAT-L0-01` (Lote 1), agora **recorrente** por crescimento do `src/` (109 módulos hoje vs. 35 quando fechada) |
| `npx vitest run src/core/storage src/core/sync` (escopo do lote, isolado) | **7 arquivos, 39/39 testes verdes**, de forma estável |
| `npm run test:security` (`node --test tests/security/*.test.mjs`) | 11/11 verdes, sem regressão (segredo/DI-05, herdados) |
| `npx playwright test` | 2/2 verdes, sem regressão |
| `npm audit` (produção e completo) | 0 vulnerabilidades nas duas checagens |
| `git diff 2fb45e7 HEAD -- package.json` | Confirma `dexie` (produção) e `fake-indexeddb` (dev) como as únicas dependências novas atribuíveis a este lote — `dexie` é biblioteca mandatada por DI-15/SDD §3 (não escolha nova do Executor); `@radix-ui/*`/`@testing-library/*`/`jsdom` pertencem ao Lote 4 (design system), commitado junto |
| Leitura direta de `src/core/storage/*.ts`/`src/core/sync/*.ts` (imports) | Nenhum import de `features/*`/`tools/*` em nenhum dos dois módulos; `core/sync` só importa de `../storage/*` e de si mesmo (direção `core → core` permitida por DI-02) — confirmado também por `lint:deps` limpo |
| Grep por `localStorage` em `src/core/storage/` e `src/core/sync/` | Nenhuma ocorrência de uso real (só menção em comentário de documentação, DI-10) — as duas únicas ocorrências reais de `localStorage` no `src/` inteiro estão em `src/main.tsx`/`src/design-tokens/theme.ts` (Lote 4, fora do escopo desta validação — ver observação cruzada na Seção 3) |

## 2. Veredito por Tarefa

### TASK-016 — `core/storage`: schema Dexie versionado (stores de Fase 1)

**Critério de aceite**: "Migração de versão testada."

- `src/core/storage/db.ts` (`AppDatabase`) declara exatamente as 8 stores de
  Fase 1 de SDD.md §5.2 (`corpusChapters`, `corpusBundle`, `contentBundle`,
  `anonProgress`, `progress`, `preferences`, `outbox`, `eventQueue`), sem as 3
  stores de Módulo 2 — confirmado por leitura direta e pelo teste que lista
  `db.tables` e nega a presença de `outlines`/`outlineSnapshots`/`presentationState`.
- Teste de migração (`db.test.ts`, caso "migração real v1 → v2...") não é
  simulado: abre um Dexie **só com o schema v1** (replicado inline no teste),
  grava uma mutação pendente **sem** `retryCount` (dado legado real), fecha o
  banco, reabre **o mesmo nome de banco/IndexedDB** com o `AppDatabase` real
  (v1+v2) — forçando o `upgrade()` de produção a rodar de fato — e confirma
  que o registro sobrevive intacto, ganha `retryCount: 0` por
  back-preenchimento, e que o índice novo `[status+createdAt]` já funciona
  sobre o dado migrado. Verificado por leitura linha a linha do teste (Seção
  1 acima), não só pela nota do Executor.
- DI-10/G-11: módulo é puramente Dexie/IndexedDB — nenhuma gravação em
  `localStorage` (confirmado por grep).

**Veredito: Aprovado.**

### TASK-017 — `core/storage`: orçamento de quota + expurgo LRU (RNF-03, RT-08)

**Critério de aceite**: "Teste de quota excedida confirma ordem de expurgo declarada."

- `purge.ts`/`quota.ts` lidos diretamente: ordem de expurgo documentada no
  cabeçalho (`corpusChapters` por LRU → `corpusBundle` condicional, no-op
  documentado sem sinal real → esboço, fora de escopo/Fase 2) reflete o que o
  código de fato executa — `purgeIfOverBudget` só chama
  `purgeCorpusChaptersLru` nesta fase, nunca toca `contentBundle`/
  `anonProgress`/`progress`/`preferences`/`outbox`/`eventQueue`.
- `purgeCorpusChaptersLru` ordena por `lastAccessedAt` (`orderBy`) e remove o
  mais antigo primeiro, via índice composto `[bookId+chapter]` — confirmado
  por leitura direta, coerente com a suíte de 6 casos de `purge.test.ts`
  (semeadura fora de ordem cronológica, confirma exclusão em ordem correta;
  demais stores permanecem intactas mesmo com quota excedida).
- `isOverBudget` usa o menor entre teto de produto (50 MB, RNF-03) e quota
  real do navegador (RT-08) — confirmado.
- Nunca dispara expurgo "no escuro": sem Storage API, `estimateStorageUsage`
  devolve quota infinita/uso zero (nunca finge estouro) — confirmado por
  leitura direta, coerente com RNF-03/ADR-006.

**Veredito: Aprovado.**

### TASK-018 — `core/sync`: outbox local (mutação com id próprio e `baseRev`)

**Critério de aceite**: "Mutação persiste sem rede e sobrevive a reload."

- `enqueue-mutation.ts`: `enqueueMutation` só escreve na store `outbox` via
  Dexie — nenhuma chamada de rede/fetch em nenhum caminho do módulo
  (confirmado por leitura direta e por grep de `fetch`/`XMLHttpRequest`, sem
  ocorrência em `core/sync`).
- Teste "sem rede" sobrescreve `globalThis.fetch` para lançar exceção antes
  de chamar `enqueueMutation`, e a mutação persiste normalmente — prova
  direta de que a função nunca depende de rede.
- Teste "sobrevive a reload" fecha e reabre a mesma conexão Dexie/IndexedDB
  (mesmo mecanismo real de TASK-016, não simulação) e confirma que o registro
  completo sobrevive.
- `id` da mutação (`crypto.randomUUID()`) é distinto do `id` da entidade,
  permitindo múltiplas mutações em sequência — confirmado.

**Veredito: Aprovado.**

### TASK-019 — `core/sync`: envio em lote idempotente com gate de rede/sessão

**Critério de aceite**: "Reenvio de lote não duplica no servidor mock."

- `sync-server-client.ts` define a porta `SyncServerClient` — `core/sync`
  nunca conhece `fetch`/SDK concreto (DI-02), confirmado por leitura direta
  (nenhum import de rede em nenhum arquivo de `core/sync`).
- `InMemorySyncServer` (fixture de teste) deduplica por `id` de mutação
  (gerado no cliente, TASK-018) — é essa dedução, não um contador de
  tentativas, que implementa idempotência real.
- `send-pending-mutations.ts` (`sendPendingMutations`): gate de rede/sessão
  confirmado por leitura direta — sem rede **ou** sem sessão, retorna
  imediatamente com `skippedReason` e nunca chama `client.sendBatch` (o
  teste confirma isso via `vi.spyOn`, não só por não observar chamada de
  rede real).
- Cenário central do critério de aceite ("reenvio não duplica"): teste
  "confirmação perdida" registra a mutação diretamente no servidor mock
  (fora do fluxo local), depois roda `sendPendingMutations` — a mutação é
  marcada `"sent"` localmente, mas `receivedCount()` do servidor permanece 1.
  Reproduz exatamente o que o critério de aceite exige.
- Mutação `"rejected"` mantém `pending` e incrementa `retryCount` — confirmado
  por leitura direta (`await db.outbox.update(mutation.id, { retryCount:
  mutation.retryCount + 1 })`).

**Veredito: Aprovado.**

### TASK-020 — `core/sync`: conciliação por entidade (união monotônica + LWW)

**Critério de aceite**: "Dois dispositivos simulados convergem conforme a regra por entidade."

- `reconcile-entity.ts` lido diretamente: `reconcileProgress` agrupa por
  `planId::dayNumber` e sempre resulta na **união** dos dois lados — nunca
  removendo um dia já concluído em qualquer lado (confirmado pela ausência de
  qualquer caminho de remoção no código, só adição/substituição por
  `completedAt` mais antigo quando a chave colide).
- `reconcilePreferences` implementa LWW por `key`, vencendo o `updatedAt`
  mais recente (via `effectivePreferenceTimestamp`, TASK-021 abaixo) — lado
  perdedor é de fato descartado (`Map` por `key`, sem preservar histórico).
- `reconcileDevices` fecha o ciclo ponta a ponta: lê `progress`/`preferences`
  de dois `AppDatabase` reais (nomes de banco distintos, simulando dois
  dispositivos), concilia, e regrava o mesmo resultado nos dois via
  `applyReconciledState` (`clear()` + `bulkAdd`/`bulkPut`) — é essa função
  que de fato prova convergência entre dois dispositivos simulados, não só
  as funções puras isoladas.
- `withoutId` remove o autoincremento Dexie local (nunca significativo entre
  réplicas) do resultado — confirmado, evita que o `id` de uma réplica vaze
  para a outra na gravação de volta.

**Veredito: Aprovado.**

### TASK-021 — `core/sync`: RT-06, relógio incorreto (`received_at` além de `updated_at`)

**Critério de aceite**: "Relógio adiantado 1 ano não corrompe LWW além do aceitável."

- `effectivePreferenceTimestamp` (`reconcile-entity.ts`) lido diretamente:
  quando `receivedAt` está presente e `updatedAt > receivedAt` (relógio do
  dispositivo implausivelmente adiantado), o timestamp usado na comparação de
  LWW é clampado para `receivedAt` — o `updatedAt` armazenado no registro
  **nunca é reescrito** (confirmado: a função só lê os campos, não muta o
  registro) — coerente com ADR-007 ("nunca corrige dado do usuário em
  silêncio").
- Registro sem `receivedAt` usa `updatedAt` puro — comportamento de TASK-020
  preservado por padrão, confirmado por leitura direta (`if (record.receivedAt
  === undefined) return record.updatedAt`).
- Cenário central do critério de aceite reproduzido no teste (dispositivo A
  com relógio adiantado 1 ano vs. dispositivo B com relógio correto e escrita
  real 1 hora depois): sem a mitigação, LWW ingênuo faria A vencer e o valor
  ficaria preso no "futuro" por ~1 ano; com o clamp, B vence — dano limitado à
  janela real de 1 hora entre as escritas. Confirmado por leitura direta do
  teste e do algoritmo, não só pela nota do Executor.
- Campo `receivedAt` em `types.ts`: opcional, **não indexado**, aditivo — sem
  migração de schema Dexie nova (mesmo padrão de campo opcional já usado por
  `PlanDay.atWordCount`, Lote 2), confirmado.

**Veredito: Aprovado.**

### TASK-022 — `core/sync`: migração de progresso anônimo (RN-07)

**Critério de aceite**: "3 dias locais aparecem em `plan_progress` após criar conta."

- `migrate-anonymous-progress.ts` lido diretamente: `migrateAnonymousProgress`
  copia cada `AnonProgressRecord` de `anonProgress` para `progress` (mesmo
  `planId`/`dayNumber`/`completedAt`), enfileira uma mutação `create` por dia
  via `enqueueMutation` (TASK-018, a ponte real para `plan_progress` no
  servidor quando o backend existir, Lote 5/6) e só então limpa
  `anonProgress` — ordem confirmada por leitura direta (grava + enfileira
  antes de limpar, nunca perde progresso anônimo original numa falha
  parcial).
- Idempotência ("migra uma vez", RN-07): `anonProgress` vazio é o próprio
  sinal de "já migrado" — segunda chamada é no-op, confirmado por leitura
  direta (`if (anonRecords.length === 0) return { migrated: [] }`) e pelo
  teste dedicado de idempotência.
- Nota sobre o critério de aceite ("aparece em `plan_progress`"): esta tarefa
  cobre só a etapa local + enfileiramento da mutação — o envio real ao
  servidor (`plan_progress`, tabela do backend) é responsabilidade de
  `sendPendingMutations` (TASK-019), que só terá efeito quando o backend do
  Lote 5/6 existir. Interpretação já documentada pelo próprio Executor e
  confirmada como coerente com o escopo real do lote (não há backend ainda) —
  não é reprovação, é o limite factual do que esta camada pode entregar hoje.

**Veredito: Aprovado.**

## 3. Testes de Integração Cruzada do Lote

- **TASK-016 (schema) × TASK-017/018 (consumidores)**: `purge.ts` usa o
  índice `[bookId+chapter]` e `enqueue-mutation.ts`/`send-pending-mutations.ts`
  usam o índice `[status+createdAt]` da outbox, ambos declarados em `db.ts` —
  confirmado por leitura direta que os dois índices realmente existem no
  schema antes de serem consultados, sem depender de índice implícito do
  Dexie.
- **TASK-018 → TASK-019 → TASK-020 (ciclo de vida da mutação)**: uma mutação
  enfileirada por `enqueueMutation` (`status: "pending"`) é a mesma lida por
  `sendPendingMutations` (`where("status").equals("pending")`) e, quando
  aceita/duplicada, marcada `"sent"` — nenhuma tarefa reimplementa o
  enfileiramento ou duplica a leitura da outbox; confirmado por leitura
  cruzada dos três arquivos.
- **TASK-020 × TASK-021 (mesma função, não dois sistemas paralelos)**:
  confirmado que TASK-021 estende `reconcilePreferences` existente (via
  `effectivePreferenceTimestamp`) em vez de criar um segundo mecanismo de
  LWW — os 9 casos de teste de TASK-020 continuam passando sem alteração
  (Seção 1, 39/39 verdes no escopo do lote), prova de que a extensão é
  aditiva e não regressiva.
- **TASK-020/021 × TASK-022 (execução em paralelo, mesma base)**: `TASK.md`
  registra contagens de teste diferentes para TASK-021 (146/146, incluindo os
  5 casos de RT-06) e TASK-022 (142/142, incluindo os 3 casos de migração) —
  ambas partiram da mesma base de 139 (pós-TASK-020) em execuções paralelas
  de Executor, sem se enxergarem uma à outra no momento do relato. Verificado
  nesta rodada que **o estado final do repositório contém as duas
  contribuições integradas sem conflito**: `npx vitest run src/core/sync`
  (escopo do lote) mostra os testes de `reconcile-entity.test.ts` (incluindo
  os 5 casos de RT-06) e `migrate-anonymous-progress.test.ts` (3 casos) todos
  presentes e verdes juntos — não é um relato inflado, os dois conjuntos
  coexistem de fato no arquivo/módulo final.
- **`enqueueMutation` (TASK-018) reaproveitado por `migrateAnonymousProgress`
  (TASK-022)** sem duplicar lógica de geração de `id`/gravação na outbox —
  confirmado por leitura direta do import e da chamada em
  `migrate-anonymous-progress.ts`.
- **Observação cruzada de lote (não reprova nada deste lote — fora do escopo
  factual de TASK-016 a TASK-022, registrada aqui para o próximo ciclo de
  validação)**: `src/main.tsx`/`src/design-tokens/theme.ts` (Lote 4, TASK-023,
  já `Concluída` no `TASK.md` mas **ainda não validada por este Validador**)
  gravam a preferência de tema (`estudobiblico:theme-preference`) diretamente
  em `window.localStorage`, via `initTheme`/`ThemeStorage` — o mesmo dado
  (`preferences`/`"theme"`) que `core/storage`/`core/sync` deste lote já
  modelam explicitamente para viver em Dexie (`PreferenceRecord`, `db.test.ts`
  grava `key: "theme"`) e sincronizar por LWW (TASK-020/021). Isto é uma
  divergência de arquitetura real entre dois lotes — dado duplicado em duas
  fontes de verdade, e a cópia em `localStorage` nunca passa pela conciliação
  entre dispositivos que este lote acabou de construir — e toca diretamente
  DI-10/G-11 ("conteúdo do usuário só em IndexedDB (Dexie)"). Não é um
  defeito de TASK-016 a TASK-022 (o schema/conciliação de `preferences` está
  correto); é um ponto que a validação futura do Lote 4 precisa examinar
  especificamente contra DI-10/G-11 antes de aprovar TASK-023. Registrado
  aqui, e também em `.md/SECURITY-REVIEW.md` (seção "Lote 3"), como
  observação — não bloqueia o fechamento deste lote.

## 4. Requisitos Não Funcionais Relevantes ao Lote

- **RNF-03 (≤ 50 MB local)**: `STORAGE_BUDGET_BYTES` = 50 MB, confirmado por
  leitura direta; `isOverBudget` usa o menor entre este teto e a quota real
  do navegador (RT-08).
- **RT-08 (uso de quota via `storage.estimate()`)**: confirmado — `quota.ts`
  usa exatamente essa API, com fallback seguro (nunca "no escuro") na
  ausência dela.
- **RT-06 (relógio incorreto)**: mitigação verificada em TASK-021 (Seção 2) —
  dano limitado à janela real entre escritas, nunca ao desvio de relógio
  inteiro.
- Tamanho de bundle estável (190,97 kB / 60,19 kB gzip, igual ao Lote 2) —
  `core/storage`/`core/sync` ainda não têm consumidor em `features/*` ou no
  entrypoint do app, então não pesam no bundle de runtime ainda; o `tsc -b`
  do build typechecka os dois módulos normalmente (confirmado sem erro).
- Nenhum requisito de acessibilidade/responsividade aplicável ainda neste
  lote (sem tela de produto implementada).

## 5. Fechamento Estrutural do Lote (checagem do próprio Validador)

- Status antes desta rodada: as 7 tarefas do Lote 3 (TASK-016 a TASK-022)
  estavam `Concluída` no `TASK.md`.
- Após esta rodada: **7 de 7 aprovadas**, nenhuma reprovação (nem crítica nem
  simples).
- Dependências da Seção 3/4 do `TASK.md` relativas ao Lote 3: TASK-016→
  TASK-001 (satisfeita, Lote 0 aprovado); TASK-017→016; TASK-018→016;
  TASK-019→018; TASK-020→019; TASK-021→020; TASK-022→020. Cadeia conferida
  uma a uma, nenhuma órfã ou inconsistente.
- Nenhuma tarefa `Bloqueada` sem resolução neste lote (confirmado por grep em
  `TASK.md`, nenhuma ocorrência do status `Bloqueada` no documento inteiro).
- **Achado simples/débito novo, criado em `Refatoração Lote-3`** (severidade
  baixa, não bloqueia nenhuma tarefa deste lote):
  - **REFAT-L3-01**: `src/tools/dependency-rule.test.ts` (TASK-001) voltou a
    apresentar o timeout intermitente de 5000ms já fechado uma vez em
    `REFAT-L0-01` (Lote 1, "sem recorrência observada"). Nesta rodada, o
    timeout **recorreu de fato** — 1 falha em 2 execuções da suíte completa, e
    mesmo isolado (sem carga concorrente) 1 de 4 execuções falhou, com as
    demais rodando perto do limite (5,54s–6,60s de duração total do arquivo).
    Causa provável, diferente da hipótese original ("carga concorrente de
    múltiplas instâncias de Executor"): o `src/` real cresceu de 35 módulos
    (quando `REFAT-L0-01` foi fechada) para 109 módulos hoje — a chamada real
    ao `dependency-cruiser` (não mockada) sobre uma árvore 3× maior está mais
    perto do teto de 5000ms por si só, independente de concorrência externa.
    Ação: aumentar o `testTimeout` deste teste especificamente (ex.: 10000ms,
    via terceiro argumento de `it`) **ou** isolar a chamada real do
    `dependency-cruiser` para rodar uma vez por suíte em vez de uma vez por
    caso — decisão de implementação do Executor. Prazo: antes do fechamento do
    Lote 4 (`src/` só vai crescer mais com `features/*`, o problema tende a
    piorar, não a se resolver sozinho como da vez anterior).
- Nenhuma inconsistência encontrada que exija redesenho de dependência ou
  decomposição — não há escalonamento ao `coordenador`.

## 6. Veredito Geral do Lote 3

**Lote 3: Aprovado — 7 de 7 tarefas concluídas e validadas** (TASK-016,
TASK-017, TASK-018, TASK-019, TASK-020, TASK-021, TASK-022). Nenhuma
reprovação em aberto, crítica ou simples. Definition of Done por lote (chapéu
QA) satisfeita:

- [x] Todo critério de aceite de cada tarefa do lote foi testado e está
      passando
- [x] Nenhuma reprovação crítica em aberto
- [x] Toda reprovação simples virou tarefa em `Refatoração Lote-3` (nenhuma
      reprovação de tarefa nesta rodada; REFAT-L3-01 é débito de integração
      cruzada/infraestrutura de teste, não reprovação de nenhuma das 7
      tarefas)
- [x] Testes de integração cruzada executados e passando (Seção 3)
- [x] Requisito não funcional relevante ao lote validado (Seção 4)

O lote **está pronto** para a auditoria de segurança do chapéu DevSecOps —
ver `.md/SECURITY-REVIEW.md` — e, após dupla aprovação (QA + DevSecOps), para
o chapéu DevOps prosseguir com deploy.

Nenhum padrão recorrente de bug que sugira problema de decomposição ou
diretriz de implementação foi observado neste lote — as 7 tarefas passaram
com evidência de teste genuína (migração real de schema, reload real de
IndexedDB, dois `AppDatabase` reais simulando dois dispositivos) em cada uma;
não escala ao `coordenador`. A observação cruzada sobre `localStorage`
(Seção 3) é um ponto isolado entre dois lotes, não um padrão recorrente —
registrada para a validação futura do Lote 4, sem exigir ação deste lote.

---

# Lote 4 — Núcleo: Design System (TASK-023 a TASK-026)

- **Data**: 2026-09-08
- **Base**: `.md/TASK.md` (Seção 3, Lote 4), `.md/UX-SPEC.md` §3.1/§3.2/§5,
  `.md/SDD.md` §5.2 (dado local do dispositivo), `.md/GUARDRAILS.md`
  (G-07, G-11, G-13), `.md/TASK.md` Seção 1 (DI-02, DI-07, DI-08, DI-10,
  DI-11, DI-14, DI-15), `.md/QA-REPORT.md` (seção "Lote 3", Seção 3 —
  observação cruzada sobre `localStorage`)
- **Método**: todo comando abaixo foi executado de forma independente neste
  ambiente; leitura direta de `src/design-tokens/*.ts`/`.css` e
  `src/design-system/**/*.tsx` para confirmar que cada critério de aceite é
  coberto de fato pelo teste correspondente e pelo próprio código — as notas
  de implementação do Executor no `TASK.md` serviram só de referência de onde
  olhar, não de evidência. Validação rodada em paralelo à validação do Lote 3
  por outra instância do Validador, no mesmo checkout — nenhuma alteração de
  código feita por este Validador; só leitura e execução de comandos.

## 1. Execução Independente — Evidência

| Comando | Resultado |
|---|---|
| `npm run lint` (eslint + `lint:deps`) | Limpo — `"no dependency violations found (109 modules, 261 dependencies cruised)"` |
| `npm run typecheck` (`tsc -b --noEmit`) | Limpo, sem erro |
| `npm run build` (`tsc -b && vite build`) | Limpo — gera `dist/assets/index-*.css` (1.97 kB / 0.61 kB gzip, `tokens.css` presente), `dist/assets/index-*.js` (190.97 kB / 60.19 kB gzip — tamanho estável frente ao Lote 3, nenhum primitivo/overlay aparece no bundle ainda por não ter consumidor, como esperado), `dist/sw.js`/`workbox-*.js` |
| `npx vitest run src/design-tokens src/design-system` (escopo do lote, isolado) | **12 arquivos, 76/76 testes verdes**, de forma estável |
| `npx vitest run` (suíte completa, 3 execuções independentes) | **1 falha nas 3 execuções**, sempre o mesmo teste (`src/tools/dependency-rule.test.ts`, timeout de 5000ms, ~6,1s de execução real) — 221/222 nas 3 rodadas; nenhum teste de TASK-023/024/025/026 falhou em nenhuma das 3 execuções |
| `npx vitest run src/tools/dependency-rule.test.ts` (isolado) | **4/4 verdes**, 6.55s — confirma que a falha da suíte completa é contenção de I/O/CPU (mais execução concorrente da instância do Validador validando o Lote 3 em paralelo no mesmo checkout), não defeito do teste em si — ver Seção 5/REFAT-L4-02 |
| `npm run test:security` (`node --test tests/security/*.test.mjs`) | 11/11 verdes, sem regressão (DI-05/segredos herdados) |
| `npx playwright test` | 2/2 verdes, sem regressão |
| `npm audit` (produção `--omit=dev` e completo) | 0 vulnerabilidades nas duas checagens |
| `git diff` de `package.json`/`package-lock.json` desde antes do Lote 4 | Confirma `@radix-ui/react-dialog` (`^1.1.23`) e `@radix-ui/react-toast` (`^1.2.23`) como as únicas dependências de **produção** novas atribuíveis a este lote — exatamente as mandatadas por DI-15 ("Radix Primitives, só em overlays"), nenhuma outra lib de produção nova; `@testing-library/*`/`jsdom` são devDependencies de teste (padrão do ecossistema, não SDK de analytics/anúncio/gamificação vedado por DI-15/G-13) |
| Leitura direta de `src/design-tokens/*.ts`, `src/design-system/primitives/*.tsx`, `src/design-system/overlays/*.tsx` (imports) | Nenhum import de `core/*`/`tools/*`; `design-tokens`/`design-system` ficam em top-level de `src/`, fora de `core/*` (DI-02 proíbe React/DOM em `core/*`) — confirmado também por `lint:deps` limpo |
| Grep por `localStorage` em `src/design-tokens/`, `src/design-system/`, `src/main.tsx` | **2 ocorrências reais**: `src/main.tsx` (`window.localStorage`, passado como `storage` para `initTheme`) e implicitamente via `ThemeStorage`/`initTheme` em `src/design-tokens/theme.ts` — ver achado DI-10/G-11 na Seção 5 (mesma observação já antecipada de forma independente pela validação paralela do Lote 3, `QA-REPORT.md` seção "Lote 3", Seção 3) |
| Verificação manual dos 4 hex de `presentation-theme.css` contra `UX-SPEC.md` §3.1 (linhas 503-508) | Idênticos byte a byte: `--apres-fundo #0B0B0F`, `--apres-texto #F2F2F7`, `--apres-secundario #A8A8B3`, `--apres-acento #F0B429` |
| Leitura direta de `src/design-tokens/contrast.ts` | Fórmula de luminância relativa/razão WCAG 2.x implementada corretamente (coeficientes 0.2126/0.7152/0.0722, transformação linear com o breakpoint 0.03928/12.92 e expoente 2.4, razão `(mais claro + 0.05) / (mais escuro + 0.05)`) — confere com a especificação WCAG citada no cabeçalho do arquivo |

## 2. Veredito por Tarefa

### TASK-023 — Tokens de tipografia/cor/espaçamento (temas claro/escuro) + teste de contraste AA

**Critério de aceite**: "Teste de contraste passa para todos os pares de
token."

- `src/design-tokens/tokens.css`: tipografia/espaçamento/alvo de toque
  copiados literalmente de `UX-SPEC.md` §3.1 (confirmado por leitura direta —
  nenhum valor divergente).
- `token-contrast.test.ts` não confia em valores documentados: parseia o
  próprio `tokens.css` por regex (`parse-tokens-css.ts`) e recalcula a razão
  de cada par anotado `CONTRAST-PAIR` via `contrast.ts` — 7 pares × 2 temas =
  14 casos gerados por `it.each`, todos passando (razões entre ~4.13:1 e
  ~17.01:1, sempre acima do mínimo do nível declarado). Fórmula de contraste
  conferida linha a linha contra a especificação WCAG (Seção 1) — está
  correta.
- Fallback `@media (prefers-color-scheme: dark)` confirmado idêntico ao
  override manual `[data-theme="dark"]` por teste dedicado (`toEqual`) — não
  há flash de tema divergente por acidente.
- **Achado real (DI-10/G-11), não coberto pelo critério de aceite literal mas
  parte do que a tarefa entregou**: o mecanismo de tema (`theme.ts`) persiste
  a preferência do usuário direto em `window.localStorage`
  (`estudobiblico:theme-preference`, via `main.tsx`), nunca em `core/storage`
  (Dexie). `SDD.md` §5.2 define explicitamente que "tema" é conteúdo da store
  `preferences`, sincronizada por LWW (já implementado no Lote 3,
  TASK-020/021) — a implementação atual não usa esse mecanismo, então (a) a
  preferência de tema não sincroniza entre dispositivos como o SDD promete, e
  (b) `GUARDRAILS.md` G-11 ("`localStorage` só pode conter o necessário de
  sessão, nunca conteúdo do usuário") é violado na letra. Achado já
  antecipado de forma independente pela validação paralela do Lote 3
  (`QA-REPORT.md`, seção "Lote 3", Seção 3) e também revisado do ângulo de
  segurança nesta rodada (ver `.md/SECURITY-REVIEW.md`, seção "Lote 4").
  **Classificação: achado simples/débito de severidade baixa-média** — não
  compromete o critério de aceite central da tarefa (o teste de contraste, o
  que foi literalmente pedido, passa de fato) nem quebra nenhuma outra tarefa
  deste lote (TASK-024/025/026 não dependem do mecanismo de persistência de
  tema); mas é uma divergência real e não documentada de um requisito de
  arquitetura explícito (SDD §5.2) e de um guardrail inegociável (G-11), sem
  diferença prática de confidencialidade local (SDD §7.3: dado local não é
  criptografado em nenhum dos dois casos) — por isso não é elevado a crítica.
  Vira tarefa `REFAT-L4-01` em `Refatoração Lote-4` (`.md/TASK.md`), com
  prazo "antes de qualquer tela que exponha o controle de tema ao usuário
  (T-11 Configurações) consumir/persistir a preferência".

**Veredito: Aprovado com ressalva** (débito registrado em `REFAT-L4-01`, não
bloqueia a tarefa nem o lote — ver Seção 5).

### TASK-024 — Tema exclusivo de apresentação (≥7:1) + teste de contraste AAA

**Critério de aceite**: "Teste falha se qualquer par cair abaixo de 7:1."

- Os 4 tokens de `presentation-theme.css` conferem, hex a hex, com a tabela
  "Token (tema apresentação)" de `UX-SPEC.md` §3.1 (Seção 1) — nada
  inventado.
- `presentation-theme-contrast.test.ts` parseia o CSS real (não confia nos
  valores documentados) e recalcula os 3 pares anotados — todos acima de
  7:1 (~17,40:1, ~8,30:1, ~11,40:1); teste dedicado confirma que todo par
  declarado usa o nível `AAA`/razão mínima 7 (não um `AA` esquecido no
  arquivo).
- Reaproveitamento correto de `contrast.ts`/`parse-tokens-css.ts` de TASK-023
  (generalização de `WcagLevel` e do parser para tema único) — nenhuma
  duplicação de fórmula.
- Escopo de aplicação (`[data-theme="apresentacao"]`) documentado como
  contrato futuro, coerente com a rota do Modo Apresentação ainda não existir
  (Lote 15, Fase 2) — não há wiring de componente/rota exigido por esta
  tarefa.

**Veredito: Aprovado.**

### TASK-025 — Primitivos: `Botao`, `CampoDeTexto`, `CampoDeSenha`, `SeletorDeHora`, `Alternador`

**Critério de aceite**: "Teste de teclado e alvo de toque 44×44 em cada."

- Os 5 primitivos são, cada um, um elemento HTML nativo (`<button>`,
  `<input>`, `<input type="time">`) ou composição fina sobre ele — nenhum
  handler de teclado escrito à mão (confirmado por leitura direta dos 5
  arquivos, Seção 1).
- `TOUCH_TARGET_MIN_STYLE` usa o token `--touch-target-min` (TASK-023, 44px)
  via `style` inline, testado via `element.style.minWidth/minHeight` —
  estratégia coerente com a ausência de motor de layout real em jsdom.
- Testes de teclado cobrem exatamente o critério de aceite em cada primitivo:
  Tab foca, Enter/Espaço ativam, `Botao` desabilitado não é alcançado por
  Tab, `CampoDeSenha` alcança o botão de mostrar/ocultar na sequência natural
  de Tab (confirmado por leitura direta de `Botao.test.tsx`/
  `CampoDeSenha.test.tsx`, Seção 1) — 23/23 casos verdes isolados.
- DI-07 (nenhuma informação só por cor) confirmado nos 3 primitivos com
  estado de erro (ícone `aria-hidden` + texto, `role="alert"`,
  `aria-describedby`) e no `Alternador` (`aria-checked` + rótulo textual
  sempre visível) — leitura direta do código, não só da nota do Executor.
- `CampoDeSenha`: senha colável confirmada por teste que dispara um evento
  `paste` sintético e verifica que nada o cancela (`dispatchEvent` retorna
  `true`) — nenhum `onPaste` bloqueador no componente.
- Rótulo sempre visível (nunca só placeholder) nos 4 campos, via
  `<label htmlFor>` real — confirmado.

**Veredito: Aprovado.**

### TASK-026 — Primitivos de sobreposição: `FolhaInferior`, `Dialogo`, `Toast`

**Critério de aceite**: "Teste automatizado de armadilha/restauração de
foco."

- `@radix-ui/react-dialog`/`@radix-ui/react-toast` (DI-15) usados como base
  real (`Dialog.Root/Trigger/Portal/Overlay/Content/Title/Description/Close`,
  `Toast.Provider/Viewport/Root/Title/Description/Action/Close`), não
  reimplementados — confirmado por leitura direta de `Dialogo.tsx`/
  `FolhaInferior.tsx`/`Toast.tsx`.
- `aria-modal="true"` setado explicitamente em `Dialog.Content` — achado do
  próprio Executor (esta versão do Radix Dialog não aplica automaticamente,
  só `role="dialog"`) confirmado por leitura direta do `node_modules`
  instalado nesta sessão (`@radix-ui/react-dialog@1.1.23`).
- `Dialogo.test.tsx`: armadilha de foco testada com 8 Tabs consecutivos (mais
  que o número de elementos focáveis internos), `document.activeElement`
  nunca sai do container — teste real, não decorativo. Restauração testada
  em dois caminhos (Esc e botão Fechar), ambos confirmando foco de volta ao
  gatilho.
- `Toast.test.tsx`: as duas metades documentadas (Toast não é modal — sem
  armadilha de foco, prova de que Tab a partir do botão fechar do toast
  alcança um elemento **fora** dele; e restauração manual só quando o foco
  de fato entrou no toast) — ambas cobertas por teste dedicado, coerente com
  o comportamento real do Radix Toast (achado do próprio Executor, também
  confirmado por leitura direta do `node_modules` nesta sessão).
- DI-08 (alvo de toque 44×44) via `overlays.css` reaproveitando
  `--touch-target-min` — nenhum valor duplicado. DI-07 no `Toast` (ícone
  `aria-hidden` + texto por tipo) confirmado por teste dedicado.
- `overlays.css` usa só tokens já existentes de `tokens.css` (TASK-023) —
  confirmado por leitura direta, nenhum valor de cor/espaçamento novo
  inventado.

**Veredito: Aprovado.**

## 3. Testes de Integração Cruzada do Lote

- **TASK-023 (tokens) × TASK-024 (tema de apresentação)**: `contrast.ts` e
  `parse-tokens-css.ts` são reaproveitados sem duplicação de fórmula entre as
  duas tarefas (`WcagLevel` generalizado para incluir `AAA`,
  `parseSingleThemeTokensCss` como variante de `parseTokensCss`) — confirmado
  por leitura direta; os dois arquivos de teste (`token-contrast.test.ts`,
  `presentation-theme-contrast.test.ts`) continuam isolados um do outro
  (nenhum caso do nível AA testado como AAA ou vice-versa).
- **TASK-023 (tokens) × TASK-025/TASK-026 (primitivos/overlays)**: os 5
  primitivos e os 3 overlays consomem `--touch-target-min` do mesmo
  `tokens.css`, sem duplicar o valor — confirmado por leitura direta de
  `touch-target.ts` e `overlays.css`. Nenhum dos dois módulos (`primitives/`,
  `overlays/`) importa um do outro nem compartilha arquivo — confirmado por
  listagem de diretório (Seção 1), coerente com a decisão documentada pelos
  dois Executores em paralelo de evitar colisão de escopo.
- **TASK-025 × TASK-026 (execução paralela do Executor no mesmo lote)**: os
  dois conjuntos de teste rodam de forma independente e isolada sem conflito
  (76/76 verdes juntos, Seção 1) — nenhuma interferência de estado global
  entre os dois (cada teste chama `afterEach(cleanup)` explicitamente,
  confirmado por leitura direta).
- **`tsconfig.app.json`**: a mudança feita por TASK-025 (deixar de excluir
  `src/**/*.test.tsx` do projeto de app) não quebrou o build de produção nem
  o typecheck de TASK-026 (que reaproveita a mesma mudança sem edição
  adicional) — confirmado por `npm run build`/`npm run typecheck` limpos após
  as duas tarefas.
- **Cross-lote TASK-016/020/021 (Lote 3, `core/storage`/`core/sync`) ×
  TASK-023 (Lote 4, `theme.ts`)**: ver achado DI-10/G-11 detalhado na Seção 2
  (TASK-023) e Seção 5 abaixo — a store `preferences` do Lote 3 já modela
  `tema` para sincronização LWW, mas `theme.ts` do Lote 4 não a utiliza.
  Confirmado por leitura direta dos dois lados (`src/core/storage/types.ts`
  `PreferenceRecord`, `src/design-tokens/theme.ts`) nesta mesma rodada, não
  só repetindo a observação já registrada pela validação paralela do Lote 3.

## 4. Requisitos Não Funcionais Relevantes ao Lote

- **RNF-02 (nenhuma fonte web)**: `--font-family-base` usa só pilha de fontes
  de sistema (`-apple-system`, `BlinkMacSystemFont`, "Segoe UI", Roboto,
  Helvetica, Arial, sans-serif) — confirmado por leitura direta, nenhum
  `@font-face`/`<link>` de fonte externa em `tokens.css` ou em qualquer
  arquivo deste lote.
- **RNF-04 (200% de ampliação)**: escala tipográfica em `rem` (base 16px),
  não `px` fixo — coerente com sobreviver a zoom do navegador; não testado
  por automação nesta rodada (exigiria motor de layout real, ausente em
  jsdom), mas a escolha de unidade é a correta estruturalmente.
- **DI-08 (alvo de toque 44×44, 48×48 em T-20)**: `--touch-target-min`
  (2.75rem = 44px) e `--touch-target-presentation` (3rem = 48px) declarados
  em `tokens.css`; o segundo ainda não é consumido por nenhum componente
  deste lote (T-20 é Lote 15, Fase 2) — coerente, não é uma lacuna desta
  tarefa.
- Bundle de produção permanece estável (~190.97 kB JS / 60.19 kB gzip, igual
  ao Lote 3) — nenhum primitivo/overlay/token entra no bundle além do CSS
  (1.97 kB), porque nenhuma tela real ainda os importa (Lote 6+); coerente
  com o que as notas do Executor declaram.
- Acessibilidade: DI-07/DI-08/DI-14 cobertos por teste automatizado em cada
  primitivo/overlay relevante (Seção 2) — nenhuma inspeção manual isolada
  substituindo teste, conforme exige DI-14.

## 5. Fechamento Estrutural do Lote (checagem do próprio Validador)

- Status antes desta rodada: as 4 tarefas do Lote 4 (TASK-023 a TASK-026)
  estavam `Concluída` no `TASK.md`.
- Após esta rodada: **4 de 4 aprovadas** (TASK-023 com ressalva registrada;
  TASK-024, TASK-025, TASK-026 sem ressalva). Nenhuma reprovação crítica nem
  simples.
- Dependências da Seção 3/4 do `TASK.md` relativas ao Lote 4: TASK-023→
  TASK-001 (satisfeita, Lote 0 aprovado); TASK-024→TASK-023; TASK-025→
  TASK-023 (paralelo a TASK-026); TASK-026→TASK-023 (paralelo a TASK-025).
  Cadeia conferida uma a uma, nenhuma órfã ou inconsistente.
- Nenhuma tarefa `Bloqueada` sem resolução neste lote.
- **2 achados simples/débito criados em `Refatoração Lote-4`** (severidade
  baixa/baixa-média, nenhum bloqueia o lote):
  - **REFAT-L4-01**: mecanismo de tema (TASK-023) persiste preferência em
    `localStorage` em vez de `core/storage`/`core/sync` (DI-10/G-11, divergência
    de SDD.md §5.2) — ver Seção 2 (TASK-023) para o detalhamento completo.
    Prazo: antes de qualquer tela expor o controle de tema ao usuário (T-11
    Configurações).
  - **REFAT-L4-02**: `src/tools/dependency-rule.test.ts` voltou a recorrer no
    timeout padrão de 5000ms de forma **consistente** (3/3 execuções da
    suíte completa falharam nesta rodada, sempre o mesmo teste, sempre
    passando isolado) — REFAT-L0-01 havia sido fechado no Lote 1 sem
    alteração de código por não recorrer sob as condições daquele momento
    (35 módulos); o crescimento do `src` (109 módulos) e/ou execução
    concorrente de outra instância do Validador no mesmo checkout tornam a
    recorrência mais provável agora. Diferente de REFAT-L0-01, desta vez a
    recomendação é agir (aumentar o timeout do teste), não só monitorar — ver
    Seção 1 para a evidência de que o teste em si é rápido isolado (a
    lentidão é de contenção de I/O/CPU sob carga, não de lógica do teste).
- Nenhuma inconsistência encontrada que exija redesenho de dependência ou
  decomposição — não há escalonamento ao `coordenador`. O achado
  DI-10/G-11 (REFAT-L4-01) é uma divergência de implementação vs. arquitetura
  já definida (SDD §5.2), não uma falha da decomposição/dependência do
  `TASK.md` em si — a tarefa que deveria ter usado `core/storage` é a mesma
  tarefa que não o fez, não uma tarefa mal decomposta.

## 6. Veredito Geral do Lote 4

**Lote 4: Aprovado com ressalva — 4 de 4 tarefas concluídas e validadas**
(TASK-023 aprovada com ressalva registrada em `REFAT-L4-01`; TASK-024,
TASK-025, TASK-026 aprovadas sem ressalva). Nenhuma reprovação crítica ou
simples em aberto. Definition of Done por lote (chapéu QA) satisfeita:

- [x] Todo critério de aceite de cada tarefa do lote foi testado e está
      passando
- [x] Nenhuma reprovação crítica em aberto
- [x] Toda reprovação simples virou tarefa em `Refatoração Lote-4` (nenhuma
      reprovação de tarefa nesta rodada; REFAT-L4-01/REFAT-L4-02 são débitos
      de arquitetura/infraestrutura de teste, não reprovação de nenhuma das
      4 tarefas)
- [x] Testes de integração cruzada executados e passando (Seção 3, incluindo
      a integração cruzada com o Lote 3)
- [x] Requisito não funcional relevante ao lote validado (Seção 4)

O lote **está pronto** para a auditoria de segurança do chapéu DevSecOps —
ver `.md/SECURITY-REVIEW.md` — e, após dupla aprovação (QA + DevSecOps), para
o chapéu DevOps prosseguir com deploy. O achado `REFAT-L4-01` (DI-10/G-11)
deve ser considerado com atenção pelo chapéu DevSecOps nesta mesma auditoria,
por tocar diretamente um guardrail de segurança (G-11), ainda que a
classificação de severidade do chapéu QA (baixa-média, não bloqueante) já
tenha sido justificada acima.

Nenhum padrão recorrente de bug que sugira problema de decomposição ou
diretriz de implementação foi observado neste lote — as 4 tarefas passaram
com evidência de teste genuína (teclado real via `user-event`, contraste
recalculado do CSS real, foco confirmado via `document.activeElement`) em
cada uma; não escala ao `coordenador`. O achado DI-10/G-11 é isolado a
`theme.ts`, não um padrão recorrente entre os 4 primitivos/overlays/tokens
deste lote (os demais respeitam DI-02/DI-07/DI-08/DI-10 corretamente,
confirmado por leitura direta em cada um).

---

# Lote 5 — Backend: Schema e Segurança (TASK-027 a TASK-032, TASK-034)

- **Data**: 2026-09-08
- **Escopo desta rodada**: as **7 tarefas já `Concluída`** do Lote 5 —
  TASK-027 (`profile`+`consent`), TASK-028 (`plan_progress`), TASK-029
  (`preference`), TASK-030 (`push_subscription`+`reminder_log`), TASK-031
  (`analytics_event`+`analytics_daily_aggregate`+`bump_metric`), TASK-032
  (`erasure_request`) e TASK-034 (headers de segurança). **TASK-033** (teste
  de catálogo de RLS, RT-04/DI-12) segue `Não iniciada` no `TASK.md` — não
  faz parte desta rodada; o Lote 5 como um todo só fecha quando ela também
  completar (ver Seção 5).
- **Base**: `.md/TASK.md` (Seção 3, Lote 5), `.md/SDD.md` §5.3 (modelo de
  dados), §7.2 (RLS/autorização), §7.5 (superfície de exposição/headers),
  §7.6 (LGPD/RN-11), `.md/GUARDRAILS.md` (G-09, G-10, G-17, G-18), `.md/TASK.md`
  Seção 1 (DI-03, DI-08, DI-09, DI-15), `.md/UX-SPEC.md` (contexto, sem tela
  neste lote — é puramente backend)
- **Método**: todo comando abaixo foi executado de forma independente neste
  ambiente, sem confiar nas notas do Executor nem nas notas de sessões
  anteriores — inclusive o `npx supabase db reset` e a suíte de RLS foram
  rerodados do zero. Leitura direta de cada uma das 6 migrations SQL
  (`supabase/migrations/202609081*`), do código de `src/tools/security/`
  (TASK-034) e dos 6 arquivos de teste real (`tests/db/*.test.mjs`) — as
  notas de implementação do Executor no `TASK.md` serviram só de referência
  de onde olhar, nunca de evidência. Consultas SQL adicionais rodadas
  diretamente contra o Postgres local (via `npx supabase db query`) para
  confirmar `pg_class.relrowsecurity`/contagem de `pg_policies` por tabela e
  o conjunto exato de `information_schema.role_table_grants` por papel —
  não apenas o que os comentários das migrations afirmam.

## 1. Execução Independente — Evidência

| Comando | Resultado |
|---|---|
| `npx supabase db reset` | Limpo — as 6 migrations do lote aplicadas do zero, sem warning/erro (`profile_consent_rls`, `plan_progress_rls`, `erasure_request_rls`, `push_subscription_reminder_log_rls`, `preference_rls`, `analytics_rls`) |
| `npm run test:db` (`node --test tests/db/*.test.mjs`) | **47/47 verdes** — 6 arquivos, cobrindo `profile`/`consent` (TASK-027), `plan_progress` (TASK-028), `preference` (TASK-029), `push_subscription`/`reminder_log` (TASK-030), `analytics_event`/`analytics_daily_aggregate` (TASK-031), `erasure_request` (TASK-032) |
| `npm run lint` (eslint + `lint:deps`) | Limpo — `"no dependency violations found (115 modules, 275 dependencies cruised)"` |
| `npm run typecheck` (`tsc -b --noEmit`) | Limpo, sem erro |
| `npm run build` (`tsc -b && vite build`) | Limpo — gera `dist/_headers` (TASK-034), `dist/manifest.json`, `dist/sw.js`/`workbox-*.js`, bundle JS/CSS estável |
| `npm run test:security` (`node --test tests/security/*.test.mjs`) | 11/11 verdes, sem regressão (DI-05/segredos herdados) |
| `npx vitest run` (suíte completa) | 229/230 — única falha é `src/tools/dependency-rule.test.ts` (timeout de 5000ms), o flake pré-existente já rastreado em `REFAT-L0-01`/`REFAT-L4-02` (contenção de I/O/CPU sob carga concorrente, não regressão deste lote) |
| `npx vitest run src/tools/dependency-rule.test.ts` (isolado) | **4/4 verdes**, ~7s — confirma que a falha da suíte completa é contenção, não defeito de lógica; consistente com o padrão já registrado nos Lotes 0/1/4 |
| Consulta SQL direta (`pg_class.relrowsecurity` + contagem de `pg_policies` por tabela, via `npx supabase db query`) | **As 9 tabelas do lote com `rls_enabled = true`**: `profile` (3 políticas), `consent` (3), `plan_progress` (2), `preference` (4), `push_subscription` (4), `reminder_log` (1), `analytics_event` (2), `analytics_daily_aggregate` (**0 políticas** — confirma "sem nenhuma forma do cliente ler o agregado", TASK-031), `erasure_request` (2) |
| Consulta SQL direta (`information_schema.role_table_grants`, papéis `anon`/`authenticated`/`service_role`, todas as 9 tabelas) | GRANTs batem exatamente com o que cada migration documenta — nenhum GRANT de SELECT/INSERT/UPDATE/DELETE a `anon` em nenhuma tabela; `authenticated` só com as operações que cada tabela pretende (ver Seção 2, tarefa a tarefa); `service_role` só com o SELECT/UPDATE/INSERT explicitamente concedido onde `jobs/erasure`/`reminder_log`/apuração de `analytics` precisam. Achado de ambiente (não defeito de código, documentado na Seção 2/TASK-032/TASK-031): `anon`/`authenticated`/`service_role` têm `TRUNCATE`/`TRIGGER`/`REFERENCES` em **todas** as tabelas por um `ALTER DEFAULT PRIVILEGES` de plataforma do próprio Supabase (papel de migração `postgres`, confirmado via `pg_default_acl`), não introduzido nem removível por nenhuma migration deste lote — ver nota de risco na Seção 5 |
| Leitura direta das 6 migrations SQL, linha a linha | DI-03/G-09 confirmado nas 9 tabelas: `alter table ... enable row level security` está sempre na mesma migration que o `create table` correspondente, nunca numa migration separada posterior |

## 2. Veredito por Tarefa

### TASK-027 — Migration `profile` + `consent` com RLS

**Critério de aceite**: "Teste de política de RLS cobre as duas tabelas."

- `20260908175414_profile_consent_rls.sql`: `profile` (`user_id` PK, FK
  `auth.users on delete cascade`) e `consent` (`version`/`text_sha256`/
  `accepted_at`/`revoked_at`), campos batendo com `SDD.md` §5.3, nada
  inventado. RLS habilitada na mesma migration para as duas tabelas
  (confirmado por leitura direta e pela consulta SQL da Seção 1).
- Política `for select/insert/update ... using/with check (auth.uid() =
  user_id)` nas duas tabelas — isola de fato, não só existe: confirmado pelo
  teste real (`rls-profile-consent.test.mjs`, 6 casos) rodando contra dois
  usuários reais via GoTrue/PostgREST locais, não simulado — usuário B não lê
  nem atualiza linha de A, insert forjado com `user_id` de outro é rejeitado
  pelo `with check`, `anon` sem sessão não lê nada de nenhuma das duas
  tabelas.
- Sem política de `delete` para `authenticated` em nenhuma das duas — coerente
  com RN-11 (exclusão só via `erasure_request`/`service_role`, TASK-032).
- GRANT explícito de tabela confirmado necessário e presente (`select,
  insert, update` para `authenticated`, nada para `anon`) — reconfirmado
  pela consulta SQL direta da Seção 1, não só pelo comentário da migration.

**Veredito: Aprovado.**

### TASK-028 — Migration `plan_progress` (append-only, `UNIQUE(user_id, plan_id, day_number)`) com RLS

**Critério de aceite**: "Insert duplicado falha por constraint; RLS testada."

- `20260908180404_plan_progress_rls.sql`: `unique (user_id, plan_id,
  day_number)` presente exatamente como pedido, mais `check (day_number >
  0)`. RLS habilitada na mesma migration.
- **Insert duplicado falha por constraint, confirmado de forma real**: o
  teste (`rls-plan-progress.test.mjs`) grava `(user_id, plan_id,
  day_number)` uma vez, tenta de novo e recebe HTTP 409/`23505`
  (`unique_violation`) via chamada HTTP real ao PostgREST — não é uma
  asserção contra o schema em memória, é o comportamento do banco real
  observado via API. O mesmo dia em plano diferente e um dia diferente no
  mesmo plano são aceitos normalmente (confirma que a constraint é
  exatamente a chave composta pedida, nem mais larga nem mais estreita).
- Append-only confirmado nas duas camadas: sem política de update/delete
  **e** sem GRANT de update/delete para `authenticated` (consulta SQL da
  Seção 1: `authenticated` só tem INSERT/SELECT em `plan_progress`) — UPDATE/
  DELETE do próprio titular sobre a própria linha são rejeitados e a linha
  original permanece intacta, confirmado pelo teste.
- Isolamento por titular confirmado (usuário não lê linha de outro; `anon`
  sem sessão não lê nem grava nada).

**Veredito: Aprovado.**

### TASK-029 — Migration `preference` com RLS

**Critério de aceite**: "RLS testada."

- `20260908181000_preference_rls.sql`: PK composta `(user_id, key)`, LWW por
  substituição de linha (coerente com `PreferenceRecord` do cliente, Lote 3).
  RLS habilitada na mesma migration, 4 políticas (`select`/`insert`/`update`/
  `delete`, todas `auth.uid() = user_id`) — `preference` é a única tabela do
  lote com `delete` exposto a `authenticated`, coerente com `SDD.md` §7.2
  listar DELETE explicitamente para esta tabela.
- **`received_at` é sempre do relógio do servidor, nunca do cliente (RT-06)**:
  confirmado por leitura direta do trigger `preference_set_received_at`
  (`before insert or update`, `new.received_at := now()`, incondicional) e
  pelo teste real (`rls-preference.test.mjs`) que envia um `received_at`
  forjado 1 ano no futuro no corpo do insert e confirma que o valor
  persistido é o do relógio real do servidor, não o forjado.
- Isolamento por titular confirmado nas 4 operações (select/insert/update/
  delete cruzados entre dois usuários reais, `anon` sem sessão bloqueado).
- GRANT explícito confirmado (`select, insert, update, delete` para
  `authenticated`, nada para `anon`) via consulta SQL direta.

**Veredito: Aprovado.**

### TASK-030 — Migration `push_subscription` + `reminder_log` com RLS

**Critério de aceite**: "RLS testada em ambas."

- `20260908180500_push_subscription_reminder_log_rls.sql`: as duas tabelas
  na mesma migration, RLS habilitada nas duas. `push_subscription` com
  `unique (user_id, endpoint)` (deduplicação de dispositivo); `reminder_log`
  com `unique (user_id, scheduled_for)` (idempotência de envio, RT-07) e
  `outcome` restrito por `check` à lista fechada (`sent`/`failed`/`skipped`).
- `push_subscription`: 4 políticas (select/insert/update/delete) por
  titular — delete permitido aqui (desinscrever dispositivo é ação legítima
  e reversível do titular, sem relação com RN-11). Confirmado por teste real:
  usuário só grava/lê/atualiza/apaga a própria linha; insert forjado com
  `user_id` de outro rejeitado; `anon` sem sessão bloqueado.
- `reminder_log`: **só `select` para `authenticated`** (confirmado pela
  consulta SQL direta: `authenticated` não tem INSERT/UPDATE/DELETE nesta
  tabela) — escrita é exclusiva de `service_role` (TASK-057, ainda não
  implementada). Teste real confirma: seed via `service_role` funciona;
  cliente autenticado tentando inserir diretamente é rejeitado; cada titular
  só lê o próprio log, nunca o de outro.
- `grant select, insert, update on public.reminder_log to service_role`
  confirmado necessário e presente (sem esse GRANT, `service_role` não teria
  privilégio de tabela para gravar, apesar de ignorar RLS) — consistente com
  o achado de ambiente já registrado na Seção 1 (default ACL de `pg_default_acl`
  não inclui SELECT/INSERT/UPDATE por padrão para nenhum papel de API).

**Veredito: Aprovado.**

### TASK-031 — Migration `analytics_event` + `analytics_daily_aggregate` + função de incremento com RLS

**Critério de aceite**: "Cliente não lê o agregado; função de incremento
testada."

- `20260908182000_analytics_rls.sql`: **confirmado por 3 vias
  independentes** que o cliente não lê `analytics_daily_aggregate` de
  nenhuma forma — (1) leitura direta do SQL: nenhuma `create policy` de
  select para essa tabela, RLS habilitada sem nenhuma política (equivale a
  "ninguém acessa"); (2) consulta SQL direta desta rodada:
  `analytics_daily_aggregate` tem **0 políticas** em `pg_policies` e nenhum
  GRANT de SELECT para `anon`/`authenticated` em `information_schema.role_table_grants`
  (só `service_role`, usado exclusivamente para apuração server-side, nunca
  pelo bundle do cliente — GUARDRAILS G-10); (3) teste real
  (`rls-analytics.test.mjs`) chama `GET` direto na tabela via PostgREST com
  token de `anon` e de `authenticated` — ambos recebem erro de permissão,
  não uma lista vazia que poderia mascarar um SELECT parcial.
- **Função de incremento testada de fato**: `bump_metric` chamada via RPC
  real por `anon` e por `authenticated`, confirmando incremento relativo
  (`+1`/`+2` sobre o valor lido antes, via `service_role` só para fins de
  teste); `p_metric` fora da lista fechada do `CHECK` é rejeitado; `204 No
  Content` confirmado como resposta esperada do PostgREST para função
  `void` (achado documentado do próprio Executor, reconfirmado aqui).
- `analytics_event` (nível 2): RLS por titular, só select/insert (imutável
  uma vez recebido, mesmo racional de `plan_progress`) — isolamento
  confirmado por teste real (insert forjado rejeitado, `anon` bloqueado,
  titular lê só a própria linha).
- **Achado real, severidade baixa-média — sem cap de inserção por
  usuário/dia**: `SDD.md` §7.5 (linha "API de dados") exige literalmente
  "teto de inserção de eventos por usuário por dia, aplicado por gatilho no
  banco". Nem a migration de `analytics_event` nem nenhuma outra migration
  do repositório implementam esse gatilho — confirmado por grep em todas as
  6 migrations do lote (nenhuma ocorrência de lógica de contagem/teto). Não
  é uma lacuna do código entregue por TASK-031 contra o próprio critério de
  aceite dela (que fala só de "cliente não lê o agregado" e "função de
  incremento testada", ambos cumpridos) — é um requisito do SDD que não foi
  decomposto em nenhuma tarefa do `TASK.md` (busquei por "teto"/"rate
  limit"/"gatilho" em todo o `TASK.md` e no ADR-009, sem ocorrência
  relacionada). **Classificação: achado simples/débito de severidade
  baixa-média** — não compromete o critério de aceite central de TASK-031
  (que não menciona esse requisito) nem quebra nenhuma outra tarefa do lote;
  é uma proteção de disponibilidade/abuso de armazenamento, não uma falha de
  confidencialidade/RLS (isolamento por titular continua correto mesmo sem o
  teto). Gap isolado a uma linha específica de §7.5, não um padrão recorrente
  de decomposição — por isso vira tarefa em `Refatoração Lote-5`
  (`REFAT-L5-01`, `.md/TASK.md`) criada por este Validador, sem escalar ao
  `coordenador`.

**Veredito: Aprovado com ressalva** (débito registrado em `REFAT-L5-01`, não
bloqueia a tarefa nem o lote — ver Seção 5).

### TASK-032 — Migration `erasure_request` com RLS

**Critério de aceite**: "RLS testada."

- `20260908180432_erasure_request_rls.sql`: `unique (user_id)` (no máximo
  um pedido por titular), RLS habilitada na mesma migration, só select/insert
  para `authenticated` — sem update/delete, exatamente o requisito "usuário
  não pode alterar `due_at`/status arbitrariamente".
- **`due_at = requested_at + 15 dias` (RN-11) confirmado calculado pelo
  servidor, não aceito do cliente**: leitura direta do trigger
  `set_erasure_request_due_at` (`before insert`, sempre sobrescreve
  `new.due_at`) e teste real (`rls-erasure-request.test.mjs`) que envia um
  `due_at` forjado no corpo do insert e confirma que o valor persistido é o
  recalculado, não o forjado.
- Cliente não consegue alterar `completed_at` via PATCH (403, bloqueio de
  GRANT de tabela — `authenticated` não tem UPDATE nesta tabela, confirmado
  pela consulta SQL direta); `service_role` consegue preencher
  `completed_at` (GRANT explícito `select, update` confirmado necessário e
  presente — sem ele, `jobs/erasure`/TASK-041 não teria acesso apesar de
  `service_role` ignorar RLS).
- Isolamento por titular confirmado (`anon` sem sessão não lê nada; usuário
  não cria pedido em nome de outro nem lê pedido de outro).

**Veredito: Aprovado.**

### TASK-034 — Headers de segurança: CSP, HSTS, Permissions-Policy, Referrer-Policy (§7.5)

**Critério de aceite**: "Teste de headers HTTP confirma presença de todos."

- `src/tools/security/securityHeaders.ts`: os 5 headers (CSP, HSTS,
  Referrer-Policy, Permissions-Policy, X-Content-Type-Options) confirmados
  por leitura direta com os valores exatos exigidos por `SDD.md` §7.5 —
  `default-src 'self'`, `frame-ancestors 'none'`, `connect-src` restrito a
  `'self'` + domínio do backend (único wildcard da policy inteira,
  confirmado por teste dedicado), HSTS com `max-age` ≥ 1 ano +
  `includeSubDomains` + `preload`, `Referrer-Policy: no-referrer` exato,
  `Permissions-Policy` negando as 3 APIs não usadas.
- **`script-src` sem `unsafe-inline`/`unsafe-eval`** (o risco real que CSP
  mitiga) confirmado — `script-src 'self'` exato, testado por regex
  dedicada, distinto de `style-src`.
- **`style-src 'self' 'unsafe-inline'` não quebra os primitivos do Design
  System (Lote 4)**: confirmado por leitura direta dos 5 primitivos
  (`src/design-system/primitives/{Botao,CampoDeTexto,CampoDeSenha,
  SeletorDeHora,Alternador}.tsx`) — todos usam `style={{...}}` do React
  (`TOUCH_TARGET_MIN_STYLE`, DI-08), que vira atributo `style=""` inline no
  DOM. Sob `style-src 'self'` estrito (sem a concessão), o navegador
  bloquearia esse atributo assim que uma tela real (Lote 6+) importasse
  esses primitivos, quebrando DI-08 silenciosamente — achado real do próprio
  Executor (correção pós-revisão inline documentada na própria migration/
  `TASK.md`), confirmado correto por este Validador: a concessão está restrita
  a `style-src`, nunca vaza para `script-src`.
- **Resposta HTTP real confirmada, não só constante em memória**: teste
  sobe um servidor `node:http` com o mesmo middleware do plugin Vite e faz
  `fetch()` real, confirmando os 5 headers na resposta HTTP — cumpre a letra
  do critério de aceite ("teste de headers HTTP").
- `dist/_headers` (formato Cloudflare Pages, ADR-008) confirmado gerado por
  `npm run build` desta rodada, com as 5 linhas de header corretas.
- Domínio de `connect-src` é `https://*.supabase.co` (wildcard), documentado
  como decisão temporária até o projeto Supabase de produção ser
  provisionado (TASK-005 só cobre local) — decisão de portabilidade
  aceitável nesta fase, não um achado (não há projeto real ainda para
  restringir a um domínio exato); registrado aqui como item de atenção para
  o chapéu DevOps antes do go-live (ver Seção 4).

**Veredito: Aprovado.**

## 3. Testes de Integração Cruzada do Lote

- **TASK-027 (bootstrap) × TASK-028/029/030/031/032 (migrations
  paralelas)**: todas as 6 migrations aplicadas em sequência por
  `npx supabase db reset` sem conflito de nome de política/índice/trigger/
  função entre tabelas de tarefas diferentes — confirmado nesta rodada com
  as 6 migrations já publicadas simultaneamente (diferente das notas do
  Executor, que rodaram sob contenção real de ambiente com instâncias
  paralelas; este Validador rodou a suíte completa de uma vez, sem
  contenção, e confirma 47/47 verdes de forma estável).
- **`grant usage on schema public to authenticated`/`to anon`**: concedido
  de forma idempotente em mais de uma migration (`profile_consent_rls`,
  `plan_progress_rls`, `push_subscription_reminder_log_rls`,
  `erasure_request_rls`, `analytics_rls`) — confirmado que `grant usage`
  repetido não falha nem gera warning no `db reset` (Postgres trata GRANT
  como idempotente por natureza), não é um defeito de coordenação entre as
  tarefas paralelas.
- **TASK-031 (`analytics_event`) × TASK-032 (`erasure_request`)**: a
  cascata de exclusão de RN-11 depende de `on delete cascade` em
  `auth.users` para ambas as tabelas (entre outras) — confirmado por leitura
  direta que as duas migrations usam exatamente o mesmo padrão de FK
  (`user_id uuid not null references auth.users (id) on delete cascade`),
  então a cascata funcionará de forma consistente quando `jobs/erasure`
  (TASK-041, fora deste lote) apagar o usuário em `auth.users` — não testado
  fim a fim nesta rodada (TASK-041 não existe ainda), mas o pré-requisito
  estrutural (FK com cascade) está presente e correto em todas as 7 tabelas
  do lote.
- **TASK-034 (headers) × Lote 4 (Design System)**: já detalhado no veredito
  de TASK-034 acima — confirmado que `style-src 'self' 'unsafe-inline'` não
  quebra os primitivos do Lote 4, cruzando os dois lotes de forma real (não
  apenas por nota do Executor).
- **DI-03 (RLS na mesma migration) verificado nas 9 tabelas de uma vez**:
  consulta SQL direta desta rodada (Seção 1) confirma `rls_enabled = true`
  para as 9 tabelas simultaneamente, não tabela por tabela isoladamente —
  nenhuma tabela do lote ficou exposta sem RLS em nenhum momento entre
  migrations.

## 4. Requisitos Não Funcionais Relevantes ao Lote

- **RT-04 (tabela sem RLS vaza dado sensível)**: mitigado nas 9 tabelas —
  confirmado por leitura direta e por consulta SQL, não só pela nota do
  Executor.
- **RT-06 (relógio de dispositivo divergente)**: `received_at` do servidor
  confirmado como fonte de verdade em `preference` (trigger) e
  `analytics_event` (`default now()`), nunca aceito do cliente.
- **RT-07 (envio duplicado de lembrete)**: `unique (user_id, scheduled_for)`
  em `reminder_log` confirmado como a constraint que impede reagendamento
  duplicado.
- **RT-10 (falsificação de métrica)**: `reminder_log` e
  `analytics_daily_aggregate` confirmados sem caminho de escrita direta do
  cliente — só `service_role`/`bump_metric` (`SECURITY DEFINER`) escrevem.
- **G-18 (CSP/HSTS/Permissions-Policy/Referrer-Policy testados por
  automação)**: confirmado — 2 arquivos de teste dedicados, incluindo
  resposta HTTP real (não decorativo).
- **Bundle de produção**: `dist/_headers` gerado corretamente; nenhum
  segredo de serviço no bundle (`npm run test:security`, 11/11 verdes,
  incluindo checagem real contra `dist/`).

## 5. Fechamento Estrutural do Lote (checagem do próprio Validador)

- Status antes desta rodada: TASK-027, TASK-028, TASK-029, TASK-030,
  TASK-031, TASK-032 e TASK-034 estavam `Concluída` no `TASK.md`. TASK-033
  estava (e segue) `Não iniciada`.
- Após esta rodada: **7 de 7 tarefas desta rodada aprovadas** (TASK-031 com
  ressalva registrada em `REFAT-L5-01`; TASK-027, TASK-028, TASK-029,
  TASK-030, TASK-032, TASK-034 sem ressalva). Nenhuma reprovação crítica nem
  simples.
- **TASK-033 permanece fora desta rodada, por instrução explícita e por
  não estar `Concluída`**: sua "Depende de" (TASK-028, TASK-029, TASK-030,
  TASK-031, TASK-032) está satisfeita — todas as 5 já `Concluída` e
  aprovadas — então TASK-033 está desbloqueada e elegível para `/executar`,
  mas ainda não foi implementada. **O Lote 5 como um todo (Definition of
  Done do lote completo) não fecha enquanto TASK-033 não completar e for
  validada** — isso é consistente com a decomposição do `TASK.md` (TASK-033
  é a última peça do lote, dependente de quase todo o resto por design,
  RP-05) e não é uma inconsistência estrutural; é simplesmente uma tarefa
  do lote ainda na fila.
- Dependências da Seção 3/4 do `TASK.md` relativas às 7 tarefas desta
  rodada: TASK-027→TASK-005 (satisfeita, Lote 0 aprovado); TASK-028/029/
  030/031/032→TASK-027 (satisfeita, todas paralelas entre si); TASK-034→
  TASK-005 (satisfeita). Cadeia conferida uma a uma, nenhuma órfã ou
  inconsistente.
- Nenhuma tarefa `Bloqueada` sem resolução neste lote.
- **1 achado simples/débito criado em `Refatoração Lote-5`** (severidade
  baixa-média, não bloqueia o lote): `REFAT-L5-01` — `analytics_event` sem
  o teto de inserção diário por usuário exigido por `SDD.md` §7.5 (ver
  Seção 2, TASK-031, para o detalhamento completo). Prazo: antes do go-live
  de produção.
- Nenhuma inconsistência encontrada que exija redesenho de dependência ou
  decomposição do Lote 5 em si — não há escalonamento ao `coordenador`. O
  gap de `REFAT-L5-01` é a ausência de um requisito específico de §7.5 em
  qualquer critério de aceite do `TASK.md`, isolado a uma linha da tabela de
  superfície de exposição, não um padrão recorrente de tarefas mal
  decompostas neste lote (as demais 6 tarefas cobrem exatamente o que seus
  próprios critérios de aceite pedem, e esses critérios batem com o SDD).

## 6. Veredito Geral do Lote 5 (rodada TASK-027 a TASK-032, TASK-034)

**7 de 7 tarefas desta rodada: Aprovadas** (TASK-031 aprovada com ressalva
registrada em `REFAT-L5-01`; as demais 6 sem ressalva). Nenhuma reprovação
crítica ou simples em aberto. **TASK-033 segue `Não iniciada` — o Lote 5
como um todo ainda não fecha.** Definition of Done por lote (chapéu QA),
aplicada às 7 tarefas desta rodada:

- [x] Todo critério de aceite de cada uma das 7 tarefas foi testado e está
      passando
- [x] Nenhuma reprovação crítica em aberto
- [x] Toda reprovação simples virou tarefa em `Refatoração Lote-5` (nenhuma
      reprovação de tarefa nesta rodada; `REFAT-L5-01` é débito de requisito
      não decomposto, não reprovação de nenhuma das 7 tarefas)
- [x] Testes de integração cruzada executados e passando (Seção 3)
- [x] Requisito não funcional relevante ao lote validado (Seção 4)
- [ ] **Lote 5 completo** — pendente de TASK-033 (fora desta rodada)

As 7 tarefas desta rodada **estão prontas** para a auditoria de segurança do
chapéu DevSecOps — ver `.md/SECURITY-REVIEW.md`, seção "Lote 5" — e, após
dupla aprovação (QA + DevSecOps), para o chapéu DevOps prosseguir com deploy
dessas 7 tarefas (junto dos Lotes 0-4, se ainda não implantados). O achado
`REFAT-L5-01` deve ser considerado pelo chapéu DevSecOps nesta mesma
auditoria, por tocar diretamente um requisito de segurança operacional do
SDD.md §7.5, ainda que a classificação de severidade do chapéu QA
(baixa-média, não bloqueante) já tenha sido justificada acima.

Nenhum padrão recorrente de bug que sugira problema de decomposição ou
diretriz de implementação foi observado nas 7 tarefas — todas passaram com
evidência de teste genuína contra Supabase local real (chamadas HTTP reais
via PostgREST/GoTrue, não simulação), consultas SQL diretas confirmando
RLS/GRANT, e um achado isolado de requisito não decomposto (`REFAT-L5-01`),
não um padrão recorrente entre as 7 tarefas; não escala ao `coordenador`.

## 7. TASK-033 e Fechamento do Lote 5

**Contexto**: rodada anterior (Seções 1-6 acima) validou TASK-027 a TASK-032
e TASK-034 (7/7 aprovadas), deixando TASK-033 (teste de catálogo de RLS,
RT-04/DI-12) explicitamente fora, por não estar `Concluída` ainda. TASK-033
agora está `Concluída` em `.md/TASK.md` — esta seção valida especificamente
essa tarefa e fecha o Lote 5 por completo (8/8).

### 7.1 Validação funcional de TASK-033

Critério de aceite literal: "CI falha ao adicionar tabela sem RLS de
propósito". Validado de forma independente, sem usar a nota de implementação
do Executor como base de aprovação — só como onde olhar:

- **Consulta genérica, sem lista hardcoded**: lida `tests/db/rls-catalog.test.mjs`
  linha a linha. A função `tablesWithUserIdColumn` faz `join` de
  `pg_catalog.pg_class`/`pg_catalog.pg_namespace` (`relrowsecurity`) com
  `information_schema.columns` (existência de coluna `user_id`), varrendo
  TODA tabela (`relkind = 'r'`) de `public` a cada execução — nenhum nome de
  tabela aparece hardcoded no código de produção do teste (o único lugar que
  cita nomes de tabela é o comentário de topo, em prosa, para explicar a
  decisão de design). Confirmado: uma tabela criada por uma migration futura
  entraria na varredura automaticamente, sem precisar editar este arquivo.
- **Rodada real da suíte**: `npm run test:db` executado contra o Supabase
  local (Docker já up, `SERVICE_ROLE_KEY` de `supabase status` idêntico ao
  hardcoded em CI, ver §7.2 abaixo) → **49/49 verdes**, incluindo os 2 testes
  de `rls-catalog.test.mjs` ("catálogo" e "autoteste") e os 47 preexistentes
  dos Lotes 5 (TASK-027 a TASK-032), sem regressão.
- **Prova empírica reproduzida de forma independente** (não confiei só no
  relato do Executor nem no autoteste do próprio arquivo): criei, via
  conexão `pg` direta ao Postgres local, uma tabela real
  `public.__validador_proof_offender` (`user_id uuid not null`, sem
  `enable row level security`) fora de qualquer migration versionada. Rodei
  `node --test tests/db/rls-catalog.test.mjs` de novo:
  - **Falhou de fato**, `exit code 1`, `AssertionError` listando
    exatamente `{"table_name":"__validador_proof_offender","rls_enabled":false}`
    como ofensora — não é asserção vazia, é detecção real contra o catálogo.
  - Removida a tabela de prova (`drop table`) e a suíte confirmada verde de
    novo (2/2) na sequência, sem resíduo.
  - Isso corrobora, de forma independente do "autoteste" já embutido na
    própria suíte (que também roda esse mesmo ciclo criar/detectar/habilitar
    RLS/confirmar sumiço e passou nas 49/49 acima), que a lógica de detecção
    funciona contra o catálogo real, não é uma simulação.
- **`.github/workflows/ci.yml` — validado como YAML e como sequência de
  steps** (mesmo tipo de checagem já feita para TASK-004 no Lote 0): arquivo
  parseado com sucesso como YAML válido (`yaml.safe_load`), sem erro de
  sintaxe. Sequência de steps confirmada coerente: `Setup Supabase CLI`
  (`supabase/setup-cli@v1`) → `Start Supabase local (Docker) and apply
  migrations` (`supabase start`, sobe Postgres/PostgREST/GoTrue e aplica as
  migrations do repo) → `DB tests (RLS, incl. gate de catálogo RT-04 —
  TASK-033)` (`npm run test:db`, com `SUPABASE_DB_URL`/`SUPABASE_URL`/
  `SUPABASE_SERVICE_ROLE_KEY` injetados via `env:` do step) → `Stop Supabase
  local` com `if: always()` (evita vazar containers entre execuções do
  runner, mesmo em falha). Posicionado corretamente entre "Unit tests" e
  "Build" — antes do gate de RLS falhar, o job já teria barrado em lint/
  typecheck/unit tests; depois dele, build/E2E só rodam se o gate de RLS
  passar. Nenhum erro de indentação, chave duplicada ou referência de step
  quebrada.
- **Critério de aceite coberto de fato**: antes desta tarefa, `test:db` já
  existia como script (`package.json`) mas nunca era invocado por nenhum
  workflow — rodava só localmente, se alguém lembrasse. Agora está plugado
  em CI como gate real: qualquer PR que adicione tabela com `user_id` sem
  `enable row level security` faz o job de CI falhar no step "DB tests",
  antes do "Build" — exatamente o que "CI falha ao adicionar tabela sem RLS
  de propósito" pede, literalmente, não uma interpretação estendida do
  critério. Mesmo achado de padrão já registrado para TASK-004 (Lote 0):
  "teste que só roda localmente não cumpre critério de aceite que fala de
  CI" — aqui corrigido da mesma forma.

**Veredito TASK-033: Aprovada, sem ressalva.**

### 7.2 Nota de suporte à auditoria do chapéu DevSecOps (não substitui §8 de `SECURITY-REVIEW.md`)

Confirmei, como checagem de suporte ao próprio chapéu DevSecOps (que audita
formalmente em `SECURITY-REVIEW.md`, seção "Lote 5", subseção "TASK-033 e
Fechamento do Lote 5"): a chave `SUPABASE_SERVICE_ROLE_KEY` hardcoded em
`ci.yml` é byte-a-byte idêntica ao `SERVICE_ROLE_KEY` que `npx supabase
status` imprime para este projeto Supabase local (`supabase_db_EstudoBiblico`
rodando no Docker local no momento desta validação) — decodificado, o JWT
tem payload `{"iss":"supabase-demo","role":"service_role","exp":1983812996}`,
o mesmo `iss: supabase-demo` documentado publicamente pelo próprio Supabase
como chave fixa de demonstração para ambiente local (não uma credencial de
projeto real, que teria `iss` apontando para uma ref de projeto específica).
`npm run test:security` rodado (11/11 verdes), incluindo o teste "bundle
real do cliente (dist/) não contém segredo de serviço" — não pulado (`dist/`
já existe), confirmando que a chave não vaza para o bundle publicado.

### 7.3 Checagem estrutural e fechamento do Lote 5

- **TASK-033 `Concluída`** em `.md/TASK.md` (linha da tabela do Lote 5),
  confirmado.
- **Dependências satisfeitas**: "Depende de" de TASK-033 é TASK-028,
  TASK-029, TASK-030, TASK-031, TASK-032 — todas as 5 já `Concluída` e
  aprovadas na rodada anterior (Seções 1-6 acima). Nenhuma dependência
  órfã ou inconsistente relativa a este lote na Seção 4 do `TASK.md`
  (reconferido: TASK-041 e TASK-069, de lotes posteriores, dependem de
  TASK-033 — agora corretamente desbloqueadas para suas próprias rodadas,
  não é uma inconsistência deste fechamento).
- **Nenhuma tarefa `Bloqueada` sem resolução** no Lote 5.
- **As 8 tarefas do Lote 5 (TASK-027 a TASK-034) estão todas `Concluída`**,
  confirmado linha a linha na Seção 3 do `TASK.md`.
- **`Refatoração Lote-5`**: `REFAT-L5-01` já existente (débito de TASK-031,
  teto de inserção diário em `analytics_event`, SDD §7.5) — nenhum achado
  novo de TASK-033 exige item adicional; não duplicado.
- Nenhuma inconsistência encontrada que exija redesenho de dependência ou
  decomposição — não há escalonamento ao `coordenador` neste fechamento.

**Lote 5 fechado estruturalmente pelo Validador — sem dispatch ao
`coordenador`.**

## 8. Veredito Geral do Lote 5 — Fechamento Completo (8/8 tarefas)

**8 de 8 tarefas do Lote 5: Aprovadas** (TASK-031 com ressalva registrada em
`REFAT-L5-01`; as demais 7, incluindo TASK-033, sem ressalva). Nenhuma
reprovação crítica ou simples em aberto.

Definition of Done por lote (chapéu QA), aplicada ao Lote 5 completo:

- [x] Todo critério de aceite de cada uma das 8 tarefas foi testado e está
      passando
- [x] Nenhuma reprovação crítica em aberto
- [x] Toda reprovação simples virou tarefa em `Refatoração Lote-5`
      (`REFAT-L5-01`; nenhuma reprovação de tarefa em nenhuma das 8)
- [x] Testes de integração cruzada executados e passando
- [x] Requisito não funcional relevante ao lote validado
- [x] **Lote 5 completo**

**O Lote 5 fecha como Validado (com ressalva registrada em `REFAT-L5-01`,
prazo antes do go-live de produção — débito de baixa-média severidade, não
bloqueante).** Segue para a auditoria completa do chapéu DevSecOps sobre as
8 tarefas (ver `.md/SECURITY-REVIEW.md`, seção "Lote 5", subseção "TASK-033
e Fechamento do Lote 5") e, após dupla aprovação (QA + DevSecOps), para o
chapéu DevOps prosseguir com deploy.

Nenhum padrão recorrente de bug observado em TASK-033 nem no fechamento do
lote; não escala ao `coordenador`.

---

# Lote 6 — Identidade (TASK-035 a TASK-040)

- **Data**: 2026-09-08
- **Escopo desta rodada**: as 6 tarefas do Lote 6, todas marcadas `Concluída`
  no `TASK.md` — TASK-035 (cadastro), TASK-036 (T-08 + `BlocoConsentimento`),
  TASK-037 (T-09, entrar/recuperar), TASK-038 (sessão/DI-10), TASK-039 (T-13,
  exportação) e TASK-040 (T-13, exclusão). Inclui também a confirmação da
  correção pós-fechamento de TASK-034 (Lote 5) — regressão de CSP em dev
  achada durante este lote.
- **Base**: `.md/TASK.md` (Seção 3, Lote 6), `.md/SDD.md` §7.1 (autenticação),
  §7.5 (superfície de exposição), §7.6 (LGPD — consentimento/portabilidade/
  exclusão), `.md/UX-SPEC.md` (T-08, T-09, T-13), `.md/GUARDRAILS.md` (G-09 a
  G-11, G-17, G-18), `.md/TASK.md` Seção 1 (DI-06, DI-07, DI-08, DI-09, DI-10,
  DI-14).
- **Método**: todo comando abaixo foi executado de forma independente neste
  ambiente (Supabase local já em execução, reaproveitado — `npx supabase
  status` confirmado de pé antes de começar), sem confiar nas notas do
  Executor. Leitura direta de todo o código de `src/features/identity/`, da
  migration nova (`20260908190000_profile_on_signup_trigger.sql`) e da
  correção pós-fechamento de TASK-034 em `src/tools/security/
  {securityHeaders.ts,vitePluginSecurityHeaders.ts}` — as notas do Executor no
  `TASK.md` serviram só de referência de onde olhar. Requisições HTTP reais
  feitas diretamente contra o GoTrue local (`/auth/v1/token?grant_type=
  password`) para confirmar, com ferramenta própria e não com o e2e já
  escrito, que login com e-mail inexistente e login com senha errada
  devolvem exatamente a mesma resposta (mesmo `status`/`error_code`/`msg`).

## 1. Execução Independente — Evidência

| Comando | Resultado |
|---|---|
| `npx supabase status` | Já em execução (ambiente herdado); nenhuma inicialização necessária |
| `npm run lint` (eslint + `lint:deps`) | Limpo — `"no dependency violations found"` (171 módulos, 469 dependências) |
| `npm run typecheck` (`tsc -b --noEmit`) | Limpo, sem erro |
| `npm run build` (`tsc -b && vite build`) | Limpo — 19 módulos (shell ainda placeholder sem roteador; `features/identity` corretamente ainda fora do bundle, nenhuma tela real a importa, esperado e documentado desde TASK-035); gera `dist/_headers` com a CSP completa (ver Seção 2, TASK-034/correção) |
| `npx vitest run --testTimeout=20000` | **325/325 verdes** (52 arquivos) — nenhuma falha, nenhum timeout intermitente de `REFAT-L0-01` nesta rodada |
| `npx playwright test` (suíte completa) | **11/11 verdes** — `identity-signup` (1), `identity-signin` (3), `identity-erasure` (2), `identity-export-data` (2), `placeholder` (2), `security-headers-dev` (1, confirma a correção pós-fechamento de TASK-034) |
| `npm run test:security` (`node --test tests/security/*.test.mjs`) | 11/11 verdes — bundle sem string proibida (DI-05) e sem segredo de serviço (DI-09/G-10), sem regressão |
| `npm run test:db` (`node --test tests/db/*.test.mjs`) | **47/49 verdes — 2 falhas reais, deterministas, não-flake** (ver Seção 2/3, achado crítico ligado a TASK-035); reproduzido isolado (`node --test tests/db/rls-profile-consent.test.mjs`) com o mesmo resultado |
| Requisição HTTP direta (`fetch` a `/auth/v1/token?grant_type=password`), e-mail sem conta vs. senha errada de conta inexistente | Resposta idêntica nos dois casos: `400 {"code":400,"error_code":"invalid_credentials","msg":"Invalid login credentials"}` — confirma o critério de aceite central de TASK-037 de forma independente do e2e já escrito |
| Leitura direta de `dist/_headers` após `npm run build` | CSP completa presente (`script-src 'self'` sem `unsafe-inline`/`unsafe-eval`; `style-src 'self' 'unsafe-inline'`), confirmando que a correção pós-fechamento de TASK-034 (ver Seção 5) não reabriu o achado original |

## 2. Achado Crítico 1 — Regressão determinística em `npm run test:db` (gate de CI, TASK-033/Lote 5)

**Severidade: crítica.** `tests/db/rls-profile-consent.test.mjs` (teste da
TASK-027, já `Validado` no Lote 5, e gate de CI real desde TASK-033) falha de
forma determinística, não uma vez, mas reproduzida 2x (suíte completa e
arquivo isolado):

```
test at tests\db\rls-profile-consent.test.mjs:137:11
✖ profile: usuário só grava a própria linha
  AssertionError: insert da própria linha deveria ter sucesso:
  {"code":"23505","message":"duplicate key value violates unique
  constraint \"profile_pkey\""}
  409 !== 201
```

**Causa raiz**: a migration nova desta tarefa,
`supabase/migrations/20260908190000_profile_on_signup_trigger.sql`
(TASK-035), cria o gatilho `on_auth_user_created` (`after insert on
auth.users`), que insere automaticamente uma linha em `public.profile` para
**todo** usuário criado — inclusive os criados via `/auth/v1/admin/users`
pelos testes de RLS de outras tarefas (Lote 5), não só via `signUp` do
cliente. O teste de `rls-profile-consent.test.mjs` (escrito na época de
TASK-027, antes deste gatilho existir) ainda assume o modelo antigo — que o
próprio teste faz o primeiro INSERT em `profile` para o usuário que acabou de
criar — e por isso recebe `409` (linha já existe, criada pelo gatilho) em vez
do `201` esperado.

**Por que isto não foi pego pelo Executor**: nenhuma das notas de
implementação de TASK-035 a TASK-040 no `TASK.md` menciona ter rodado `npm
run test:db` (grep confirmado — só aparece nas notas de TASK-031/TASK-033,
Lote 5). TASK-035 confirma a criação da linha de `profile` só via `supabase
db reset` manual e via o e2e novo desta tarefa, nunca rodando a suíte
completa de regressão de banco que já existia.

**Por que é crítico, não um débito registrável**: `npm run test:db` é gate
real de CI desde TASK-033 (`.github/workflows/ci.yml`) — toda PR futura terá
esse step vermelho até isto ser corrigido, bloqueando merge de qualquer
trabalho, não só deste lote. Não é uma reprovação isolada de um critério de
aceite de uma tarefa do Lote 6 (nenhum dos 6 critérios de aceite deste lote
menciona esse teste), mas quebra algo que **todas** as tarefas futuras do
projeto dependem (o gate de CI) — mesmo raciocínio de "quebra algo que outra
tarefa depende" da definição de crítica, estendido ao gate compartilhado do
projeto.

**Nota**: as demais asserções do mesmo bloco de teste (`profile: usuário não
lê linha de outro titular`, `profile: usuário não atualiza linha de outro
titular`) continuam verdes — a política de RLS de `profile` em si **não**
está quebrada, é a asserção específica de "insert cria a linha" que ficou
obsoleta frente ao novo mecanismo (gatilho). Isolamento por titular
permanece intacto e confirmado.

## 3. Achado Crítico 2 — Consentimento não é "garantido por política no banco" (SDD.md §7.6)

**Severidade: crítica (compliance obrigatório, LGPD art. 5º II).**
`SDD.md` §7.6 exige, em texto explícito e sem ambiguidade:

> Base legal: consentimento específico e destacado (RNF-05, CA-10.2) [...]
> **Nenhuma escrita de dado pessoal no servidor ocorre antes de existir a
> linha de consentimento — garantido por política no banco, não por ordem
> de chamadas no cliente.**

A implementação atual (TASK-035 + TASK-036) faz exatamente o que o SDD
explicitamente proíbe como mecanismo de garantia:

1. **TASK-035** (`handle_new_auth_user`, gatilho `security definer` `after
   insert on auth.users`) cria a linha de `profile` **incondicionalmente**,
   para todo usuário novo, sem nenhuma checagem de que `consent` existe ou
   vai existir. Não há política de banco (trigger/constraint/FK) que
   impeça essa escrita na ausência de consentimento.
2. **TASK-036** (`signUpWithConsent`, `sign-up-with-consent.ts`) implementa a
   garantia de ordem **inteiramente no cliente**: chama `signUpWithEmailPassword`
   e, "imediatamente em seguida, como o próximo passo do mesmo fluxo", chama
   `recordConsent`. A própria documentação do código reconhece a tensão (a
   leitura literal "antes do `signUp`" é impossível, pois `consent.user_id`
   referencia `auth.users.id`) mas resolve com ordenação de chamadas no
   cliente — que é **exatamente** o mecanismo que o SDD diz não bastar.

**Consequência real, não só teórica**: se `signUp` tiver sucesso e a chamada
seguinte a `recordConsent` falhar (queda de rede, aba fechada, crash do JS
entre as duas chamadas — nenhum retry/transação as une), fica uma conta real
persistida em `auth.users` + uma linha em `profile`, **sem nenhuma linha de
`consent`**, e nada no banco corrige ou impede isso. O código trata esse caso
como falha de cadastro para o usuário (`"consent-failed"`), mas o estado
inconsistente já foi gravado no servidor — a UI "esconde" a falha, o banco
não a impede.

**Por que isto é o achado central, não uma leitura excessivamente literal**:
o próprio SDD antecipa a objeção óbvia ("mas `consent.user_id` só pode
existir depois de `auth.users`") ao dizer explicitamente que a garantia deve
vir de **política no banco**, não de ordem no cliente — ou seja, o SDD já
sabia que "antes do `signUp`" é impossível e mesmo assim optou por exigir
enforcement no banco, não abrir uma exceção para ordenação no cliente.

**Direção de correção recomendada (decisão de implementação, não imposta)**:
o mecanismo mais direto para cumprir o requisito literal é enviar a aceitação
do consentimento (`version`, `text_sha256`) como metadata do próprio
`signUp` (`options.data`, suportado pelo Supabase Auth) e estender o mesmo
gatilho `security definer` já existente (`handle_new_auth_user`) para
inserir **`profile` e `consent` na mesma transação** que cria a linha em
`auth.users` — eliminando por completo a dependência de duas chamadas HTTP
separadas do cliente. Isso resolve simultaneamente o Achado Crítico 1 (Seção
2): a suíte `rls-profile-consent.test.mjs` precisa ser atualizada de qualquer
forma para o novo mecanismo de criação de `profile`.

**Não é redesenho de dependência/decomposição do Coordenador** — é correção
de implementação para satisfazer um requisito de arquitetura já decidido e
já escrito no SDD.md; fica com o `executor`, não escala ao `coordenador`.
Escala ao `gestor` em paralelo (Seção 6), por ser achado de compliance com
relevância estratégica (LGPD).

## 4. Veredito por Tarefa

### TASK-035 — Cadastro e-mail+senha com verificação

**Critério de aceite**: "e2e cria conta e recebe verificação mock."

- Confirmado independentemente: `npx playwright test
  e2e/identity-signup.spec.ts` verde; leitura de `sign-up.ts`/
  `confirm-signup.ts`/`auth-gateway.ts` confirma que só a `anon key` é usada
  (DI-09), nunca `service_role`; e2e usa Mailpit real (porta 54424) para
  extrair o token de verificação, não um mock em memória — cumpre "recebe
  verificação mock" no sentido do UX-SPEC (ambiente local simula o e-mail
  real via Mailpit, não um stub que pula a chamada de rede).
- **Reprovação crítica** (Seção 3): o gatilho `handle_new_auth_user` desta
  tarefa é metade da causa raiz do Achado Crítico 2 (cria `profile` sem
  checar consentimento) e a causa raiz integral do Achado Crítico 1
  (regressão em `test:db`). O critério de aceite explícito da tarefa
  ("e2e cria conta e recebe verificação mock") está cumprido isoladamente,
  mas a tarefa não pode ficar `Concluída` porque seu artefato central (o
  gatilho de criação de `profile`) precisa ser redesenhado junto da correção
  do Achado Crítico 2 — voltam juntas ao `executor`.

**Veredito: Reprovada (crítica) — volta para `executor`.**

### TASK-036 — Tela T-08 (Criar conta) + `BlocoConsentimento`

**Critério de aceite**: "Teste de acessibilidade 3.3.2/1.3.1; grava
`consent` antes de qualquer dado pessoal."

- Acessibilidade confirmada por leitura direta de `BlocoConsentimento.tsx`:
  `fieldset`/`legend` (1.3.1, relação estrutural real, não só posição
  visual), rótulo do `Alternador` é o texto de consentimento completo e
  sempre visível — nunca placeholder/tooltip (3.3.2), "ler o texto completo"
  é um `button[aria-expanded]` nativo (navegável por teclado sem handler
  escrito à mão), erro com ícone **e** texto (DI-07) e `role="alert"`. Sem
  `axe`/`jest-axe` integrado ao projeto (honesto sobre a lacuna, já
  registrado pelo próprio Executor como observação para DI-14) — a checagem
  estrutural via Testing Library é uma aproximação razoável, não uma
  substituição plena de um auditor de acessibilidade automatizado; ver
  `REFAT-L6-02`.
- "Grava `consent` antes de qualquer dado pessoal": **este é exatamente o
  Achado Crítico 2** (Seção 3) — a garantia existe só como ordem de chamadas
  no cliente (`signUpWithConsent`), não como política de banco, contrariando
  o texto explícito do `SDD.md` §7.6. A interpretação do Executor (credencial
  de identidade ≠ dado pessoal sensível de RNF-05) é razoável **como
  argumento de escopo do que é "dado pessoal"**, mas não resolve o requisito
  de **mecanismo** ("garantido por política no banco") — os dois são
  requisitos distintos do mesmo parágrafo do SDD, e só o segundo foi
  deixado sem solução.
- Os 4 estados de `TelaCriarConta` (DI-06) e os 26 testes novos (todos
  verdes) cobrem corretamente o que foi implementado — o problema não é
  cobertura de teste insuficiente, é a garantia estrutural que falta.

**Veredito: Reprovada (crítica) — volta para `executor`, junto de TASK-035**
(mesma correção, mesmo gatilho).

### TASK-037 — Tela T-09 (Entrar/recuperar acesso)

**Critério de aceite**: "e2e de login, recuperação e mensagem de erro sem
revelar existência do e-mail."

- Confirmado de forma independente (Seção 1): requisição HTTP direta contra
  `/auth/v1/token?grant_type=password`, fora do e2e já escrito, devolve
  resposta byte-a-byte idêntica para e-mail inexistente e senha errada.
- Leitura de `classify-auth-error.ts` confirma o desenho correto: a
  classificação nunca inspeciona o texto da mensagem do servidor para
  diferenciar os dois casos — normalização acontece inteiramente no
  cliente, não depende de o backend já devolver mensagem genérica hoje
  (defesa contra uma futura mudança de comportamento da API).
  `sign-in-with-magic-link.ts`/`request-password-reset.ts` colapsam
  qualquer erro que não seja `rate-limited`/`network-error` no mesmo
  resultado de sucesso aparente (`"request-sent"`) — cumpre "recuperação...
  sem revelar existência do e-mail" nos dois fluxos, não só no de senha.
  Ausência de CAPTCHA confirmada (grep no DOM renderizado, sem
  `iframe`/texto "captcha"), conforme UX-SPEC 3.3.8/WCAG 2.2.
- `npx playwright test e2e/identity-signin.spec.ts` (3/3) verde; 35 testes
  unitários novos, todos verdes.

**Veredito: Aprovada, sem ressalva.**

### TASK-038 — Sessão com refresh rotativo (DI-10)

**Critério de aceite**: "`localStorage` sem dado do usuário; refresh
automático testado."

- Leitura direta de `dexie-auth-storage.ts`/`create-identity-supabase-client.ts`
  confirma o desenho: `createDexieAuthStorage` implementa a porta mínima
  `getItem`/`setItem`/`removeItem` sobre `db.authSession` (IndexedDB via
  Dexie), passada como `auth.storage` do `createClient` — o SDK nunca toca
  `globalThis.localStorage`.
- Verificação independente do critério "localStorage vazio": os testes de
  `create-identity-supabase-client.test.ts` usam o SDK completo real (não um
  fake de `AuthGateway`) contra um `fetch` que só intercepta as chamadas
  HTTP (`/token?grant_type=password`/`grant_type=refresh_token`), com
  ambiente `jsdom` (implementação real de `window.localStorage`, não um
  stub) — `window.localStorage.length === 0` confirmado após login real e
  após o próprio mecanismo automático do SDK (`_startAutoRefresh`, não
  chamado manualmente pelo teste) trocar o token com `expires_in: 60`.
  Considerado evidência suficiente e equivalente a uma inspeção via
  DevTools/Playwright real, dado que exercita o SDK genuíno com um DOM real
  (jsdom), e nenhuma tela ainda está montada em rota real para uma inspeção
  de navegador ponta a ponta ser possível neste ponto do projeto (mesma
  limitação já registrada desde TASK-035, App.tsx ainda placeholder).
- Nenhum código de `src/features/identity` faz `db.from("profile")` nem
  qualquer escrita de perfil a partir do cliente (grep confirmado) — a
  única fonte de `profile` é o gatilho de TASK-035, então DI-10 desta tarefa
  não tem sobreposição com o achado crítico de TASK-035/036.

**Veredito: Aprovada, sem ressalva.**

### TASK-039 — Tela T-13: exportação de dados em JSON

**Critério de aceite**: "e2e baixa JSON com progresso/preferências/esboços."

- Leitura de `export-data.ts` confirma os 3 campos presentes
  (`progresso`/`preferencias`/`esbocos`) e a decisão documentada e honesta
  sobre `esbocos` sempre `[]` nesta fase (Módulo 2/Fase 2 ainda não existe,
  `notas.esbocos` explica o motivo dentro do próprio arquivo exportado —
  cumpre DI-06 sem inventar um estado "vazio" que o UX-SPEC já descartava).
- `npx playwright test e2e/identity-export-data.spec.ts` (2/2) confirmado —
  download real de navegador via harness dev-only, não alcançável a partir
  do bundle de produção (confirmado: `npm run build` mostra só os módulos do
  shell real).

**Veredito: Aprovada, sem ressalva.**

### TASK-040 — Tela T-13: fluxo de exclusão

**Critério de aceite**: "e2e cria `erasure_request` com `due_at = +15
dias`."

- Leitura de `request-account-erasure.ts` confirma que o corpo do INSERT
  **nunca** envia `due_at` — só `user_id`; o valor exibido vem do
  `select("requested_at, due_at")` encadeado, que reflete o que o trigger
  `set_erasure_request_due_at` (TASK-032, já `Validado` no Lote 5)
  calculou no servidor.
- `npx playwright test e2e/identity-erasure.spec.ts` (2/2) confirma: (1) o
  fluxo cria `erasure_request` com `due_at - requested_at === 15 dias`
  exatos; (2) guardrail extra — POST manual via REST com `due_at` forjado
  (100 anos no futuro) é sobrescrito pelo trigger do servidor, confirmando
  que o cliente não tem autoridade nenhuma sobre a data, mesmo tentando
  contorná-la fora do código deste projeto.

**Veredito: Aprovada, sem ressalva.**

## 5. Correção da Regressão de CSP (dev vs. preview/build) — Confirmação Independente

Confirmado por leitura direta de `src/tools/security/securityHeaders.ts`
(`applyDevSecurityHeaders`) e `vitePluginSecurityHeaders.ts`:

- `configureServer` (dev) usa `applyDevSecurityHeaders` — aplica todos os
  headers **exceto** `Content-Security-Policy`. Decisão registrada e
  razoável: `vite dev` nunca é o ambiente de ameaça real que a CSP protege,
  e manter uma segunda CSP "relaxada" sincronizada manualmente com a de
  produção divergiria silenciosamente com o tempo.
- `configurePreviewServer` e `writeBundle` continuam usando
  `applySecurityHeaders` (conjunto completo, com CSP) — **nenhuma
  regressão no critério de aceite original de TASK-034**. Confirmado
  empiricamente: `npm run build` gerou `dist/_headers` com a CSP completa
  intacta (`script-src 'self'` sem `unsafe-inline`/`unsafe-eval`,
  `style-src 'self' 'unsafe-inline'`, `frame-ancestors 'none'`, os 5
  headers), inspecionado diretamente nesta rodada (Seção 1), byte a byte
  igual ao valor documentado antes da correção.
- `npx playwright test e2e/security-headers-dev.spec.ts` (1/1) verde,
  confirmando via HTTP real e via listener de `console`/`pageerror` que (a)
  o header CSP está ausente em dev e (b) nenhuma violação de CSP é
  registrada pelo navegador ao carregar o app.

**Conclusão**: a correção está completa e não reabriu o achado original de
TASK-034 (Lote 5, já `Validado`). Não há achado remanescente sobre CSP.

## 6. Testes de Integração Cruzada do Lote

- TASK-035→TASK-036: `signUpWithConsent` reaproveita `signUpWithEmailPassword`
  sem duplicar lógica — confirmado por leitura, não é uma reimplementação
  paralela.
- TASK-035/036→TASK-038: sessão de login/cadastro é a mesma manipulada por
  `createIdentityAuthGateway`; nenhuma duplicação de gestão de token entre as
  tarefas.
- TASK-038→TASK-039/TASK-040: os dois blocos (`BlocoExportarDados`,
  `BlocoExcluirConta`) foram deliberadamente mantidos como subcomponentes
  independentes (não uma `TelaT13` fundida) justamente para permitir esta
  paralelização sem colisão de arquivo — confirmado que nenhum dos dois
  toca o outro nem duplica a leitura de sessão.
- TASK-032 (Lote 5, `erasure_request`)→TASK-040: RLS e trigger de `due_at`
  reconfirmados end-to-end pelo e2e de TASK-040, sem necessidade de alterar
  nada em TASK-032.
- Achado cruzado real (não é integração "limpa"): TASK-035→testes de RLS de
  TASK-027 (Lote 5) — ver Achado Crítico 1 (Seção 2). Este é exatamente o
  tipo de teste de integração cruzada que este chapéu existe para pegar.

## 7. Requisitos Não Funcionais Relevantes ao Lote

- **Usabilidade/acessibilidade (UX-SPEC §5, DI-06/DI-07/DI-08/DI-14)**:
  4 estados cobertos em toda tela nova (`TelaCriarConta`, `TelaEntrar`,
  `BlocoExportarDados`, `BlocoExcluirConta`); nenhuma informação só por cor
  em nenhum dos 4 componentes (confirmado por leitura, todo estado de erro
  tem ícone **e** texto); alvo de toque herdado dos primitivos do Design
  System (Lote 4), não redefinido aqui. DI-14 (axe/Playwright automatizados)
  segue sem integração real no projeto — débito já conhecido, reforçado
  como `REFAT-L6-02`.
- **Segurança operacional de senha (SDD §7.1/§7.5)**: "senha mínima e
  verificação de vazamento conhecido" — `supabase/config.toml` tem
  `minimum_password_length = 6` (o mínimo absoluto aceito pela ferramenta,
  não um valor deliberadamente escolhido acima do piso) e nenhuma
  verificação de vazamento conhecido configurada (recurso de proteção de
  senha vazada não está disponível na config local desta versão da CLI —
  é tipicamente um recurso do projeto hospedado). Não bloqueia este lote
  (ver `REFAT-L6-01`, débito operacional para o chapéu DevOps confirmar
  antes do go-live).
- **Performance básica**: nenhum teste de carga aplicável a este lote
  (nenhuma tela lista grande volume de dado); não há achado.

## 8. Fechamento Estrutural do Lote (checagem do próprio Validador)

- Das 6 tarefas do Lote 6: **4 seguem `Concluída` sem ressalva relevante**
  (TASK-037, TASK-038, TASK-039, TASK-040); **2 voltam para `Em andamento`**
  (TASK-035, TASK-036), por reprovação crítica correlata (Seção 2 e 3).
- Nenhuma dependência da Seção 3/4 do `TASK.md` órfã: TASK-036/037/038
  dependem de TASK-035 (correta, ainda que TASK-035 volte a `Em andamento` —
  a dependência de sequência de trabalho continua válida, só o status
  muda); TASK-039 depende de TASK-038 (Aprovada); TASK-040 depende de
  TASK-038 (Aprovada) e TASK-032 (Lote 5, já `Validado`). Nenhuma tarefa
  `Bloqueada` sem resolução.
- **O Lote 6 não fecha como `Validado` nesta rodada** — 2 de 6 tarefas
  reprovadas com severidade crítica. `TASK.md` atualizado (Seção 3, Lote 6):
  TASK-035 e TASK-036 revertidas de `Concluída` para `Em andamento`, com
  nota apontando para esta seção do `QA-REPORT.md` e para
  `.md/SECURITY-REVIEW.md`, seção "Lote 6".
- `Refatoração Lote-6` criada nesta rodada, com 2 tarefas de débito
  baixa/média severidade (não bloqueantes, não ligadas às 2 reprovações
  críticas): `REFAT-L6-01` (política de senha), `REFAT-L6-02`
  (axe/jest-axe para as telas de identidade).
- Não escala ao `coordenador`: os dois achados críticos exigem correção de
  código (gatilho de banco + orquestração cliente-servidor), não redesenho
  de dependência/decomposição de tarefas — a decomposição do Lote 6 em si
  (6 tarefas, dependências, paralelismo) permanece válida e não precisa
  mudar.

## 9. Veredito Geral do Lote 6

**4 de 6 tarefas: Aprovadas** (TASK-037, TASK-038, TASK-039, TASK-040).
**2 de 6 tarefas: Reprovadas (crítica)** — TASK-035 e TASK-036, voltam para
`executor` (ver Seção 2, Seção 3, `.md/BLOCKERS.md`).

Definition of Done por lote (chapéu QA), aplicada ao Lote 6:

- [ ] Todo critério de aceite de cada tarefa do lote foi testado e está
      passando — **não**, TASK-035/TASK-036 têm reprovação crítica em
      aberto (mecanismo de consentimento, não o critério de aceite textual
      isolado)
- [ ] Nenhuma reprovação crítica em aberto — **não**, 2 em aberto
- [x] Toda reprovação simples virou tarefa em `Refatoração Lote-6`
      (`REFAT-L6-01`, `REFAT-L6-02`)
- [x] Testes de integração cruzada executados e passando (à exceção do
      achado cruzado registrado na Seção 2/6)
- [x] Requisito não funcional relevante ao lote validado

**O Lote 6 fecha como Reprovado (crítica) nesta rodada — não segue para a
auditoria completa do chapéu DevSecOps.** Ver `.md/SECURITY-REVIEW.md`,
seção "Lote 6", para o registro formal dos 2 achados (que são, ao mesmo
tempo, achados deste chapéu QA e achados de segurança/compliance do chapéu
DevSecOps) e para o veredito de bloqueio de deploy. TASK-035 e TASK-036
voltam para o `executor`; quando corrigidas, a revalidação cobre **só** o
que foi reprovado e o que depende disso dentro do lote (TASK-036 já
depende de TASK-035; TASK-037/038 usam `AuthGateway`/sessão mas não o
gatilho de `profile`/`consent` em si — revalidação focada, não o lote
inteiro de novo).

Escalado ao `gestor` em paralelo (não como pré-requisito do bloqueio):
achado de compliance (LGPD, Seção 3) tem relevância estratégica. Não
escalado ao `coordenador`: nenhum padrão recorrente de bug de decomposição,
e a correção não exige redesenho de dependência/decomposição do lote.
