# PIPELINE-CONVENTIONS.md

Convenções que amarram os agentes do pipeline entre si. Onde AGENT-TEMPLATE.md
define a estrutura de **cada** agente, este documento define como eles **conversam**:
artefatos, handoff, nomenclatura, governança de inconsistência e o papel do
GUARDRAILS.md.

> **Modelo ativo: 4 agentes consolidados.** Desde a consolidação registrada aqui, os
> fluxos ativos (`PLANNING-FLOW.md`, `EXECUTION-FLOW.md` e os comandos `/planejar`,
> `/definir_organizar`, `/listar`, `/executar`, `/validar`, `/deploy`) usam só 4
> agentes: `gestor` (CTO + PM + Business Analyst), `coordenador` (Software Architect
> + Tech Lead + UX/UI), `executor` (Backend + Frontend + Mobile) e `validador` (QA +
> DevSecOps + DevOps). O usuário é o orquestrador — decide quando cada comando roda;
> nenhum fluxo encadeia fases automaticamente sem o usuário acionar o próximo
> comando. Os 12 agentes originais (`cto`, `pm`, `business-analyst`,
> `software-architect`, `ux-ui`, `tech-lead`, `backend`, `frontend`, `mobile`, `qa`,
> `devsecops`, `devops`) foram movidos para `.claude/agents_inativos/` (fora de
> `.claude/agents/`, para não aparecerem mais como tipo de agente invocável) e
> não são mais referenciados por nenhum fluxo ou comando ativo — a ordem de 12 papéis
> abaixo fica documentada só como referência histórica de onde cada responsabilidade
> dos 4 agentes consolidados veio.

## Ordem de atuação (modelo ativo — 4 agentes)

```
1. Gestor (CTO/PM/BA) → 2. Coordenador (Arquitetura/UX/Decomposição)
→ 3. Executor (Backend/Frontend/Mobile, em paralelo por tarefa)
→ 4. Validador (QA/DevSecOps/DevOps)
```

O Gestor não é só a etapa 1: também é a camada de governança que reabre em pontos
específicos mais adiante (Gate 1 no início, Gate 4 no fechamento pós-deploy — ver
"Gates do Gestor" abaixo). O Executor roda em **paralelo por tarefa** (não mais 3
trilhas fixas por papel): o Coordenador decompõe o TASK.md em tarefas pequenas e
marca quais são paralelizáveis dentro do lote (Seção 4), e o usuário dispara uma
instância do Executor por tarefa elegível a cada rodada.

<details>
<summary>Ordem de atuação — pipeline de 12 agentes (legado, não usado pelos fluxos ativos)</summary>

```
1. CTO / Head de Tecnologia   → 2. PM  → 3. Business Analyst → 4. Software Architect
→ 5. UX/UI → 6. Tech Lead → 7. Backend → 8. Frontend → 9. Mobile → 10. QA
→ 11. DevSecOps → 12. DevOps
```

Backend, Frontend e Mobile (7-9) rodavam em paralelo/sequência conforme o escopo do
projeto exigir; a ordem numérica era só referência de handoff a partir do TASK.md.
Este pipeline de 12 continua existindo em `.claude/agents_inativos/`, mas nenhum comando
ativo o referencia mais.
</details>

---

## 1. Lista definitiva de artefatos

