# VISAO-PRODUTO.md — ERP Financeiro (C#)

> Desafio técnico CartSys — Programador Delphi e C# Sênior/Especialista
> Autor: Leandro Segheto Moraes · Data: 21/09/2026 · Versão: 0.1 (fase de planejamento — nenhum código escrito)
> Base: `Documento_de_Visao_ERP_Financeiro_CSharp.pdf` v1.0 + contrato de integração acordado com o ERP Vendas (Delphi).

**Legenda:** 🟦 **[OBRIG]** requisito explícito do desafio · 🟧 **[SUGESTÃO]** sugestão adicional do autor · ❓ decisão em aberto (Seção 7).

---

## 1. Problema, objetivo e escopo

**Problema.** O ERP Vendas (Delphi) precisa de um módulo financeiro separado que quite e cancele vendas, mantenha o registro financeiro de cada uma e permita consultar e emitir relatório do que foi processado.

**Objetivo.** Entregar o módulo **ERP Financeiro** em C# (.NET Framework 4.8) que **expõe** uma API REST/JSON consumida pelo ERP Vendas, persiste o registro financeiro e oferece consulta (DevExpress WinForms) e relatório (FastReport). O módulo é avaliado em: padrão de desenvolvimento, linha de raciocínio, boas práticas e maturidade técnica.

**Público.** Avaliadores da CartSys (leitura do código "linha a linha") e o ERP Vendas como cliente da API.

**Dentro do escopo (MVP):** quitação, cancelamento, consulta de informações financeiras, relatório financeiro, API REST para o Vendas.
**Fora do escopo:** contas a pagar/receber genéricas, conciliação bancária, multiempresa/multimoeda.

**Stack obrigatória:** C# / .NET Framework 4.8 · DevExpress WinForms · Entity Framework 6 (não EF Core) · FastReport · Firebird 3.0 **ou** SQL Server 2020/2022.

**Critério de sucesso.** Fluxo ponta a ponta funcionando com o Vendas real: Vendas envia quitação → Financeiro valida, persiste e responde → venda aparece na consulta e no relatório. Entregáveis: código-fonte, executável, script/backup do banco, documentação de execução.

### 1.1 Licenciamento: custo zero (decisão D-09)

Por ser um desafio de processo seletivo, a stack obrigatória é mantida **sem compra de licença**:

| Componente | Estratégia sem custo | Ressalvas a confirmar no Dia 1 |
|---|---|---|
| .NET Framework 4.8 / EF6 / Web API 2 / OWIN | Gratuitos (NuGet) | — |
| Firebird 3.0 embarcado (D-01) | Gratuito (licença IDPL/IBPL), DLLs embarcadas junto ao executável | Bitness x86/x64 das DLLs; evitar dois processos no mesmo `.fdb` |
| SQL Server 2022 (só fallback) | Express ou LocalDB (gratuitos) | Limite de 10 GB por banco, irrelevante aqui |
| DevExpress WinForms | **Trial** oficial (registro no site da DevExpress) | Duração do trial (em geral 30 dias, confirmar), possível aviso/marca de avaliação nos controles e necessidade de o avaliador ter o mesmo trial para rodar o executável. Cobre os 7 dias do desafio |
| FastReport | Preferir **FastReport .NET Trial**; alternativa: edição *Open Source* (gratuita) | A edição Open Source é menor (pode não trazer designer/preview WinForms); se usada, emitir o relatório exportando para PDF/HTML e abrindo no visualizador do sistema. Confirmar antes de O-10 |
| Serilog, Autofac, xUnit, Moq | Gratuitos | — |

**Consequência para a entrega:** o README (O-12) informa versões e tipo de licença usados, como instalar os trials e o que fazer se expirarem. A entrega inclui **evidências** (prints da tela e PDF de exemplo do relatório) para o avaliador ver o resultado mesmo sem os trials instalados. Se algum trial se mostrar inviável, a substituição só ocorre com registro em ADR e aviso, por ser stack obrigatória.

---

## 2. Contrato de integração

### 2.1 Contrato-base (ponto de partida acordado)

