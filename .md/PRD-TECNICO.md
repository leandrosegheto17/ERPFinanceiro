# PRD-TECNICO.md — ERP Financeiro (C#)

> **RASCUNHO v0.2** (rodada 2: D-01 Firebird 3.0 embarcado, fallback SQL Server Express) — 21/09/2026. Autor: Gestor (chapéu BA). Base: `PRD.md`, `VISAO-PRODUTO.md`.
> Legenda: **[OBRIG]** desafio · **[SUG]** sugestão adicional · **[A VALIDAR]** depende de decisão em aberto (assumida com a recomendação do VISAO-PRODUTO).
> Critérios em formato EARS. Não fixa decisão de arquitetura (competência do Coordenador); só requisitos.
> O registro de mudanças do contrato (VISAO-PRODUTO Seção 2.4) permanece como fonte única e deve ser preservado; a v1.1 só é escrita lá quando P-1…P-9 forem validadas.

## 1. Requisitos Funcionais

### RF-01 Quitar venda [OBRIG] — `POST /api/vendas/quitacao`
Entrada `{vendaId, clienteId, valorTotal, itens[{produtoId, quantidade, precoUnitario}]}`; saída `{status:"Quitada", dataQuitacao}`.
- CA-01.1: Quando receber payload válido de venda inexistente, o sistema deve persistir venda, itens e histórico, e responder 200 `Quitada` com `dataQuitacao` ISO 8601 UTC.
- CA-01.2: Quando `vendaId` já estiver Quitada, o sistema deve responder 200 com a `dataQuitacao` original, sem novo histórico [A VALIDAR P-2].
- CA-01.3: Quando a venda estiver Cancelada, o sistema deve responder 409 `VENDA_JA_CANCELADA` sem alterar dados [A VALIDAR P-2].
- CA-01.4: Quando a venda estiver Pendente (existir por P-9), o sistema deve mudá-la para Quitada e registrar histórico [A VALIDAR D-03].
- CA-01.5: Se `vendaId`/`clienteId` ausente ou vazio, `itens` vazio, `quantidade` <= 0 ou `precoUnitario` < 0, então o sistema deve responder 400 `PAYLOAD_INVALIDO` sem persistir.
- CA-01.6: Se `|valorTotal − Σ(quantidade × precoUnitario)| > 0,01`, então o sistema deve responder 400 `VALOR_TOTAL_DIVERGENTE` [A VALIDAR P-7].
- CA-01.7: Quando duas quitações simultâneas da mesma `vendaId` ocorrerem, o sistema deve persistir uma única venda (UNIQUE em `VENDA_ID`) e responder ambas com sucesso idempotente ou 409, nunca duplicar; a perdedora da concorrência (violação de UNIQUE ou conflito de `VERSAO`) relê e reavalia [SUG S-04].

### RF-02 Cancelar venda [OBRIG] — `POST /api/vendas/cancelamento`
Entrada `{vendaId, motivo?}`; saída `{status:"Cancelada"}`.
- CA-02.1: Quando a venda estiver Pendente, o sistema deve mudá-la para Cancelada, registrar `dataCancelamento`, motivo e histórico, e responder 200.
- CA-02.2: Quando a venda estiver Quitada e `motivo` preenchido, o sistema deve cancelá-la registrando motivo e histórico [A VALIDAR D-06, SUG S-05].
- CA-02.3: Se a venda estiver Quitada e `motivo` vazio/ausente, então o sistema deve responder 409 `MOTIVO_OBRIGATORIO` sem alterar [A VALIDAR D-06].
- CA-02.4: Quando a venda já estiver Cancelada, o sistema deve responder 200 `Cancelada` sem novo histórico [A VALIDAR P-2].
- CA-02.5: Quando `vendaId` for desconhecido, o sistema deve criar o registro já Cancelada (com histórico) e responder 200 [A VALIDAR D-08; alternativa 404 `VENDA_NAO_ENCONTRADA`]. Como não há itens/valor, ver Seção 7 (I-03).
- CA-02.6: Se `vendaId` ausente, então 400 `PAYLOAD_INVALIDO`.

### RF-03 Consultar status [OBRIG] — `GET /api/vendas/{vendaId}/status`
- CA-03.1: Quando a venda existir, o sistema deve responder 200 `{vendaId, status}` com status em `Pendente|Quitada|Cancelada`.
- CA-03.2: Se a venda não existir, então 404 `VENDA_NAO_ENCONTRADA`.