| # | Artefato | Dono (cria) | Consumidores (leem) | Formato |
|---|---|---|---|---|
| 1 | `CTO-REVIEW.md` | Gestor | Todos | Log datado por gate, cada seção termina em veredito (Aprovado / Aprovado com ressalvas / Reprovado) |
| 2 | `PRD.md` | Gestor | Coordenador, Executor (contexto), Validador (contexto) | Requisitos funcionais e não-funcionais, regras de negócio, critérios de aceite |
| 3 | `PRD-TECNICO.md` | Gestor | Coordenador, Executor (contexto), Validador (contexto) | Tradução dos requisitos em restrições/contratos técnicos |
| 4 | `SDD.md` | Coordenador | Executor, Validador, Gestor | Arquitetura, schemas de dados, contratos de API, decisões estruturais |
| 5 | `UX-SPEC.md` | Coordenador | Executor, Validador, Gestor | Fluxos de tela, wireframes, design system, estados de tela, acessibilidade (WCAG), comportamento responsivo |
| 6 | `GUARDRAILS.md` | Coordenador (propõe) + Gestor (aprova) | Todos | Regras inegociáveis do projeto — documento vivo, ver seção 5 |
| 7 | `TASK.md` | Coordenador | Executor, Validador, Gestor | Tarefas pequenas, ordenadas por dependência, com coluna de paralelismo dentro do lote, cada uma com dono (chapéu) e Status atualizado pelo Executor conforme progresso |
| 8 | `TEST-PLAN.md` | Validador | Gestor | Estratégia de teste (funcional, integração, regressão, e2e) derivada de PRD-TECNICO.md + TASK.md |
| 9 | `QA-REPORT.md` | Validador | Executor, Coordenador, Gestor | Validação por tarefa/lote (aprovado/reprovado/aprovado com ressalva), log de bugs com severidade e evidência, veredito de release-readiness |
| 10 | `SECURITY-REVIEW.md` | Validador | Gestor | Achados de segurança por severidade (SAST, dependências, secrets, OWASP, requisitos do SDD.md), status (bloqueia deploy / débito registrado com prazo), requisitos de segurança operacional |
| 11 | `DEPLOY.md` | Validador | Gestor | IaC, pipeline de CI/CD, execução de deploy por ambiente, observabilidade, estratégia de rollback, relatório de cada deploy (status, versão, incidente) |
| — | `BLOCKERS.md` | Qualquer agente que reporta bloqueio | Usuário (orquestrador) | Log de inconsistências/bloqueios — ver seção 4 |
| — | `adr/NNN-titulo-kebab-case.md` | Coordenador | Executor, Validador, Gestor | Um arquivo por decisão arquitetural, imutável — ver exceção de nomenclatura abaixo |
| — | `API-CONTRACT.yaml` | Executor | Executor (outras instâncias), Validador | OpenAPI 3.x, publicado incrementalmente por endpoint — ver exceção de nomenclatura abaixo |

Esta lista é o contrato: um agente só pode listar em `upstream`/`downstream` (no
frontmatter, ver AGENT-TEMPLATE.md) um artefato que apareça aqui, e só pode produzir um
artefato que não esteja na tabela se, no mesmo PR/sessão, esta tabela for atualizada
junto.

**Notas de consolidação** (auditoria end-to-end, herdadas do pipeline de 12 e ainda
válidas): dois artefatos citados em versões anteriores deste documento nunca chegaram
a ter um produtor real e foram consolidados em artefatos já existentes:
- `CLAUDE.md` (guia de estilo/convenções de código) → Seção 1 do `TASK.md`
  ("Diretrizes de Implementação").
- `VISAO-PRODUTO.md` (problema, objetivo de negócio, escopo do MVP) → Seções 1-3 do
  `PRD.md` ("Problema e Contexto", "Público-Alvo", "Objetivo de Sucesso").
- `CHANGELOG.md` (registro do que foi entregue por tarefa) → coluna Status + notas
  do `TASK.md`, já atualizada pelo Executor conforme progresso.

### Exceção de nomenclatura: ADRs e API-CONTRACT.yaml

Todo artefato do pipeline é um arquivo único (`.md/<NOME>.md`, regra da seção 3). Duas
exceções:

- **ADRs**: uma decisão arquitetural não é um documento que se reescreve, é um
  registro que se acumula — por isso vivem em `.md/adr/`, um arquivo imutável por
  decisão, numerado sequencialmente (`001-titulo-kebab-case.md`, `002-...`). O
  `SDD.md` (Seção "Decisões Arquiteturais") não copia o conteúdo do ADR, só indexa e
  linka para ele. Mudar uma decisão já registrada nunca edita o ADR original — cria
  um novo ADR com `Status: Superseded by ADR-NNN` no antigo, apontando para o novo.
- **`API-CONTRACT.yaml`**: contrato de API é consumido por ferramenta (codegen,
  Swagger UI, validação de schema), não só lido por humano — por isso usa OpenAPI
  3.x (`.yaml`), não Markdown. Continua vivendo em `.md/` como arquivo único, só a
  extensão muda. Publicado incrementalmente por endpoint conforme cada um fica
  estável, não só ao final da tarefa que o expõe.

### Gates do Gestor (do início ao fim do pipeline)