| Método/Rota | Request | Response |
|---|---|---|
| `POST /api/vendas/quitacao` | `{ vendaId, clienteId, valorTotal, itens: [{ produtoId, quantidade, precoUnitario }] }` | `{ status: "Quitada", dataQuitacao: ISO 8601 }` |
| `POST /api/vendas/cancelamento` | `{ vendaId, motivo? }` | `{ status: "Cancelada" }` |
| `GET /api/vendas/{vendaId}/status` | — | `{ vendaId, status }` |

### 2.2 Lacunas identificadas no contrato-base

1. **Sem tratamento de erro definido** (venda inexistente, já cancelada, payload inválido, falha de autenticação).
2. **Sem regra de idempotência** — o Vendas pode reenviar após timeout (risco já citado no doc de visão, seção 10).
3. **Não há como uma venda ficar "Pendente"**: o contrato só tem quitação/cancelamento, mas o relatório sugerido pede "total pendente".
4. **Cancelar venda inexistente** no Financeiro (nunca quitada): erro ou registro já como Cancelada?
5. **`valorTotal` × itens**: sem regra de consistência; decimal em JSON entre Delphi e C# exige cuidado com arredondamento.
6. **Rota**: o doc de visão sugere `/api/v1/vendas`; o contrato acordado usa `/api/vendas`.
7. **Autenticação, health check e formato de data/hora** não especificados.

### 2.3 Refinamentos propostos (todos aditivos — o Vendas só precisa ignorar campos que não conhece)

| # | Proposta | Status |
|---|---|---|
| P-1 | Envelope de erro: `{ "erro": { "codigo": "VENDA_JA_CANCELADA", "mensagem": "..." } }`. HTTP: `400` payload inválido · `401` API Key ausente/inválida · `404` venda inexistente · `409` conflito de estado · `500` erro interno | ⏳ validar com Vendas |
| P-2 | **Idempotência:** quitação repetida de venda já Quitada → `200` com a mesma `dataQuitacao` original. Cancelamento repetido → `200 Cancelada`. Quitar venda Cancelada → `409` | ⏳ validar |
| P-3 | Valores de `status`: `"Pendente"`, `"Quitada"`, `"Cancelada"` (string, PascalCase) | ⏳ validar |
| P-4 | Autenticação por header `X-Api-Key` em todos os endpoints, exceto health 🟧 | ⏳ validar |
| P-5 | `GET /api/health` → `200 { "status": "ok" }` sem autenticação 🟧 | ⏳ validar |
| P-6 | Datas em ISO 8601 UTC (`2026-09-21T17:30:00Z`); decimais como **número JSON** com ponto e até 2 casas (`valorTotal`) / 4 (`precoUnitario`) | ⏳ validar |
| P-7 | Validação `valorTotal ≈ Σ(quantidade × precoUnitario)` com tolerância de R$ 0,01; divergência → `400 VALOR_TOTAL_DIVERGENTE` | ⏳ validar |
| P-8 | Rota canônica mantém `/api/vendas/...` (contrato acordado); alias `/api/v1/vendas/...` mapeado ao mesmo controller | ❓ D-05 |
| P-9 | Novo endpoint opcional `POST /api/vendas` (registrar venda como **Pendente**, mesmo payload da quitação, resposta `{ status: "Pendente" }`) para viabilizar "total pendente". Quitação passa a fazer *upsert* (cria se não existir) | ❓ D-03 |

### 2.4 Registro de mudanças do contrato

> O Vendas segue esta mesma referência. **Toda alteração aprovada deve ser registrada aqui e comunicada ao outro lado.**

| Data | Versão | Mudança | Aprovada por |
|---|---|---|---|
| 21/09/2026 | 1.0 | Contrato-base (Seção 2.1) | Leandro |
| — | 1.1 | *(a preencher quando P-1…P-9 forem validadas)* | — |

---

## 3. Estrutura de projeto (camadas)

Solução única `ERPFinanceiro.sln`, .NET Framework 4.8, dependências apontando sempre para dentro (Domain não depende de nada):