### RF-04 Registrar venda Pendente [SUG, A VALIDAR D-03/P-9] — `POST /api/vendas`
- CA-04.1: Quando receber payload válido (mesmo da quitação), o sistema deve persistir como Pendente e responder `{status:"Pendente"}`.
- CA-04.2: Quando a venda já existir, o sistema deve responder 200 com o status atual (idempotente).
- Se D-03 for rejeitada: RF-04 sai e "pendente" é removido do relatório com justificativa documentada.

### RF-05 Persistência e histórico [OBRIG persistir / SUG histórico]
- CA-05.1: Toda transição de estado deve gerar uma linha append-only em histórico (operação, status anterior/novo, data UTC, motivo, correlationId), nunca atualizada ou excluída.
- CA-05.2: A venda deve ser única por `vendaId` (índice/constraint UNIQUE em `VENDA_ID`), com `valorTotal` e itens persistidos como decimal (nunca float).
- CA-05.3 (Firebird): Ao atualizar uma venda, o sistema deve incrementar `VERSAO` (INT, por trigger ou aplicação) e, se a `VERSAO` lida diferir da atual no momento da gravação, deve rejeitar a escrita como conflito de concorrência, sem sobrescrever.
- CA-05.4 (Firebird): O schema é criado por DDL manual em `database/` (sem migrations); todo identificador (tabela, coluna, índice, constraint, trigger) deve ter no máximo 31 caracteres; todo DECIMAL no máximo 18 dígitos.
- CA-05.5: Ao executar o DDL em um `.fdb` novo (Firebird 3.0 embarcado), o banco deve ficar apto a subir a aplicação sem passos manuais além do README.

### RF-06 Consulta na tela [OBRIG]
Necessidade: tela de consulta (DevExpress) com filtros; desenho fica no UX-SPEC.md.
- CA-06.1: Quando o operador abrir a consulta, o sistema deve listar vendas com vendaId, clienteId, valor, status e datas.
- CA-06.2: Quando filtrar por período, cliente e/ou status [SUG S-06], o sistema deve exibir somente registros que atendam a todos os filtros combinados.
- CA-06.3: Quando selecionar uma venda, o sistema deve permitir ver o histórico [SUG S-03].
- CA-06.4: Cliente exibido/filtrado por ID [A VALIDAR D-10].

### RF-07 Relatório financeiro [OBRIG]
- CA-07.1: Quando o operador solicitar emissão a partir da tela, o sistema deve gerar o relatório FastReport (ou exportação PDF/HTML se a edição gratuita não tiver preview) com as vendas do filtro corrente.
- CA-07.2: O relatório deve exibir totais quitado, cancelado [SUG S-07] e pendente [SUG, A VALIDAR D-03; se D-03 rejeitada, omitir pendente].
- CA-07.3: Os totais do relatório devem ser iguais à soma dos valores listados (verificável por teste com massa conhecida).

### RF-08 API Key e health [SUG, A VALIDAR P-4/P-5]
- CA-08.1: Se `X-Api-Key` ausente ou inválida em qualquer endpoint exceto health, então 401 `NAO_AUTORIZADO`.
- CA-08.2: `GET /api/health` deve responder 200 `{status:"ok"}` sem autenticação.

### RF-09 Rota versionada [SUG, A VALIDAR D-05]
- CA-09.1: Todo endpoint deve responder igualmente em `/api/vendas/...` e `/api/v1/vendas/...`.

### RF-10 Entregáveis [OBRIG]
- CA-10.1: A entrega deve conter código-fonte, executável, script `.sql` e backup do banco, e README com versões, tipo de licença, instalação dos trials e passo a passo.
- CA-10.2: A entrega deve conter evidências (prints da tela, PDF de exemplo do relatório).
- CA-10.3: Uma execução em máquina limpa seguindo só o README deve subir API, tela e banco.
- CA-10.4: Quando chegar 25/09, o sistema deve estar com todos os P0 (exceto O-12) completos; 26 e 27/09 não devem receber funcionalidade nova.
- Escopo de sugestões (proposta, a validar): Tier A obrigatório na prática = S-01, S-02, S-04, S-05, S-08, S-09. Tier B (só se houver folga) = S-06, S-07, S-11 reduzido. Tier C (cai / evolução) = S-03, S-10, S-12, S-13 e o restante de S-11. Por isso, os critérios CA-06.2, CA-06.3, CA-07.2 (totais) e RNF-07, RNF-08 ficam condicionais ao Tier B/C.