- **Gate 1 — Pré-descoberta** (antes do chapéu PM iniciar o levantamento, dentro do
  `/planejar`): valida alinhamento estratégico direto sobre o briefing de negócio
  recebido do stakeholder — `PRD.md` ainda não existe neste ponto. Libera ou não o
  próprio Gestor para seguir com os chapéus PM/BA na mesma chamada.
- **Gate 4 — Fechamento** (dentro do `/deploy`, após o Validador reportar o
  resultado final): o Validador reporta o resultado do deploy (sucesso, rollback,
  incidente) em `DEPLOY.md`; o Gestor registra o encerramento do ciclo de governança
  em `CTO-REVIEW.md`, fechando o que foi aberto no Gate 1. É o único gate sem poder
  de veto — o deploy já aconteceu; é um registro de fechamento, não uma aprovação
  prévia.
- **Ad hoc**: qualquer agente pode escalar um conflito para o Gestor (ver seção 4);
  toda alteração estrutural em `GUARDRAILS.md` também passa por ele (seção 5).

**O que substituiu os antigos Gates 2 e 3 do CTO** (revisão pós-SDD.md e
pré-TASK.md): não existem mais como aprovação de agente. O `/definir_organizar`
entrega SDD.md + UX-SPEC.md + TASK.md numa única sequência do Coordenador, e é o
**usuário** quem aprova, pede ajuste pontual ou reprova diretamente — sem um agente
Gestor intermediário. As skills que faziam essa análise
(`architecture-decision-review`, `build-vs-buy-analysis`, `risk-and-compliance-check`,
`capacity-and-timeline-validation`) continuam disponíveis no Gestor para uso ad hoc,
caso o usuário peça um parecer de risco antes de aprovar, mas não são mais
disparadas automaticamente por nenhum fluxo.

---

## 2. Convenção de handoff

- **Localização**: todo artefato do pipeline vive na pasta `.md/` na raiz do projeto
  (`.md/PRD.md`, `.md/SDD.md`, ...) — exceção apenas para os arquivos de config dos
  próprios agentes, que ficam em `.claude/agents/` e `.claude/skills/`.
- **Nome de arquivo**: exatamente o nome da coluna "Artefato" da tabela acima —
  `MAIUSCULO-COM-HIFEN.md`. Um agente nunca inventa uma variação de nome
  (`prd-v2.md`, `SDD_final.md`); se precisar de uma nova versão, ver regra de
  versionamento abaixo.
- **Como o output de um agente vira input do próximo**: o agente downstream lê o
  artefato pelo caminho fixo (`.md/<ARTEFATO>.md`), nunca por um caminho combinado em
  conversa. Se o artefato esperado não existe, o agente aplica a coluna "Se ausente" do
  seu próprio `Inputs Esperados` (AGENT-TEMPLATE.md) — não presume conteúdo.
- **Versionamento**: os artefatos são versionados pelo git do projeto, não por sufixo
  de nome de arquivo. Um artefato "congela" quando o agente dono o entrega ao próximo
  da cadeia (commit); mudanças depois disso são um novo commit no mesmo arquivo, nunca
  uma cópia paralela. Exceção: `GUARDRAILS.md` mantém adicionalmente um log de
  alterações dentro do próprio arquivo (seção 5).
- **Reset de contexto**: a partir da fase de execução (Executor → Validador), cada
  agente entra com escopo limpo — não carrega o histórico de decisões de
  implementação das fases anteriores, só os artefatos formais da tabela acima. Isso é
  deliberado: evita que vício de contexto de implementação influencie a revisão de
  segurança/infra.

## 3. Convenção de nomenclatura de arquivos e pastas

- Artefatos de pipeline (raiz `.md/`): `MAIUSCULO-COM-HIFEN.md`, conforme tabela da
  seção 1.
- Definições de agente (`.claude/agents/`): `slug-kebab-case.md`, um arquivo por
  agente, nome igual ao campo `name` do frontmatter (AGENT-TEMPLATE.md).
- Skills (`.claude/skills/<slug>/SKILL.md`): pasta em `kebab-case` nomeando a skill,
  arquivo sempre `SKILL.md` dentro dela.
- Código-fonte: segue a convenção de nomenclatura definida em `CLAUDE.md` (artefato
  de dono do Coordenador, Seção 1 do `TASK.md`) — este documento não define
  convenção de código, só de artefatos de pipeline e config de agente.