```
ERPFinanceiro.sln
├── src/
│   ├── ERPFinanceiro.Domain           Entidades, enums (StatusVenda), regras de estado, exceções de domínio
│   ├── ERPFinanceiro.Application      Serviços de negócio (QuitacaoService, CancelamentoService,
│   │                                  ConsultaService), interfaces de repositório, comandos/resultados, logging (abstração)
│   ├── ERPFinanceiro.Infrastructure   EF6: DbContext, mapeamentos Fluent, repositórios, migrations,
│   │                                  Unit of Work/transação, implementação de log (Serilog)
│   ├── ERPFinanceiro.Api              ASP.NET Web API 2 sobre OWIN: controllers finos, DTOs próprios,
│   │                                  filtro de API Key, handler global de exceção→envelope de erro, roteamento
│   ├── ERPFinanceiro.Reports          Layouts .frx (FastReport), datasource do relatório
│   └── ERPFinanceiro.Desktop          WinForms + DevExpress (consulta com filtros, emissão do relatório);
│                                      composition root (Autofac) e host da API
├── tests/
│   └── ERPFinanceiro.Tests            Testes unitários dos serviços (repositórios fake/mock)
├── database/                          Script de criação (DDL) + seed + backup
└── docs/                              Execução, configuração, decisões (ADRs), contrato
```

**Regras de dependência**

| Camada | Pode referenciar | Não pode |
|---|---|---|
| Domain | — | qualquer outra |
| Application | Domain | EF, Web API, DevExpress |
| Infrastructure | Application, Domain | Api, Desktop |
| Api | Application (+ Domain só para enums) | Infrastructure, entidades EF na resposta |
| Desktop | Application, Reports, Api (só para subir o host) | acesso direto ao DbContext |

**Convenções.** Controllers apenas traduzem DTO ↔ comando e chamam o serviço; **toda regra de quitação/cancelamento vive em Application/Domain**; DTOs da API nunca são entidades EF; DI com Autofac (`Autofac.WebApi2`); mapeamento manual DTO↔domínio (evita AutoMapper por custo/benefício em escopo pequeno).

**Hospedagem da API (recomendação — ❓ D-04):** OWIN self-host (`Microsoft.Owin.Host.HttpListener`) subido **dentro do executável Desktop**, em `http://localhost:{porta}/`. Um único executável para entregar, sem IIS, sem `netsh urlacl` (localhost não exige admin). O código do host fica isolado em `Api` para poder ser movido para serviço Windows/IIS sem tocar nas camadas de negócio.

---

## 4. Modelo de dados inicial

**Princípios:** (a) o Financeiro possui seu próprio banco; `VendaId` do Vendas é só referência externa, sem FK entre bancos (❓ D-02); (b) histórico é **append-only** — nunca atualizado/excluído; (c) valores monetários em `DECIMAL`, nunca float.

### 4.1 Tabelas

**`FIN_VENDA`** — registro financeiro atual (uma linha por venda)

| Coluna | Tipo | Observações |
|---|---|---|
| `ID` | INT/BIGINT PK identity | chave interna |
| `VENDA_ID` | VARCHAR(50) **UNIQUE** NOT NULL | id externo vindo do Vendas — base da idempotência/concorrência |
| `CLIENTE_ID` | VARCHAR(50) NOT NULL | índice (filtro por cliente) |
| `VALOR_TOTAL` | DECIMAL(18,2) NOT NULL | |
| `STATUS` | TINYINT NOT NULL | 0 Pendente · 1 Quitada · 2 Cancelada; índice |
| `DATA_RECEBIMENTO` | DATETIME2 NOT NULL | UTC |
| `DATA_QUITACAO` | DATETIME2 NULL | UTC |
| `DATA_CANCELAMENTO` | DATETIME2 NULL | UTC |
| `MOTIVO_CANCELAMENTO` | VARCHAR(500) NULL | |
| `VERSAO` | INT NOT NULL DEFAULT 0 | token de concorrência otimista (Firebird não tem rowversion): incrementado por trigger `BEFORE UPDATE` ou pela aplicação; EF6 `IsConcurrencyToken` |

**`FIN_VENDA_ITEM`**

