# GUARDRAILS.md — App de Leitura Bíblica Guiada + Preparo de Estudo

- **Versão**: rascunho inicial (produzido junto do `TASK.md`, Loop C do
  `/definir_organizar`)
- **Autor**: coordenador (chapéu Tech Lead), a partir de `CTO-REVIEW.md`,
  `SDD.md` e ADRs 001–012
- **Status**: rascunho — aguarda aprovação rápida do Gestor. Depois de aprovado,
  regra inegociável para todos os agentes (executor, validador, coordenador)
- **Escopo**: regras que nenhuma tarefa, nenhum PR e nenhuma decisão tática pode
  violar silenciosamente. Violação encontrada pelo Executor ou pelo Validador vai
  para `BLOCKERS.md`, nunca é decidida sozinha no momento.

---

## 1. Produto e conteúdo

- **G-01** Nunca exibir "Almeida Atualizada", "Almeida Revista e Atualizada",
  "ARA" ou "ARC" em nenhuma tela, texto de marketing, commit message de conteúdo
  ou variável de código. Teste automatizado obrigatório sobre o bundle final
  (`TASK-011`, `TASK-091`). Origem: `CTO-REVIEW.md` R-02, RN-06.
- **G-02** Toda tela com texto bíblico exibe a atribuição da licença (BLIVRE, CC
  BY 4.0, data da versão) a no máximo **um toque**. Origem: RN-06, CA-01.3.
- **G-03** Nenhum HTML vindo do usuário (esboço) ou do acervo editorial (notas)
  é injetado sem sanitização. Origem: SDD §7.7, ADR-004.
- **G-04** Conteúdo (plano, perícopes, notas, ganchos) só publica se passar pelo
  validador C1–C11 + cobertura de RN-13; o validador tem poder de **veto de
  build**, nunca é aviso. Origem: ADR-004, CA-02.5, CA-06.6.
- **G-05** Nenhum requisito de gamificação (medalha, aviso de sequência
  interrompida, penalidade visual por atraso) entra no produto, mesmo que
  pedido em tarefa futura, sem voltar a este guardrail. Origem: RNF-13, F-17.

## 2. Arquitetura e dados

- **G-06** Regra de negócio existe **uma vez**, no cliente. O backend
  (Supabase/Postgres) nunca reimplementa uma regra de negócio já implementada
  no cliente — decide apenas identidade, propriedade de linha (RLS) e
  conciliação. Origem: ADR-002.
- **G-07** `core/*` é TypeScript puro, sem dependência de React, DOM ou de
  `features/*`. Dependência entre camadas é unidirecional e verificada por lint
  no CI, não por revisão manual. Origem: ADR-001, ADR-012.
- **G-08** `features/outline` e `features/presentation` (Módulo 2) nunca são
  importados por código da Fase 1; a rota do Módulo 2 não é registrada sem
  sessão válida. Origem: ADR-012, CA-09.4, CA-16.5.
- **G-09** **Toda** tabela com coluna `user_id` nasce com RLS habilitada **na
  mesma migration** que a cria. Uma migration que cria tabela sem política de
  RLS é bloqueio de deploy, não item de backlog. Teste de catálogo obrigatório
  no CI (`TASK-033`). Origem: RT-04, SDD §7.2.
- **G-10** Nenhum segredo de serviço (chave VAPID privada, credencial de
  service role, connection string privilegiada) é gravado em código de
  cliente ou aparece no bundle publicado. Teste de grep obrigatório no CI antes
  de qualquer release (`TASK-042`). Origem: SDD §7.1, §7.5.
- **G-11** Nenhum dado do usuário é gravado em `localStorage`; conteúdo do
  usuário só em IndexedDB (Dexie). `localStorage` só pode conter o necessário de
  sessão, nunca conteúdo do usuário. Origem: SDD §7.1, §7.3.
- **G-12** O esboço materializado para apresentação (`outlineSnapshots`) é um
  documento autocontido: em tempo de apresentação, lê-se **um único registro** e
  nada mais — nenhuma outra store, nenhum cache HTTP, nenhuma chamada de rede.
  Qualquer requisição observada durante a rota de apresentação é defeito de
  severidade máxima. Origem: ADR-006, RNF-01, M-04.