## 4. Governança — reporte de inconsistência entre agentes

Quando um agente encontra um problema num artefato de outro (ex.: Executor encontra
ambiguidade no `UX-SPEC.md` do Coordenador):

1. **Nunca resolve por conta própria** reinterpretando o artefato de outro agente —
   isso quebra a rastreabilidade da decisão.
2. **Registra o bloqueio** em `BLOCKERS.md` (raiz `.md/`), como uma nova entrada:

   ```markdown
   ## Bloqueio <NNN> — <data>
   - Reportado por: <slug do agente>
   - Escalado para: <slug do agente dono do artefato afetado, ou "usuário" quando é
     conflito entre pares sem dono claro>
   - Artefato/trecho afetado: <arquivo.md#seção-ou-linha>
   - Descrição: <o que está ambíguo/inconsistente/impossível de cumprir>
   - Impacto se não resolvido: <o que trava a jusante>
   - Sugestão (opcional): <proposta do agente que reportou>
   - Status: Aberto / Em resolução / Resolvido em <artefato + data>
   ```

3. **O comando em execução pausa** e apresenta a entrada ao usuário (orquestrador) —
   nenhum agente dispara sozinho o agente de destino para resolver; isso é decisão do
   usuário, que decide se/quando rodar o comando correspondente para resolver.
4. **O dono do artefato original resolve** (quando o usuário decidir acionar o
   comando correspondente), atualiza o artefato afetado e marca o bloqueio como
   `Resolvido`, referenciando o commit/seção que corrigiu.
5. **Conflito entre pares sem dono claro** (ex.: Executor e Validador discordam de um
   contrato que o SDD.md deixou subespecificado): registrado com "Escalado para:
   usuário" — é o usuário quem arbitra, não mais um agente CTO. Se a arbitragem
   envolver decisão de negócio/estratégia, o usuário pode optar por pedir um parecer
   ao Gestor antes de decidir.
6. Nenhum trabalho novo começa sobre um artefato com bloqueio `Aberto` que o afete
   diretamente — trabalho não-relacionado ao bloqueio pode continuar em paralelo.

## 5. GUARDRAILS.md como documento vivo

`GUARDRAILS.md` guarda as regras inegociáveis do projeto (ex.: "não implementar camada
de frontend neste MVP", "toda migration precisa de rollback", limites de stack). Regras
de governança:

- **Quem propõe**: Coordenador, ao gerar/atualizar o documento (dentro do
  `/definir_organizar`), ou qualquer agente que precise de uma exceção pontual a uma
  regra existente.
- **Quem aprova mudança estrutural ou exceção**: só o Gestor. Uma "mudança
  estrutural" é qualquer alteração que adiciona, remove ou reescreve uma regra — não
  inclui correções de formatação/typo, que qualquer agente pode fazer diretamente.
- **Rastreabilidade obrigatória**: toda alteração aprovada é registrada numa tabela no
  final do próprio `GUARDRAILS.md`:

  ```markdown
  ## Log de Alterações
  | Data | Proposto por | Aprovado por | Mudança | Motivo |
  |---|---|---|---|---|
  | AAAA-MM-DD | <slug> | gestor | <regra adicionada/removida/alterada> | <por quê> |
  ```

- **Exceção temporária vs. mudança permanente**: uma exceção pontual (ex.: "esta
  sprint pode pular o requisito X por causa de Y") entra no log com uma coluna extra
  `Validade` (data ou "permanente"); ao expirar, a regra original volta a valer sem
  precisar de nova aprovação.
- Qualquer agente pode **ler** `GUARDRAILS.md` livremente (é input padrão de todos);
  só o Coordenador propõe e só o Gestor aprova a escrita.

---

## Checklist de auto-revisão antes de considerar este documento aplicado

- [ ] Todo artefato citado em algum `AGENT-TEMPLATE.md` de agente existe na tabela da
      seção 1 aqui
- [ ] Todo agente que "lê X" tem o dono de X no seu `upstream`
- [ ] Todo agente que "escreve Y" tem Y na tabela da seção 1 com ele como "Dono"
- [ ] Caminho de qualquer artefato é sempre `.md/<NOME>.md`, sem exceção não
      documentada aqui