| Coluna | Tipo | Observações |
|---|---|---|
| `ID` | PK | |
| `VENDA_REF` | FK → `FIN_VENDA.ID` | `ON DELETE CASCADE` |
| `PRODUTO_ID` | VARCHAR(50) NOT NULL | |
| `QUANTIDADE` | INT NOT NULL | `CHECK > 0` |
| `PRECO_UNITARIO` | DECIMAL(18,4) NOT NULL | `CHECK >= 0` |

**`FIN_VENDA_HISTORICO`** — trilha de auditoria (1 linha por transição/tentativa relevante)

| Coluna | Tipo | Observações |
|---|---|---|
| `ID` | PK | |
| `VENDA_REF` | FK → `FIN_VENDA.ID` | índice (venda, data) |
| `OPERACAO` | TINYINT | 0 Recebida · 1 Quitação · 2 Cancelamento |
| `STATUS_ANTERIOR` / `STATUS_NOVO` | TINYINT | |
| `DATA_HORA` | DATETIME2 NOT NULL | UTC |
| `MOTIVO` | VARCHAR(500) NULL | |
| `CORRELATION_ID` | VARCHAR(50) NULL | liga ao log estruturado 🟧 |

### 4.2 Máquina de estados

```
            ┌──────── quitar ────────▶ Quitada ──── cancelar (exige motivo*) ───┐
Pendente ───┤                                                                    ▼
            └──────── cancelar ──────────────────────────────────────────▶ Cancelada (terminal)
```
\* regra 🟧 do doc de visão: cancelar venda já quitada só com justificativa explícita (❓ D-06). Transições repetidas são idempotentes (P-2); demais transições inválidas → `409`.

### 4.3 Concorrência 🟧

Duas quitações simultâneas da mesma venda: `UNIQUE(VENDA_ID)` impede duplicidade na criação; `VERSAO` (INT) + transação (`DbContext.Database.BeginTransaction`) faz o segundo escritor falhar com `DbUpdateConcurrencyException`, tratada como *reler e reavaliar* (resultado idempotente ou `409`).

### 4.4 Diferenças por banco (D-01 decidida: Firebird 3.0 embarcado; SQL Server só como fallback)

> Modelo das Seções 4.1 a 4.3 adotado em Firebird: DDL manual em `database/`, sem migrations, nomes ≤ 31 chars, DECIMAL ≤ 18 dígitos, `VERSAO` no lugar de `ROW_VERSION`, `TINYINT` vira `SMALLINT`, `DATETIME2` vira `TIMESTAMP` (UTC), PK por `GENERATED BY DEFAULT AS IDENTITY` (FB3) ou generator. `UNIQUE(VENDA_ID)` mantido.

| Aspecto | SQL Server 2022 (Express/LocalDB) | Firebird 3.0 |
|---|---|---|
| Provider EF6 | Nativo (`System.Data.SqlClient`) | `FirebirdSql.Data.FirebirdClient` + `EntityFramework.Firebird` |
| Migrations | Suportadas | Limitadas — usar DDL manual |
| Concorrência otimista | `rowversion` | Sem equivalente nativo → coluna `VERSAO INT` incrementada por trigger/aplicação |
| Identificadores | 128 chars | ≤ 31 chars (FB3) — nomes acima já respeitam |
| DECIMAL | até 38 dígitos | até 18 dígitos (suficiente) |
| Entrega | script `.sql` + `.bak` | script `.sql` + `.fdb` |

---

## 5. Tarefas priorizadas

Prioridade: **P0** = sem isso o desafio não cumpre requisito · **P1** = alto valor/baixo custo · **P2** = só se sobrar tempo.
Estimativas em horas de trabalho efetivo (referência, não compromisso).

### 5.1 Requisitos obrigatórios 🟦