- **G-13** Nenhuma dependência de custo por usuário entra no produto (SDK de
  analytics pago, provedor de push pago, CDN com cobrança de banda, CMS por
  uso) sem voltar ao Gestor. Origem: `CTO-REVIEW.md` R-05, RNF-06.

## 3. Segurança e privacidade (insumo para o Validador, chapéu DevSecOps)

- **G-14** Nenhuma escrita de dado pessoal no servidor ocorre antes de existir a
  linha de consentimento correspondente em `consent` — garantido por política
  de banco, não por ordem de chamadas no cliente. Origem: RNF-05, CA-10.2.
- **G-15** Recusar o consentimento nunca degrada a experiência sem conta — todo
  o comportamento de CA-09.1 permanece disponível. Origem: CA-10.5.
- **G-16** Exclusão de conta e dados sempre em até 15 dias, com cascata
  documentada e retenção de backup declarada ao titular antes da confirmação.
  Origem: RN-11.
- **G-17** Nenhum destino de terceiro para telemetria, anúncio ou fonte externa
  — garantido mecanicamente por CSP (`default-src 'self'`), não apenas por
  convenção de código. Origem: ADR-009, E-07, CA-08.2.
- **G-18** CSP, HSTS, `Permissions-Policy` e `Referrer-Policy` são testados
  automaticamente antes de cada release, não revisados manualmente. Origem:
  SDD §7.5.
- **G-19** Nenhum CAPTCHA de quebra-cabeça em nenhum fluxo de autenticação
  (viola WCAG 2.2, 3.3.8); limite de tentativas fica no servidor. Origem:
  §7.5, CA-09/T-09.

## 4. Acessibilidade (não negociável em nenhuma tela)

- **G-20** Toda tela atinge WCAG 2.2 nível AA; o modo apresentação (T-20) atinge
  contraste ≥ 7:1 (nível AAA para esse critério). Verificado por teste
  automatizado de contraste sobre pares de token, nunca por inspeção manual.
  Origem: RNF-04, CA-14.6.
- **G-21** Nenhuma informação de estado é transmitida só por cor — todo selo,
  indicador ou estado tem ícone **e** texto. Origem: UX-SPEC §5.1 (1.4.1).
- **G-22** Toda funcionalidade de arrastar (reordenar bloco) tem alternativa por
  botão, com anúncio da nova posição em região viva. Origem: WCAG 2.5.7.
- **G-23** Alvo de toque mínimo de 44×44 px em toda a interface, 48×48 px nos
  controles do modo apresentação. Origem: WCAG 2.5.8, UX-SPEC §3.1.
- **G-24** Toda tela é cruzada contra os 4 estados (vazio, carregando, erro,
  sucesso) antes de ser marcada `Concluída`; ausência de algum estado exige
  justificativa escrita, nunca omissão silenciosa. Origem: UX-SPEC §4.

## 5. Processo e decomposição (para o próprio Coordenador/Executor)

- **G-25** Nenhuma tarefa mistura mais de um item de: tela/fluxo de tela;
  endpoint de API; regra de negócio distinta; mudança de schema/SQL — salvo
  inseparabilidade genuína, documentada explicitamente (não decidida em
  silêncio). Origem: convenção de decomposição deste pipeline.
- **G-26** Nenhuma tarefa deve exigir do Executor mais de ~300 mil tokens de
  contexto de trabalho previstos; se a previsão apontar mais, a tarefa é
  dividida antes de publicada. Origem: canário de sizing deste pipeline.
- **G-27** ADRs são imutáveis. Mudança de decisão arquitetural é sempre um novo
  ADR com `Status: Superseded by ADR-NNN`, nunca edição do ADR original.
  Origem: `create-adr`, `adr-drafting`.

---

## Nota de aprovação

Este é o rascunho inicial, extraído mecanicamente de `CTO-REVIEW.md`, `SDD.md` e
dos ADRs 001–012 — nenhuma regra aqui é nova; cada uma referencia sua origem.
Cabe ao Gestor a aprovação rápida (ou ajuste) antes do início da execução pelo
Executor.
