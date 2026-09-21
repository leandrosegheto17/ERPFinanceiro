# PRD.md — ERP Financeiro (C#)

> **RASCUNHO v0.2** (rodada 2: D-01 Firebird 3.0 embarcado) — 21/09/2026. Autor: Gestor (chapéu PM). Base: `VISAO-PRODUTO.md`, Gate 1 (Aprovado com ressalvas).
> Legenda: **[OBRIG]** requisito do desafio · **[SUG]** sugestão adicional · **[A VALIDAR]** decisão em aberto assumida com a recomendação do VISAO-PRODUTO.

## 1. Problema e Contexto
O ERP Vendas (Delphi) não possui módulo financeiro. Precisa de um sistema separado que quite e cancele vendas, mantenha o registro financeiro de cada uma e permita consultar e emitir relatório do que foi processado. O módulo é parte de um desafio técnico da CartSys (vaga Delphi/C# Sênior), prazo de 7 dias corridos a partir de 21/09/2026, avaliado em padrão de desenvolvimento, linha de raciocínio, boas práticas e maturidade técnica.
Restrições: stack obrigatória (C# .NET Framework 4.8, DevExpress WinForms, EF6, FastReport, Firebird 3 ou SQL Server), **custo zero** (trials/edições gratuitas, D-09 decidida), sem acesso ao código do Vendas (contrato JSON é a única referência).

## 2. Público-Alvo
- **ERP Vendas (Delphi):** cliente da API REST (sistema, não pessoa).
- **Avaliadores técnicos da CartSys:** leem o código, executam o sistema e o relatório.
- **Operador financeiro (persona de uso da tela):** consulta vendas processadas e emite o relatório.

## 3. Objetivo de Sucesso (mensurável)
| Métrica | Baseline | Meta |
|---|---|---|
| Fluxo ponta a ponta com o Vendas real (quitação enviada, persistida, respondida, visível na consulta e no relatório) | 0 | 100% dos 3 endpoints obrigatórios funcionando em teste de integração real até o fim do Dia 4 (24/09) |
| Requisitos obrigatórios (O-01…O-12) entregues | 0/12 | O-01…O-11 até 25/09/2026 (fim do dev); O-12 (entrega) até 27/09 |
| Cenários de aceite do PRD-TECNICO passando | 0 | 100% dos cenários P0; 100% das regras de estado cobertas por teste unitário |
| Execução em máquina limpa seguindo o README | não testado | 1 execução bem-sucedida no Dia 7 |
| Quitações duplicadas/inconsistentes sob reenvio e concorrência | n/a | 0 (idempotência e concorrência testadas) |

## 4. Escopo
**Dentro (MVP):**
- Quitação de venda [OBRIG]; justificativa: função central do módulo.
- Cancelamento de venda [OBRIG]; idem.
- Consulta de status por venda [OBRIG]: reconciliação com o Vendas.
- Tela de consulta com filtros (período, cliente, status) [OBRIG consulta / SUG filtros]. Fluxo e desenho de telas ficam para o UX-SPEC.md.
- Relatório financeiro com emissão a partir da tela [OBRIG].
- API REST/JSON para o Vendas [OBRIG].
- Persistência com histórico append-only [SUG, alto valor/baixo custo].
- Entregáveis: código, executável, script/backup do banco, README com licenças [OBRIG].

**Fora:** contas a pagar/receber genéricas, conciliação bancária, multiempresa/multimoeda (fora do desafio), cadastro de clientes (só `clienteId`, D-10), estorno formal (S-13, evolução), autenticação de usuários da tela.

## 5. Requisitos de Alto Nível e Prioridade
| ID | Requisito | Tipo | Prio | Justificativa |
|---|---|---|---|---|
| R1 | Quitar venda via API | OBRIG | P0 | Núcleo |
| R2 | Cancelar venda via API | OBRIG | P0 | Núcleo |
| R3 | Consultar status via API | OBRIG | P0 | Reconciliação |
| R4 | Persistir registro financeiro (venda, itens, histórico) | OBRIG | P0 | Sem persistência não há consulta |
| R5 | Tela de consulta (DevExpress) | OBRIG | P0 | Exigida |
| R6 | Relatório financeiro (FastReport) | OBRIG | P0 | Exigido |
| R7 | Entregáveis e README (licenças, trials, evidências) | OBRIG | P0 | Avaliador precisa rodar |
| R8 | Integração real com o Vendas | OBRIG | P0 | Critério de sucesso |
| R9 | Arquitetura em camadas, DTOs próprios (S-01, S-02) | SUG | P0 | Avaliado (maturidade); custo embutido |
| R10 | Idempotência e concorrência (S-04) | SUG | P1 (não cortar) | Integridade financeira |
| R11 | Erros padronizados (envelope), validação `valorTotal` | SUG | P1 | Contrato robusto |
| R12 | Histórico consultável na tela (S-03) | SUG | P1 | Auditoria |
| R13 | Filtros e totais quitado/cancelado/pendente (S-06, S-07) | SUG | P1 | Valor da consulta/relatório |
| R14 | API Key e health check (S-08, S-09) | SUG | P1 | Segurança básica |
| R15 | Log estruturado Serilog (S-10) | SUG | P1 | Diagnóstico |
| R16 | Testes unitários xUnit + Moq (S-11) | SUG | P1 | Boas práticas |
| R17 | Histórico via API (S-12); estorno formal (S-13) | SUG | P2 | Só se sobrar tempo; cortar primeiro |

**Prazo (D-07 decidida):** dev até 25/09 (Dia 5); 26 e 27/09 só entrega. P0 + P1 (62h) não cabem em 5 dias; proposta de escopo, a validar com o usuário:
- **Tier A (manter, ~49h ≈ 10h/dia):** P0 + S-01, S-02, S-04, S-05, S-08, S-09.
- **Tier B (só se houver folga, +6h):** S-06, S-07, S-11 reduzido (máquina de estados).
- **Tier C (cai / evolução):** S-03, S-10, S-12, S-13 e o restante de S-11.
- Consequência: R12, R15 e parte de R13 e R16 viram condicionais. Se a jornada for 8h/dia (40h), nem o P0 cabe; nesse caso reduzir o layout de O-09/O-10 ao mínimo.
Ordem de corte: S-13, S-12, S-10, S-03, S-11, S-07, S-06, S-09. Nunca cortar O-01…O-12, S-01, S-04, S-05, S-08. Marco: API utilizável pelo Vendas até fim do Dia 3; integração real (O-11) até fim do Dia 4.

## 6. Premissas e Riscos
| ID | Premissa / Risco | Dono | Prazo de validação | Status |
|---|---|---|---|---|
| A-01 | **D-01 DECIDIDA (rodada 2): Firebird 3.0 embarcado**, um `.fdb` próprio; provider EF6 `EntityFramework.Firebird` + `FirebirdSql.Data.FirebirdClient`; sem migrations (DDL manual em `database/`). Fallback: SQL Server Express só se o spike do Dia 1 falhar (ver A-13) | Leandro | decidida; spike no início do Dia 1 | DECIDIDA (com fallback) |
| A-02 | **D-02:** banco próprio (.fdb do Financeiro), sem FK/tabelas cruzadas com o Vendas, que também usa Firebird 3.0 | Leandro | Dia 1 | Segue "banco próprio" (rodada 2) |
| A-13 | Spike Dia 1 (~2h, início do dia, antes de O-02/O-03): **GO Firebird** se, com EF6 + Firebird embarcado, (a) gravar em FIN_VENDA com identidade/PK funcionando, (b) leitura de volta correta com DECIMAL, (c) duas atualizações concorrentes da mesma linha, com `VERSAO`, resultarem em uma bem-sucedida e a outra detectada (falha de concorrência). **NO-GO (fallback SQL Server Express)** se qualquer um falhar e não for contornável em +1h de tentativa. Resultado registrado em ADR | Leandro | Dia 1, ~2h | PENDENTE |
| A-03 | **D-03:** adotar P-9 (`POST /api/vendas` cria Pendente; quitação faz upsert). Alternativa: remover "pendente" do relatório | Leandro + Vendas | Dia 1 | A VALIDAR |
| A-04 | **D-04:** OWIN self-host dentro do Desktop | Leandro | Dia 1 | A VALIDAR |
| A-05 | **D-05:** rota `/api/vendas` + alias `/api/v1/vendas` | Leandro + Vendas | Dia 1 | A VALIDAR |
| A-06 | **D-06:** cancelar venda Quitada só com `motivo`, senão 409 | Leandro | Dia 1 | A VALIDAR |
| A-07 | **D-07 DECIDIDA (rodada 2):** desenvolvimento até sexta 25/09 (Dia 5); 26 e 27/09 = buffer/entrega (O-12, máquina limpa), sem dev novo | Leandro | decidida | DECIDIDA |
| R-07 | **Capacidade:** P0 = 43h (sem O-12) + spike; P0 + P1 = 62h. Em 5 dias só P0 cabe com ~9 a 10h/dia; P0 + P1 não cabe | Leandro | Dia 1 (confirmar horas/dia) | Ver escopo em camadas abaixo |
| A-08 | **D-08:** cancelar venda desconhecida cria registro Cancelada | Leandro + Vendas | Dia 1 | A VALIDAR |
| A-09 | **D-10:** cliente exibido por ID (sem nome) | Leandro | Dia 4 | A VALIDAR |
| A-10 | **D-11:** xUnit + Moq | Leandro | Dia 2 | A VALIDAR |
| A-11 | Contrato v1.1 (P-1…P-9) aceito pelo Vendas | Leandro | Dia 1 (marco fim Dia 3) | A VALIDAR |
| A-12 | D-09 decidida: custo zero com trials | Leandro | decidida; confirmar duração/limites no Dia 1 | DECIDIDA |
| R-01 | Trials expiram ou exibem marca d'água/aviso | Leandro | Dia 1 | Mitigar: README + evidências |
| R-02 | Prazo apertado (dual-stack) | Leandro | contínuo | Ordem de corte |
| R-03 | Divergência de contrato/decimais/datas Delphi↔C# | Leandro | Dia 3 | P-6/P-7 + exemplos JSON |
| R-04 | Sem acesso ao código do Vendas; integração real só no Dia 6 | Leandro | Dia 3 | Testar cedo com Postman/curl |
| R-05 | EF6 diferente de EF Core; provider Firebird menos maduro, sem migrations e sem rowversion | Leandro | Dia 1 (spike A-13) | DDL manual versionado, `VERSAO` INT, fallback SQL Server |
| R-06 | Firebird embarcado: acesso simultâneo de dois processos ao mesmo `.fdb` (API e tela estão no mesmo executável, mitiga); bitness da DLL cliente (x86/x64) | Leandro | Dia 1 | Confirmar no spike |

## 7. Perguntas em Aberto
D-01 e D-02 estão decididas (rodada 2). Ainda abertas: D-03, D-04, D-05, D-06, D-07, D-08, D-10, D-11 (ver Seção 6, todas com recomendação assumida). Perguntas adicionais:
- Q-1: O Vendas aceita os refinamentos P-1…P-9 (aditivos)? Quem valida do lado do Vendas?
- Q-2: O relatório tem layout/campos definidos pelo desafio, ou é livre?
- Q-3: O avaliador tem DevExpress/FastReport instalados ou dependerá das evidências?

Alinhamento com Gate 1 (stakeholder-alignment-check): sem conflito; prazo e custo zero mantidos.