| ID | Tarefa | Prio | Est. |
|---|---|---|---|
| O-01 | Instalar trials (Seção 1.1), fechar decisões D-03/D-04 e congelar contrato v1.1 com o lado Vendas. **Inclui spike de ~2h no início do Dia 1** (EF6 + Firebird embarcado gravando em `FIN_VENDA` e atualização concorrente detectada por `VERSAO`). **Go:** gravação, leitura de DECIMAL e conflito de concorrência funcionam. **No-go (fallback SQL Server Express):** qualquer um falha e não se resolve em +1h. Registrar em ADR | P0 | 4h (era 2h) |
| O-02 | Esqueleto da solução (projetos, referências, NuGet: EF6, Web API 2, OWIN, DevExpress, FastReport) | P0 | 2h |
| O-03 | Domain + mapeamento EF6 (provider Firebird) + DDL manual em `database/` das 3 tabelas (sem migrations; nomes ≤ 31 chars; trigger/regra de `VERSAO`) | P0 | 5h (era 4h) |
| O-04 | Repositórios EF6 e unidade de trabalho/transação (tratamento de conflito por `VERSAO`) | P0 | 3h |
| O-05 | `QuitacaoService` (valida, persiste venda+itens+histórico, retorna status) | P0 | 4h |
| O-06 | `CancelamentoService` | P0 | 3h |
| O-07 | Endpoints `POST quitacao`, `POST cancelamento`, `GET {id}/status` + DTOs + envelope de erro | P0 | 5h |
| O-08 | Host OWIN no executável Desktop + configuração (porta, connection string) | P0 | 2h |
| O-09 | Tela de consulta DevExpress (GridControl com dados financeiros das vendas) | P0 | 5h |
| O-10 | Relatório financeiro FastReport (listagem das vendas) chamado pela tela | P0 | 5h |
| O-11 | Teste de integração real com o ERP Vendas (Delphi) e correções | P0 | 5h |
| O-12 | Entregáveis: executável, script + backup do banco, README de execução/dependências/licenças | P0 | 4h |

### 5.2 Sugestões adicionais 🟧

| ID | Tarefa | Prio | Est. | Observação |
|---|---|---|---|---|
| S-01 | Arquitetura em camadas | P0 | — | Já embutida em O-02…O-07; custo zero, mas estrutural |
| S-02 | DTOs próprios + rota versionada (`/api/v1` alias) | P0 | 1h | Embutido em O-07 |
| S-03 | Data/hora da quitação + histórico consultável (`FIN_VENDA_HISTORICO` + aba na tela) | P1 | 3h | Modelo já previsto em O-03 |
| S-04 | Controle de concorrência + idempotência (UNIQUE + `VERSAO` INT + transação) | P1 | 3h | Difícil de retrofitar — fazer junto de O-05 |
| S-05 | Bloquear cancelamento de venda quitada sem motivo/estorno | P1 | 1h | Regra em O-06 |
| S-06 | Filtros por período, cliente e status na consulta | P1 | 2h | |
| S-07 | Totais quitado / cancelado / pendente no relatório | P1 | 2h | Depende de D-03 para "pendente" |
| S-08 | Autenticação por API Key (`X-Api-Key`) | P1 | 1,5h | |
| S-09 | Health check `GET /api/health` | P1 | 0,5h | |
| S-10 | Log estruturado (Serilog, arquivo JSON, CorrelationId) | P1 | 2h | |
| S-11 | Testes unitários dos serviços de quitação/cancelamento (xUnit + Moq) | P1 | 4h | Escrever junto de O-05/O-06 |
| S-12 | Endpoint de consulta do histórico via API | P2 | 1h | |
| S-13 | Estorno explícito como transição própria (status/operação "Estornada") | P2 | 3h | Só se D-06 optar por estorno formal |

---

## 6. Cronograma (5 dias de desenvolvimento + 2 de buffer/entrega)

✅ **D-07 decidida (rodada 2):** prazo de **desenvolvimento = sexta 25/09/2026** (21/09 é segunda). **Dia 1 = 21/09 … Dia 5 = 25/09** (dev completo). **26 e 27/09 = buffer/entrega final** (O-12: README, teste em máquina limpa, empacotamento, evidências), **sem desenvolvimento novo**.

### 6.1 Checagem de capacidade (proposta, a validar com o usuário)

