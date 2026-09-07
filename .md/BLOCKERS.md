# BLOCKERS.md — App de Leitura Bíblica Guiada + Preparo de Estudo

## Entrada 1 — TASK-010 aguarda SPIKE-03

- **Status**: Resolvido — 2026-09-07
- **Resolução**: SPIKE-03 executado. Tabela de abreviações dos 66 livros e
  lista de casos "não resolvido" fechadas em
  `.md/spikes/SPIKE-03-resultado.md`. TASK-010 voltou a `Pendente` no
  `.md/TASK.md` (Lote 1) — elegível para `/executar` novamente.
- **Desde**: 2026-09-07
- **Afeta**: TASK-010 (Lote 1 — Núcleo: Corpus e Importação); em cascata, todo o
  Módulo 2 (Lotes 14–15 dependem de TASK-010, ver `.md/TASK.md` Seção 5, RP-01).
- **O quê**: SPIKE-03 (gramática de reconhecimento de referência bíblica em
  pt-BR: abreviações ambíguas, intervalos entre capítulos, variações de
  digitação reais — `.md/TASK.md` Seção 2) ainda não foi executado nesta
  sessão. TASK-010 (`core/reference`) depende explicitamente da conclusão do
  spike antes de começar (Seção 3, coluna "Depende de").
- **Escala para**: usuário (orquestrador) — decidir quando/como rodar o
  spike (não é uma tarefa de implementação do Executor no formato do
  TASK.md; é investigação sem estimativa forçada, conforme Seção 2). Conforme
  RP-01 (Seção 5 do TASK.md): "se ultrapassar 2 dias sem convergir, escalar ao
  Coordenador via BLOCKERS.md antes de estimar TASK-010 com confiança" — ainda
  dentro da janela, registrado aqui como bloqueio ativo para visibilidade.
- **Estado do Lote 1**: TASK-006, TASK-007, TASK-008, TASK-009 e TASK-011
  `Concluída`. TASK-010 é a única tarefa pendente do lote — o lote não fecha
  (não fica elegível para `/validar` como um todo) enquanto TASK-010 estiver
  bloqueada.
