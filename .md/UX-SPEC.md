# UX-SPEC.md — ERP Financeiro (C#)

> **APROVADO v0.1 (usuário, 22/09/2026).** Autor: Coordenador (chapéu UX/UI). Base: `PRD-TECNICO.md` (RF-06, RF-07, RF-08), `SDD.md` (limites técnicos).
> Aplicativo WinForms local para um operador; sem mockup de alta fidelidade. Wireframes em ASCII.
> **Marcadores de tier** (escopo do PRD): **[A]** obrigatório · **[B]** só se sobrar tempo · **[C]** cai para evolução. A tela deve funcionar em [A] e crescer por camadas sem redesenho.
> Itens **[A VALIDAR]** seguem as recomendações assumidas no SDD (D-03 pendente, D-08 cancelada sem quitação, D-10 cliente por ID).

## 1. Fluxos de Tela

Uma janela principal (`FrmConsulta`), um diálogo modal de detalhe (`FrmDetalheVenda`) e a visualização do relatório (`FrmRelatorio` ou PDF externo). Sem login (SDD 7).

```
Abrir app -> [inicia banco + host API] -> FrmConsulta (carrega lista)
   |-- filtrar [B] -> lista atualizada
   |-- duplo clique / Enter em linha -> FrmDetalheVenda (itens; histórico [C]) -> Fechar (Esc)
   |-- Emitir relatório -> (mesmo filtro da grid) -> preview FastReport | PDF externo
   |-- Atualizar (F5)
   `-- Fechar app -> para host API (confirmação se houver operação em andamento)