Horas referência (Seção 5), com O-01 = 4h (spike) e O-03 = 5h: P0 dentro dos 5 dias = O-01 4 + O-02 2 + O-03 5 + O-04 3 + O-05 4 + O-06 3 + O-07 5 + O-08 2 + O-09 5 + O-10 5 + O-11 5 = **43h** (O-12, 4h, vai para 26-27/09). Sugestões P1 = S-03 3 + S-04 3 + S-05 1 + S-06 2 + S-07 2 + S-08 1,5 + S-09 0,5 + S-10 2 + S-11 4 = **19h**. P0 + P1 = **62h** (+ O-12 4h no buffer).

| Capacidade assumida | Horas em 5 dias | Cabe P0 (43h)? | Cabe P0 + P1 (62h)? |
|---|---|---|---|
| 8h/dia | 40h | **Não** (faltam 3h) | Não |
| 10h/dia | 50h | Sim, com 7h de folga | **Não** (faltam 12h) |
| 12h/dia | 60h | Sim | Quase (faltam 2h, sem margem) |

**Conclusão:** P0 + P1 **não cabem** em 5 dias com jornada normal. Mesmo P0 exige ~9 a 10h/dia. **O usuário precisa confirmar as horas/dia.** Proposta de escopo (recomendação, não decisão):
- **Manter (Tier A, ~49h ≈ 10h/dia):** P0 + S-01, S-02 (embutido), S-04, S-05, S-08, S-09.
- **Se houver folga (Tier B, +6h):** S-06 (filtros, 2h), S-07 (totais, 2h; depende de D-03), S-11 reduzido (~2h: só a máquina de estados de quitação/cancelamento).
- **Cai / vai para "evolução" (Tier C):** S-13, S-12, S-10 (log), S-03 (histórico na tela; o histórico continua gravado no banco), restante de S-11.

### 6.2 Cronograma

| Dia | Data | Foco | Entregas do dia (Tier A) |
|---|---|---|---|
| 1 | 21/09 (seg) | Spike + fundação | **Spike Firebird (~2h, primeira coisa, go/no-go)**, O-01 (contrato v1.1 enviado ao Vendas), O-02, O-03 (~11h) |
| 2 | 22/09 (ter) | Núcleo de negócio | O-04, O-05 (+S-04), S-05, começo de O-06 (~11h) |
| 3 | 23/09 (qua) | API | O-06 (fim), O-07 (+S-02), O-08 · testado via Postman/curl · **marco: API utilizável pelo Vendas ao fim do Dia 3** (antecipar para o meio do Dia 3 se possível: quitação e status primeiro) (~10h) |
| 4 | 24/09 (qui) | Integração + consulta | S-08, S-09 (2h), **O-11 integração real com o Vendas (até o fim do Dia 4)**, O-09 tela de consulta (~12h) |
| 5 | 25/09 (sex) | Relatório + fechamento do dev | O-10, correções da integração; Tier B se houver folga (S-06, S-07, S-11 reduzido). **Fim do desenvolvimento** |
| 6 | 26/09 (sáb) | Entrega | O-12 (README, licenças/trials, evidências: prints e PDF do relatório), build limpo, teste em máquina "zerada". Sem código novo (só correção de bug bloqueante) |
| 7 | 27/09 (dom) | Buffer | Imprevistos, empacotamento final |

**Ordem de corte se o prazo apertar** (do primeiro a cair): S-13 → S-12 → S-10 → S-03 → S-11 (manter só o mínimo da máquina de estados) → S-07 → S-06 → S-09. **Nunca cortar:** O-01…O-12, S-01, S-04 (integridade financeira), S-05 e S-08. Se P0 estourar, o que cai primeiro é qualidade de tela/relatório dentro de O-09/O-10 (layout simples), não integração nem persistência.

**Marco crítico:** fim do Dia 3 (API utilizável pelo Vendas), com integração real (O-11) concluída até o fim do Dia 4. Contrato v1.1 fechado no Dia 1.

---

## 7. Decisões em aberto (validar antes de implementar)