### RF-11 Histórico via API [SUG P2, S-12] e Estorno formal [SUG P2, S-13]
Só se sobrar tempo; critérios a detalhar se entrarem no escopo.

## 2. Requisitos Não-Funcionais
| ID | Requisito | Tipo | Critério verificável |
|---|---|---|---|
| RNF-01 | Stack: C# .NET Framework 4.8, DevExpress WinForms, EF6, FastReport, **Firebird 3.0 embarcado (D-01 decidida)** com `EntityFramework.Firebird` + `FirebirdSql.Data.FirebirdClient`; fallback SQL Server Express só por ADR se o spike falhar | OBRIG | Referências do projeto conferem |
| RNF-12 | Banco: `.fdb` próprio do Financeiro, sem FK/tabelas cruzadas com o Vendas; DDL manual em `database/`, sem migrations; identificadores ≤ 31 chars; DECIMAL ≤ 18 dígitos; concorrência por coluna `VERSAO` INT | OBRIG (D-01/D-02) | CA-05.3 a CA-05.5; revisão do DDL |
| RNF-13 | Spike Dia 1 (~2h): EF6 grava em FIN_VENDA e detecta atualização concorrente via `VERSAO`; go/no-go registrado em ADR (ver PRD A-13) | proposto | ADR do spike existe; decisão go/no-go explícita |
| RNF-14 | Entrega do banco: script `.sql` + `.fdb` (não `.bak`); DLLs do Firebird embarcado (bitness correta) junto ao executável | OBRIG | Máquina limpa sobe (CA-10.3) |
| RNF-02 | Custo zero (trials/edições gratuitas), versões e licenças no README | OBRIG (D-09) | Nenhuma licença paga necessária |
| RNF-03 | Arquitetura em camadas com dependências para dentro; regras de negócio fora dos controllers; DTOs distintos das entidades EF | SUG (avaliado) | Revisão de referências de projeto |
| RNF-04 | Integridade: valores em DECIMAL, transição atômica (transação), concorrência otimista via `VERSAO` + UNIQUE(`VENDA_ID`) | SUG S-04 | Teste de concorrência (CA-01.7, CA-05.3) |
| RNF-05 | Datas ISO 8601 UTC; decimais como número JSON com ponto (2 casas total, 4 unitário) | A VALIDAR P-6 | Teste de contrato com exemplos JSON |
| RNF-06 | Erros em envelope `{erro:{codigo,mensagem}}`, sem stack trace ao cliente | A VALIDAR P-1 | Teste por código HTTP (400/401/404/409/500) |
| RNF-07 | Log estruturado com CorrelationId (Serilog) | SUG S-10 | Log JSON contém correlationId da requisição |
| RNF-08 | Testes unitários xUnit + Moq dos serviços de quitação/cancelamento cobrindo todas as transições de estado | SUG S-11 | Suíte verde; matriz de estados coberta |
| RNF-09 | Segurança: API Key fora do código-fonte (configuração); parametrização de consultas via EF | SUG | Revisão |
| RNF-15 | Prazo: desenvolvimento completo até 25/09/2026 (Dia 5); 26 e 27/09 só entrega (O-12, máquina limpa, buffer), sem desenvolvimento novo. Marco: API utilizável pelo Vendas até fim do Dia 3; integração real (O-11) até fim do Dia 4 | OBRIG (D-07) | Calendário do VISAO-PRODUTO Seção 6 |
| RNF-10 | Desempenho: resposta da API < 1 s para uma venda com até ~100 itens em ambiente local | proposto | Medição no teste de integração |
| RNF-11 | Compatibilidade: API integra com cliente Delphi (JSON, UTF-8) | OBRIG | O-11 |

## 3. Regras de Negócio
| ID | Regra | Racional |
|---|---|---|
| RN-01 | `vendaId` é único no Financeiro | Base da idempotência |
| RN-02 | `valorTotal` ≈ Σ(qtd × preço) com tolerância R$ 0,01 [A VALIDAR P-7] | Consistência financeira, arredondamento Delphi↔C# |
| RN-03 | Estados: Pendente, Quitada, Cancelada; Cancelada é terminal | Evita ressurreição de venda |
| RN-04 | Transições válidas: Pendente→Quitada, Pendente→Cancelada, Quitada→Cancelada (com motivo) [A VALIDAR D-06]; repetição é idempotente; demais 409 | Protege o financeiro |
| RN-05 | Histórico é append-only | Auditoria |
| RN-06 | Quitação de venda desconhecida faz upsert (cria e quita) | Vendas pode não ter registrado Pendente |
| RN-07 | Cancelar venda desconhecida cria registro Cancelada [A VALIDAR D-08] | Evita 404 que o Vendas teria de tratar |
| RN-08 | Financeiro não valida existência de cliente/produto (só referências externas) | Banco próprio [A VALIDAR D-02] |