```

| Fluxo | RF | Tier |
|---|---|---|
| F-1 Consultar vendas | RF-06 CA-06.1 | A |
| F-2 Filtrar por período, cliente, status | CA-06.2 | B |
| F-3 Detalhe: cabeçalho + itens | CA-06.3 (parcial) | A (itens: A; histórico: C) |
| F-4 Ver histórico da venda | CA-06.3 | C (aba oculta se cortado; dados já gravados) |
| F-5 Emitir relatório com filtro corrente | RF-07 | A (totais quitado/cancelado B; pendente B/A VALIDAR D-03) |
| F-6 Ver estado da API/banco | RF-08 | A |
| F-7 Falha de inicialização (banco/porta) | RF-05, ADR-004 | A |

## 2. Wireframes

### 2.1 FrmConsulta (janela principal)

```
+--------------------------------------------------------------------------------+
| ERP Financeiro - Consulta de Vendas                                   [_][o][x]|
+--------------------------------------------------------------------------------+
| Filtros [B]                                                                    |
| Período: [dd/mm/aaaa]  a  [dd/mm/aaaa]   Cliente (ID): [__________]            |
| Status: [Todos v]                        [Aplicar (Enter)] [Limpar]            |
+--------------------------------------------------------------------------------+
| Venda ID   | Cliente ID | Valor total | Status     | Recebida em | Quitada em |  |
|------------|------------|-------------|------------|-------------|------------|  |
| V-1001     | C-77       |    1.250,00 | Quitada    | 21/09 10:12 | 21/09 10:12|  |
| V-1002     | C-12       |      310,50 | Pendente   | 21/09 10:15 | -          |  |
| V-1003     | -          |           - | Cancelada* | 21/09 10:20 | -          |  |
| ...                                                       (GridControl)         |
+--------------------------------------------------------------------------------+
| [Atualizar F5]  [Detalhe Enter]  [Emitir relatório]         37 vendas          |
+--------------------------------------------------------------------------------+
| API: [(o) Ativa http://localhost:5000]  Banco: [(o) OK]      * ver legenda     |
+--------------------------------------------------------------------------------+
```

- Colunas: VendaId, ClienteId (texto; D-10), Valor total (`N2`, alinhado à direita), Status (texto + ícone), Recebida em, Quitada em, Cancelada em (oculta por padrão, selecionável no column chooser). Datas UTC no banco exibidas em **hora local** com o fuso no tooltip.
- Grid **somente leitura**, ordenável, filtro automático do DevExpress desligado em [A] para não competir com os filtros do painel; ordenação padrão: `Recebida em` decrescente.
- `*` Cancelada sem quitação prévia (ADR-007): Cliente e Valor exibem "—" e a linha tem tooltip "Cancelada sem quitação registrada; sem cliente/valor".
- Limite de 5.000 linhas com aviso na barra ("Mostrando as 5.000 mais recentes; refine o filtro") (SDD RT-09).
- Rodapé de totais na grid (soma por status) apenas em [B].

### 2.2 FrmDetalheVenda (modal)

```
+--------------------------------------------------------------+
| Venda V-1001                                              [x]|
| Cliente: C-77   Status: Quitada   Valor: 1.250,00            |
| Recebida: 21/09/2026 10:12   Quitada: 21/09/2026 10:12       |
| Cancelamento/Motivo: -                                       |
+--------------------------------------------------------------+
| [Itens]  [Histórico [C]]                                     |
| Produto ID | Quantidade | Preço unit. | Subtotal             |
| P-10       |          2 |     100,0000|   200,00             |
| P-22       |          1 |   1.050,0000| 1.050,00             |
|                                          Total itens: 1.250,00|
+--------------------------------------------------------------+
| Histórico [C]: Data/hora | Operação | De -> Para | Motivo | CorrelationId |
+--------------------------------------------------------------+
|                                                      [Fechar]|
+--------------------------------------------------------------+
```

- Preço unitário com 4 casas, valores totais com 2. Somente leitura; sem editar/quitar/cancelar (mutação é só via API).
- Venda cancelada sem itens: aba Itens mostra "Sem itens registrados (venda cancelada sem quitação prévia)". Histórico [C]: se o tier for cortado, a aba não é exibida (sem aba vazia enganosa).

### 2.3 Emissão do relatório (F-5)
Botão **Emitir relatório** usa o **mesmo filtro aplicado** na grid (exibido no cabeçalho do relatório: período, cliente, status, data de emissão).

```
RELATÓRIO FINANCEIRO DE VENDAS          Emitido em 21/09/2026 14:03
Filtro: 01/09/2026 a 21/09/2026 | Cliente: (todos) | Status: (todos)
VendaId | ClienteId | Valor | Status | Recebida | Quitada | Cancelada
...
---------------------------------------------------------------
Total quitado:   R$ 12.500,00  (n vendas)      [B]
Total cancelado: R$    800,00  (n vendas)      [B]
Total pendente:  R$  1.900,00  (n vendas)      [B / A VALIDAR D-03]
Total listado:   R$ 15.200,00  (soma dos valores listados)
```

- Em [A] o relatório traz a listagem e o "Total listado"; totais por status são [B]. Se D-03 for rejeitada, a linha "pendente" é removida.
- Vendas com valor nulo (cancelada sem quitação) entram como 0 e são contadas (nota de rodapé).
- Comportamento por edição FastReport (ADR-008): com preview -> `FrmRelatorio` com barra (imprimir, exportar PDF); sem preview -> gera PDF em pasta temporária e abre no visualizador padrão; se não abrir, mostra o caminho do arquivo.

### 2.4 Indicador de status (F-6)
Barra de status com dois indicadores independentes, sempre **ícone + texto** (nunca só cor):
- `API: Ativa (http://localhost:5000)` / `API: Inativa - porta em uso` / `API: Iniciando...`
- `Banco: OK` / `Banco: indisponível`
Atualização: na abertura, a cada 15 s por temporizador (via `IHealthService` em processo, sem HTTP) e ao clicar. Clique no indicador abre diálogo com detalhes (porta, caminho do `.fdb`, último erro, botão **Copiar**).

## 3. Design System

**Base:** controles DevExpress WinForms (skin única, definida em `DefaultLookAndFeel`, ex. "Office 2019 Colorful", com alternativa clara). Nenhum componente customizado desenhado do zero.

| Necessidade | Componente | Novo? |
|---|---|---|
| Listagem | `GridControl` + `GridView` (read-only) | não (padrão DX) |
| Filtros | `DateEdit` x2, `TextEdit`, `ComboBoxEdit`, `SimpleButton`, `LayoutControl` | não |
| Detalhe | `LayoutControl` + `GridControl` (itens) + `XtraTabControl` | não |
| Status/indicador | `BarManager` `StatusBar` + `BarStaticItem` (ícone + texto) | não |
| Mensagens | `XtraMessageBox`, `AlertControl` (avisos não bloqueantes) | não |
| Carregando | `WaitForm`/`SplashScreenManager` ou `ProgressPanel` | não |
| Estado vazio/erro em grid | `GridView.CustomDrawEmptyForeground` (texto central) | **NOVO (customização leve, sinalizada)**: só desenha texto sobre o grid vazio |
| Preview do relatório | `FastReport PreviewControl` | não (condicional ADR-008) |

**Tokens (via skin DX, sem cores hardcoded):**
- Status (ícone + texto; a cor é reforço): Quitada = verde + "check"; Pendente = âmbar + "relógio"; Cancelada = vermelho + "x". Ícones em SVG DX (`ImageCollection`), tamanho 16 px.
- Fonte: padrão da skin, mínimo 9 pt (não reduzir); números `N2` alinhados à direita; datas `dd/MM/yyyy HH:mm` (cultura pt-BR).
- Espaçamento: `LayoutControl` com padding padrão; alvos de clique >= 24 px de altura.
- Formatação centralizada em uma classe `Formatadores` (moeda, data local, status) reutilizada por grid, detalhe e relatório (consistência e testabilidade).

## 4. Estados de Tela

| Tela/Fluxo | Vazio | Carregando | Erro | Sucesso |
|---|---|---|---|---|
| **F-1 Lista** | Sem nenhuma venda: "Nenhuma venda registrada ainda. As vendas aparecem aqui quando o ERP Vendas as enviar." Botão Emitir relatório continua habilitado (relatório vazio com totais 0, PRD Seção 4) | `ProgressPanel` sobre a grid + botões desabilitados; UI não congela (consulta em `Task`); cancelável ao mudar filtro | Banco indisponível: painel "Não foi possível consultar o banco." + **Tentar novamente**; detalhe técnico só no log/diálogo do indicador; grid mantém dados anteriores esmaecidos se existirem | Grid preenchida, contador "N vendas" e foco na primeira linha |
| **F-2 Filtros [B]** | Sem correspondência: "Nenhuma venda para os filtros informados." + link **Limpar filtros** | Igual à lista | Validação: período inicial > final -> mensagem inline junto ao campo, sem consultar | Lista filtrada; filtros aplicados ficam visíveis (resumo na barra) |
| **F-3 Detalhe** | Venda sem itens (cancelada sem quitação): texto explicativo (2.2) | Abre imediato com cabeçalho da linha; itens carregam com `ProgressPanel` | Falha ao carregar itens: aviso na aba + **Tentar novamente**; janela não fecha | Itens e total conferem com o valor da venda |
| **F-4 Histórico [C]** | "Sem histórico registrado." (não deveria ocorrer) | Idem itens | Idem itens | Linhas append-only em ordem cronológica |
| **F-5 Relatório** | Filtro sem vendas: relatório gerado com "Nenhuma venda no período" e totais 0,00 | `WaitForm` "Gerando relatório..." (cancelável) | Falha (banco, arquivo, sem preview e sem visualizador PDF): `XtraMessageBox` com causa resumida e caminho do arquivo quando existir; log | Preview aberto ou PDF aberto; barra "Relatório gerado" |
| **F-6 Indicador** | n/a (sempre tem estado) — justificativa: é indicador, não conteúdo | "API: Iniciando..." | "API: Inativa - porta em uso"/"Banco: indisponível" + ação **Detalhes**; caso de falha na inicialização (F-7) mostra diálogo com causa e instruções (trocar porta em `App.config`, fechar outra instância) e opção **Sair** | "API: Ativa" / "Banco: OK" |
| **F-7 Inicialização** | n/a | Splash "Iniciando banco e API..." | Falha do banco: o app abre a `FrmConsulta` em estado de erro (F-1 Erro), nunca fecha silencioso; segunda instância detectada por mutex: aviso "Já existe uma instância em execução" e sai | Janela principal carregada |

## 5. Acessibilidade (WinForms, WCAG 2.1 AA como referência aplicável)

Critério não negociável; escopo: o que WinForms/DevExpress permite.

- **Teclado completo:** Tab/Shift+Tab em ordem lógica (filtros -> grid -> botões, `TabIndex` explícito); `Enter` aplica filtros e abre detalhe na linha; `Esc` fecha diálogos; `F5` atualiza; atalhos com `&`/`AccessKey` nos botões (`&Aplicar`, `&Limpar`, `&Emitir relatório`); nenhuma ação exclusiva de mouse.
- **Foco visível:** manter o indicador de foco padrão da skin; foco inicial: primeiro filtro (B) ou a grid (A); ao fechar o detalhe, foco volta à linha de origem.
- **Nomes acessíveis (MSAA/UIA):** `AccessibleName`/`AccessibleDescription` em todo controle, incluindo grid ("Lista de vendas") e indicadores de status; rótulos associados aos campos.
- **Cor nunca isolada:** status sempre com texto + ícone; indicadores API/Banco com texto. Contraste >= 4,5:1 texto normal (validar na skin escolhida; se a skin reprovar, usar a variante de alto contraste).
- **Escala/alto contraste:** suportar DPI 125/150% (`AutoScaleMode = Dpi`, `DevExpress.XtraEditors.WindowsFormsSettings.SetDPIAware()`); respeitar tema de alto contraste do Windows; não fixar tamanho de fonte menor que o padrão.
- **Mensagens:** erros em texto claro em português com ação seguinte; `XtraMessageBox` é lido por leitores de tela; estados de carregamento anunciados por texto ("Carregando vendas...") e não só por animação.
- **Leitor de tela:** ler a grid por linha (`GridView` expõe UIA); verificação manual com Narrador no Dia 5 (checklist de 8 itens: navegação por Tab, leitura da grid, abertura/fechamento de detalhe, emissão, indicador, erro, vazio, foco).
- **Idioma/formatos:** cultura pt-BR fixada para datas e moeda.
- Resultado da `accessibility-review` (autorrevisão sobre este documento): sem pendência crítica no desenho; verificação prática de contraste da skin e leitura no Narrador ficam como item de aceite da tela (não é verificável em documento).

## 6. Comportamento Responsivo

Aplicação desktop, não mobile. Definições:
- Tamanho mínimo da janela 1024x600; layout com `Anchor`/`Dock` e `LayoutControl` (colunas do grid ajustam por `BestFitColumns` no carregamento e ajuste manual preservado no redimensionamento).
- Em janela estreita, o painel de filtros quebra em duas linhas (recurso do `LayoutControl`); grid rola horizontalmente.
- Detalhe: modal redimensionável (mínimo 640x420), abas com rolagem.
- Resolução alvo: 1366x768 a 1920x1080, DPI 100-150%. Mobile/web: **não aplicável**.
- Preferência de layout salvo: **não persistir** em [A]/[B] (evita escopo); persistência de layout do grid [C].

## 7. Restrições Técnicas Aplicadas (autochecagem contra o SDD)

| Restrição do SDD | Efeito na UX | Decisão / trade-off |
|---|---|---|
| ADR-008: tela consome `ConsultaService` em processo | Tela funciona com a API inativa; indicador separa API e banco | Aceito; mostra "API: Inativa" sem bloquear consulta |
| ADR-004: API vive só enquanto o app está aberto | Operador precisa saber; fechar o app derrubaria a integração | Indicador sempre visível; confirmação ao fechar: "Fechar encerra a API; o ERP Vendas não conseguirá enviar vendas." (decisão de detalhe, sem impacto de custo/prazo) |
| Firebird embarcado, único processo | Segunda instância do app não pode abrir o `.fdb` | Mutex de instância única + mensagem clara (F-7) |
| ADR-002/RT-01: possível fallback SQL Server | UI independente de banco (usa só `ConsultaService`) | Nenhuma mudança de tela no fallback |
| RT-09: performance da grid | Limite 5.000 linhas com aviso; sem paginação | Evita custo de paginação; aviso mantém honestidade dos totais: o **relatório usa o filtro completo, sem o limite de 5.000** (aviso adicional na tela se houve truncamento) |
| ADR-007: colunas anuláveis | "—" e marca "Cancelada*" | Sem coluna nova; legenda no rodapé |
| ADR-008: relatório sem preview possível (FastReport gratuito) | Fluxo alternativo PDF externo | Mesma ação do usuário nos dois casos; diferença só no destino |
| S-03 histórico = Tier C; S-06/S-07 = Tier B | Abas/filtros/totais ocultos se cortados | Fluxos marcados por tier; não deixa controle desabilitado sem função |
| Somente leitura na tela | Sem quitar/cancelar manualmente | Coerente com RF (mutação só via API); reduz risco de inconsistência com Vendas |
| D-10: cliente por ID | Filtro por `TextEdit` exato (com "contém" opcional), sem lookup de nome | Se Vendas enviar `clienteNome`, coluna adicional (campo aditivo) sem redesenho |

Nenhum trade-off acima altera custo/prazo do PRD; nenhum sinalizado ao Gestor como bloqueio.