| ID | Decisão | Recomendação | Justificativa / impacto |
|---|---|---|---|
| **D-01** | Firebird 3.0 ou SQL Server? | ✅ **Decidida (rodada 2): Firebird 3.0 embarcado**, um `.fdb` próprio (o Vendas também usa Firebird 3.0). **Fallback:** SQL Server 2022 Express/LocalDB somente se o spike do Dia 1 falhar (go/no-go em O-01). | Provider `EntityFramework.Firebird` + `FirebirdSql.Data.FirebirdClient`; sem migrations (DDL manual); concorrência por `VERSAO` INT; ver Seção 4.4. Risco: provider menos maduro, mitigado pelo spike. |
| **D-02** | Banco compartilhado com o Vendas ou próprio? | **Banco próprio (segregado)** — mantida (rodada 2): `.fdb` próprio, sem FK/tabelas cruzadas com o Vendas | Módulos desacoplados, só a API integra; sem FK entre bancos; comportamento realista de dois sistemas. Custo: `VendaId`/`ClienteId` são referências externas sem integridade. Registrar como ADR. |
| **D-03** | Como uma venda fica "Pendente"? | **Adotar P-9** (`POST /api/vendas` opcional; quitação faz *upsert*) | Sem isso "total pendente" (S-07) é sempre zero. Alternativa: remover "pendente" do relatório e documentar o motivo. |
| **D-04** | Hospedagem da API no .NET 4.8 | **OWIN self-host dentro do Desktop** (Seção 3) | Um executável, sem IIS. Alternativas: serviço Windows separado (mais "produção", 1 executável a mais) ou IIS/IIS Express (dependência de configuração na máquina do avaliador). |
| **D-05** | Rota: `/api/vendas` (acordada) ou `/api/v1/vendas` (doc de visão)? | **Manter `/api/vendas/...` e expor alias `/api/v1/vendas/...`** | Não quebra o que o Vendas já implementa e ainda atende a sugestão de versionamento. |
| **D-06** | Cancelar venda já Quitada | **Permitir apenas com `motivo` preenchido**, registrando no histórico; sem motivo → `409` | Contrato-base define `motivo` opcional; esta regra o torna obrigatório só nesse caso. Estorno formal (S-13) fica como evolução. |
| **D-07** | Data limite exata | ✅ **Decidida (rodada 2): desenvolvimento até sexta 25/09/2026**; 26 e 27/09 = buffer/entrega final | Cronograma na Seção 6. Capacidade não comporta P0 + P1 (ver 6.1). |
| **D-08** | Cancelar venda desconhecida do Financeiro | **Criar registro já como Cancelada** (com histórico) | Vendas pode cancelar antes de qualquer quitação; evita `404` que o Vendas teria de tratar. Alternativa: `404`. |
| **D-09** | Licenças DevExpress e FastReport | ✅ **Decidido: custo zero.** Manter a stack e usar trial/edição gratuita (Seção 1.1) | Nenhuma compra. Instalar os trials no Dia 1, antes de O-02. Registrar no README versão e tipo de licença usados. |
| **D-10** | Nome do cliente | Contrato só traz `clienteId` — **filtrar/exibir por ID** | Sem cadastro de clientes no Financeiro. Se o Vendas puder enviar `clienteNome` (campo aditivo), a consulta/relatório ficam mais úteis. |
| **D-11** | Framework de teste | **xUnit + Moq** | Compatível com net48. |

---

## 8. Riscos

| Risco | Mitigação |
|---|---|
| EF6 ≠ EF Core (API, configuração, migrations) | Mapeamento Fluent explícito, DDL versionado em `database/`, testar migration cedo (Dia 1) |
| Timeout/indisponibilidade entre módulos → status divergente | Idempotência (P-2) + `GET status` como reconciliação + log com CorrelationId |
| Prazo apertado (dual-stack) | Obrigatórios primeiro; ordem de corte da Seção 6 |
| Contrato divergir entre os dois repositórios | Seção 2.4 como fonte única; toda mudança comunicada ao Vendas |
| Diferença Delphi↔C# em decimal/data | P-6 e P-7 explícitos; exemplos JSON reais no `docs/` |
| Trials expiram ou exibem marca d'água/aviso na máquina do avaliador | Seção 1.1: instalar cedo, README com passo a passo, evidências (prints/PDF do relatório) na entrega |
| Avaliador não conseguir rodar | README com passo a passo, script de banco, teste em máquina limpa no Dia 7 |