## 4. Fluxos (texto; telas ficam no UX-SPEC.md)
**Quitação:** Vendas envia → autenticar (401) → validar payload (400) e total (400) → buscar venda → {inexistente: criar+quitar | Pendente: quitar | Quitada: retornar original (idempotente) | Cancelada: 409} → persistir venda/itens/histórico em transação → conflito de concorrência: reler e reavaliar → 200.
**Cancelamento:** autenticar → validar → buscar → {inexistente: criar Cancelada (D-08) | Pendente: cancelar | Quitada: exige motivo, senão 409 | Cancelada: 200 idempotente} → histórico → 200.
**Timeout no Vendas:** reenvia a mesma operação (idempotência) e/ou consulta `GET status` para reconciliar.
**Consulta/relatório:** operador define filtros → lista → (opcional) histórico → emite relatório com o mesmo filtro. Alternativo: sem resultados, relatório vazio com totais zero; falha de banco, mensagem de erro e log.

## 5. Dependências e Integrações
- Dependências: RF-01/02/03/06/07 dependem de RF-05 (persistência); RF-07 depende de RF-06 (filtro) e de D-03 (pendente); RF-04 depende de D-03; RF-08 e RF-09 independentes; O-11 depende de O-07 e contrato v1.1 aceito.
- Integração externa: ERP Vendas (Delphi) via REST/JSON (única integração). Sem acesso ao código; contrato JSON é a referência.
- Ferramentas: Firebird 3.0 embarcado (D-01 decidida; fallback SQL Server Express), providers EF6 Firebird, DevExpress WinForms trial, FastReport trial/Open Source, Serilog, Autofac, xUnit, Moq.
- Restrição: FastReport Open Source pode não ter preview WinForms (exportar PDF/HTML). Confirmar antes de O-10.

## 6. Premissas e Riscos Resolvidos
Sem acesso a fontes externas nesta rodada; nenhuma premissa do PRD foi refutada ou confirmada com evidência. Estado: **A-02…A-11 do PRD permanecem "A VALIDAR"** (D-03…D-06, D-08, D-10, D-11 e contrato v1.1); A-01 (D-01 Firebird), A-02 (D-02), A-07 (D-07, prazo de dev 25/09) e A-12 (D-09) são decisões do usuário; A-13 (spike Firebird) pendente. Validar com o usuário e com o lado Vendas até o Dia 1.

## 7. Interpretações Registradas
| ID | Ambiguidade | Interpretação escolhida | Porquê |
|---|---|---|---|
| I-01 | "Consulta" e "relatório" do desafio: o desafio pede só dados financeiros? | Consulta lista todas as vendas com filtros; relatório reflete o filtro | Menor ambiguidade e reaproveita a consulta |
| I-02 | "Pendente" sem endpoint no contrato-base | Só existe com D-03/P-9; caso contrário removido do relatório | Não inventar estado inalcançável |
| I-03 | Cancelamento de venda desconhecida não traz `clienteId`/`valorTotal` | Registro criado com `clienteId` e `valorTotal` nulos/zero e sem itens: exige que o Coordenador decida colunas anuláveis (o modelo atual tem NOT NULL) | Conflito do modelo com D-08; sinalizar ao Coordenador. Se rejeitado, usar 404 |
| I-03b | Firebird: colunas de I-03 | Segue em aberto (D-08 não decidida): `CLIENTE_ID` e `VALOR_TOTAL` precisam ser anuláveis se D-08 for adotada | Sem mudança na rodada 2 |
| I-04 | "Quitar venda Pendente" com payload diferente do registrado | Prevalece o registrado; divergência de valor gera 409 `DADOS_DIVERGENTES` (proposto, novo código de erro a validar) | Evita alterar valor financeiro silenciosamente |
| I-05 | Idempotência no cancelamento repetido com motivo diferente | Mantém o primeiro motivo; 200 | Histórico imutável |
| I-06 | RNF-10 sem meta do desafio | Meta proposta de 1 s local | Torna o requisito verificável |
