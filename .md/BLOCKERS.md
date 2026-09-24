# BLOCKERS.md — ERP Financeiro

## Bloqueio 001 — 22/09/2026
- Reportado por: executor (chapéu BE, T-11)
- Escalado para: usuário (orquestrador) — bloqueio de ambiente/host, sem dono de
  artefato claro (não é um problema de SDD.md/UX-SPEC.md/TASK.md)
- Artefato/trecho afetado: ambiente de execução deste worktree/sessão (não um
  arquivo `.md`); primeiro observado em `.md/TASK.md` linha T-11
- Descrição: `dotnet test`/`dotnet vstest` (tanto de dentro do worktree OneDrive
  quanto buildado direto para uma pasta local fora do OneDrive — os dois
  contornos já documentados por T-06/T-07/T-12) falham ao carregar
  `ERPFinanceiro.Application.dll` com `System.IO.FileLoadException`: "Uma
  política de Controle de Aplicativo bloqueou este arquivo" (HRESULT
  0x800711C7). Diagnóstico via `reg query
  HKLM\SYSTEM\CurrentControlSet\Control\CI\Policy`:
  `VerifiedAndReputablePolicyState=1` e `SAC_PreviousState=2` — Smart App
  Control / Application Control do Windows ativo nesta máquina, bloqueando
  carregamento de DLLs .NET Framework recém-compiladas e não assinadas.
  **Não é o mesmo problema de OneDrive já documentado** (sem stream
  `Zone.Identifier`/MOTW nos arquivos; build feito direto para pasta local
  reproduz o mesmo erro). **Confirmado que não é regressão de código desta
  tarefa**: a suíte de T-12 (`DbInitializerTests`), já `Concluída` com 9/9
  verde documentado no `TASK.md`, falha agora **de forma idêntica** ao ser
  reexecutada nesta sessão/host — ou seja, é um bloqueio novo em nível de
  ambiente, que também invalida a reprodutibilidade da evidência antiga de
  T-06/T-12 nesta sessão específica.
- Impacto se não resolvido: nenhuma tarefa BE pendente que exija "execução real
  contra `.fdb`" (T-14, T-15, T-16, T-18, T-19, T-20, T-22, T-23, T-24, T-36,
  T-38…) consegue produzir evidência de execução verificável nesta
  sessão/host — só inspeção estrutural/build, o que é mais fraco que o padrão
  já estabelecido pelas tarefas anteriores do Lote 1-3. T-11 permanece "Em
  andamento" (código completo e revisável, mas sem execução real confirmada).
- Sugestão (opcional): desabilitar/ajustar a política "Smart App Control"/
  "Reputation-based protection" (Configurações do Windows > Privacidade e
  segurança > Segurança do Windows > Controle de aplicativos inteligente, ou
  política de grupo equivalente) ou adicionar exclusão para a pasta de
  build/output do repositório — requer acesso interativo/admin ao Windows fora
  deste sandbox (powershell.exe/cmd.exe são bloqueados pela ferramenta de
  shell deste agente). Alternativa: rodar a suíte de testes de integração real
  numa máquina/agente sem essa política ativa, ou assinar digitalmente os
  assemblies de saída (fora do escopo atual, TASK.md não pede assinatura).
- **Opções avaliadas com o usuário (22/09/2026) e descartadas por ora:**
  - *Assinatura digital dos assemblies de build/teste*: descartada. O Smart App
    Control não confia em certificado autoassinado (baseia-se em reputação na
    nuvem, não só na validade da assinatura); um certificado de CA confiável é
    pago (~US$70–700+/ano), conflitando com G-2.6 do `GUARDRAILS.md` (custo
    zero). Além disso os assemblies de teste são recompilados a cada
    `dotnet build`/`dotnet test` (múltiplas vezes por dia durante o
    desenvolvimento) — assinar a cada build do ciclo de dev não é prático;
    assinatura de release só faria sentido em T-53 (empacotamento final), não
    para destravar execução de testes agora.
  - *Desativar o Smart App Control agora*: descartada por decisão do usuário
    (é uma mudança de sistema inteiro e **irreversível sem reinstalar o
    Windows** — não desligar nesse momento).
- **Encaminhamento decidido:** nenhuma ação de ambiente será tomada agora. As
  tarefas que dependem de execução real contra `.fdb` (T-11 em diante, ver
  lista de impacto acima) ficam pausadas nesta sessão/host; os testes de
  integração real serão retomados "de outra forma" (máquina/ambiente sem SAC,
  ou outra solução a definir) em uma chamada futura de `/executar`. Até lá,
  novas tarefas BE que dependam desse tipo de evidência não devem ser
  disparadas nesta worktree além do que já está seguro por inspeção
  estrutural.
- **Atualização (22/09/2026, executor chapéu BE, T-10):** confirmado que o
  bloqueio também afeta **Domain puro**, sem qualquer dependência de
  Firebird/`.fdb`. `dotnet test`/`dotnet vstest` sobre
  `ERPFinanceiro.Tests.dll` (suíte `VendaTests`, só `ERPFinanceiro.Domain` +
  xUnit) falha antes mesmo de descobrir os testes: "Falha ao carregar as
  extensões do arquivo
  '...\ERPFinanceiro.Tests\bin\Debug\net48\xunit.runner.visualstudio.testadapter.dll'"
  e, em seguida, "Não há nenhum teste disponível". O log de diagnóstico
  (`/diag`) não mostra `FileLoadException` explícito desta vez (o host de
  teste sai com `exitcode: 0` sem stderr), mas o sintoma é o mesmo padrão já
  descrito acima (adapter/extensão .NET Framework recém-compilada falhando ao
  carregar) — mesma causa-raiz (Smart App Control), agora comprovada
  independente de qualquer dependência em Firebird/Application/Infrastructure.
  Ou seja: **nenhum teste `dotnet test` roda nesta sessão/host, nem mesmo os
  puramente unitários de Domain**. T-10 (`Venda.CriarCancelada`) fica "Em
  andamento": implementação e teste (`VendaTests.CriarCancelada_...`) escritos
  e revisáveis, mas sem execução real confirmada — mesmo encaminhamento já
  decidido acima (nenhuma ação de ambiente agora; retomar execução real "de
  outra forma" em chamada futura de `/executar`).
- **Atualização (22/09/2026, executor chapéu BE, retomada T-11):** `dotnet build`
  de `ERPFinanceiro.Infrastructure` e `ERPFinanceiro.Tests` confirmado limpo (0
  erro/aviso). Copiado `src/ERPFinanceiro.Tests/bin/Debug/net48` (já com as DLLs
  nativas do Firebird de T-12) para `C:\temp_erp_tests\net48`, fora do OneDrive,
  e rodado `dotnet vstest ERPFinanceiro.Tests.dll
  /TestCaseFilter:"FullyQualifiedName~FinanceiroDbContextTests"` duas vezes
  seguidas — **ambas as tentativas falharam de forma idêntica**, mesmo erro já
  documentado acima (`FileLoadException` ao carregar
  `ERPFinanceiro.Application.dll`, "Uma política de Controle de Aplicativo
  bloqueou este arquivo", HRESULT 0x800711C7). Desta vez o bloqueio **não se
  mostrou intermitente** dentro da mesma sessão (2/2 tentativas falharam).
  Pasta temporária `C:\temp_erp_tests` removida ao final. T-11 permanece "Em
  andamento" (implementação e testes completos e revisáveis em
  `src/ERPFinanceiro.Infrastructure/Persistencia/FinanceiroDbContext.cs` e
  `src/ERPFinanceiro.Tests/Infrastructure/Persistencia/FinanceiroDbContextTests.cs`,
  sem execução real confirmada nesta sessão/host). Nenhuma ação nova de
  ambiente tomada, mesmo encaminhamento já decidido acima.
- **Atualização (22/09/2026, executor chapéu BE, T-25):** o contorno já
  documentado (build de `ERPFinanceiro.Tests` para pasta fora do OneDrive, ex.
  `C:\temp_erp_tests_t25`, e `dotnet vstest` direto ali) funcionou de primeira
  para a suíte nova `StartupTests` (JSON puro, sem Firebird/Application real em
  runtime além de `ERPFinanceiro.Api`/`ERPFinanceiro.Application` como
  dependências de compilação) — 4/4 verde na 1ª tentativa, sem repetição do
  `FileLoadException`/política de Controle de Aplicativo. Ou seja, o bloqueio
  **é intermitente conforme já suspeitado**, não 100% reprodutível a cada
  execução; T-25 não ficou bloqueada por ele. Mantido o mesmo encaminhamento
  para as tarefas que seguem batendo no bloqueio (T-11 em diante): nenhuma ação
  de ambiente nova, pasta temporária removida ao final.
- **Atualização (22/09/2026, executor chapéu BE, T-29):** `dotnet vstest`
  direto de dentro do worktree (OneDrive) voltou a falhar com o mesmo sintoma
  ("Falha ao carregar as extensões do arquivo
  `xunit.runner.visualstudio.testadapter.dll`", "Não há nenhum teste
  disponível") na 1ª e na 2ª tentativa — mesma suíte `CorrelationIdHandlerTests`
  (Api pura, sem Firebird). Aplicado o contorno já validado em T-25: `dotnet
  build` do `ERPFinanceiro.Tests` seguido de cópia do `bin/Debug/net48` inteiro
  para uma pasta fora do OneDrive (`C:\temp_erp_tests_t29`) e `dotnet vstest`
  rodado direto ali — funcionou de primeira, 3/3 verde. Pasta temporária
  removida ao final. T-29 não ficou bloqueada; confirma outra vez que o
  bloqueio é intermitente e contornável copiando o output de teste para fora
  do OneDrive antes de rodar `dotnet vstest`.
- **Decisão revista pelo usuário (22/09/2026):** ao contrário da decisão
  anterior registrada acima ("Desativar o Smart App Control agora: descartada
  por decisão do usuário"), o usuário decidiu **desativar o Smart App Control**
  para destravar a execução real de testes/`.fdb` nesta máquina. Confirmado
  que a mudança é de sistema inteiro e **irreversível sem reinstalar o
  Windows** — ciente do trade-off, decisão mantida. Ação é manual, feita pelo
  usuário fora deste sandbox (Configurações > Privacidade e segurança >
  Segurança do Windows > Controle de aplicativos e navegador > Controle de
  aplicativos inteligente > Desativar), possivelmente seguida de reinício.
- **Atualização (22/09/2026, executor chapéu BE, retomada T-11 pós-SAC
  desativado):** usuário confirmou ter desativado o Smart App Control (sem
  reiniciar a máquina ainda). `dotnet build` de `ERPFinanceiro.Infrastructure`/
  `ERPFinanceiro.Tests` limpo (0 erro/aviso). `dotnet vstest` direto de dentro
  do worktree (OneDrive) **ainda falhou**, mas com um erro **diferente** do
  documentado acima: `FileLoadException` ao carregar
  `xunit.runner.visualstudio.testadapter.dll`, HRESULT **0x80131515**
  ("Operação sem suporte") — não mais 0x800711C7 ("política de Controle de
  Aplicativo bloqueou este arquivo"). 0x80131515 é o padrão típico de
  restrição de zona/Mark-of-the-Web em pasta sincronizada do OneDrive (mesma
  classe de problema já mencionada como "não é isso" em atualizações
  anteriores, mas que aqui se confirmou presente com o SAC fora do caminho).
  Aplicado o contorno já validado por T-12/T-25/T-29 (copiar
  `src/ERPFinanceiro.Tests/bin/Debug/net48` para `C:\temp_erp_tests`, fora do
  OneDrive, e rodar `dotnet vstest` de lá, filtro
  `FullyQualifiedName~FinanceiroDbContextTests`): **funcionou de primeira,
  2/2 testes verdes** (round-trip decimal + conflito de concorrência). Pasta
  temporária removida ao final.
  **Conclusão:** desativar o Smart App Control resolveu a classe de erro
  0x800711C7 (Controle de Aplicativo), mas **não elimina a necessidade do
  contorno de copiar para fora do OneDrive** — a pasta sincronizada do
  OneDrive continua impondo uma restrição de carregamento de assembly
  separada (0x80131515) independente do SAC. Ou seja, a causa raiz era mista
  (duas políticas distintas bloqueando em momentos diferentes), e o contorno
  "copiar `bin/Debug/net48` para fora do OneDrive antes de `dotnet vstest`"
  permanece obrigatório para toda tarefa BE com teste de integração real,
  mesmo agora com o SAC desativado. T-11 marcada `Concluída` no `TASK.md`.
- Status: Resolvido (parcialmente) — SAC desativado eliminou o erro
  0x800711C7; T-11 destravada e concluída usando o contorno de cópia para
  fora do OneDrive (0x80131515), que segue necessário para as próximas
  tarefas BE com execução real contra `.fdb`/testes de integração nesta
  máquina/worktree.

## Bloqueio 002 — 23/09/2026
- Reportado por: orquestrador (`/executar --continuar`), ao recalcular a fila do
  Lote 8 após T-35/T-36/T-37 fecharem `Concluída`
- Escalado para: usuário (orquestrador) — dependência externa ao alcance de
  qualquer agente deste sandbox, não é um problema de código/SDD.md/UX-SPEC.md
- Artefato/trecho afetado: `.md/TASK.md`, Lote 8, linhas T-38 e T-39
- Descrição: **T-38** ("Integração real com o Vendas (Delphi): quitação e
  consulta de status disparadas pelo Vendas real") depende de T-03 ("aceite do
  Vendas") e de acesso real ao sistema Vendas (Delphi) — nenhum dos dois está
  disponível neste ambiente. `T-03` já registrou (22/09/2026) que o envio do
  `docs/contrato-v1.1.md` ao lado Vendas e o aceite ponto a ponto **não foram
  realizados** ("sem acesso ao Vendas neste ambiente"); a própria linha de T-38
  já antecipa essa possibilidade ("Se contrato v1.1 não foi aceito: usar v1.0 e
  registrar bloqueio em BLOCKERS.md"). Além do aceite pendente, T-38 pede uma
  ação disparada pelo sistema Vendas real (Delphi) contra esta API — não é
  algo que um Executor consiga simular de forma fiel sem o outro lado real (T-39
  depende de T-38 e tem a mesma natureza: "cada cenário executado no Vendas
  real").
- Impacto se não resolvido: Lote 8 não fecha (T-38/T-39 seguem `A fazer`);
  `/executar --continuar` não consegue validar o Lote 8 nem avançar para lotes
  seguintes que dependam dele (T-40 em diante, ver Seção 4 do TASK.md). Todo o
  restante do Lote 8 (T-35, T-36, T-37) e do Lote 7 (T-30…T-34) está
  `Concluída`/`Validado`, sem bloqueio.
- Sugestão: usuário decide entre (a) obter o aceite do Vendas e agendar a
  integração real fora deste sandbox (ambiente com acesso ao sistema Delphi),
  (b) destacar T-38/T-39 como pendência de outro time/ambiente e seguir
  `/executar`/`/validar` nos lotes seguintes que não dependam delas (checar
  Seção 4 do TASK.md antes), ou (c) redefinir o escopo de T-38/T-39 com o
  Coordenador caso a integração real não seja viável dentro do prazo do
  projeto.
- **Decisão do Coordenador (23/09/2026, chapéu Tech Lead/Software Architect,
  opção (c) acima, registrada em `.md/adr/010-integracao-vendas-simulada-sem-acesso-delphi.md`):**
  - Conferida a Seção 4 do `TASK.md` antes de decidir: **nenhuma tarefa dos
    Lotes 9, 10, 11 ou 12 depende de T-38/T-39/T-40** (dependem de T-04,
    T-12, T-22…T-24, T-26, T-27, T-41, T-42 — todas já `Concluída`/`Validado`
    ou liberadas). A única dependência real encadeada é `T-53` (Lote 13,
    empacotamento) -> `T-40` -> `T-39` -> `T-38`. Ou seja, o bloqueio não
    travava a tela/relatório/acessibilidade, só o fechamento final de
    empacotamento/entrega.
  - O marco "API utilizável pelo Vendas" (fim do Dia 3, Lote 7) **já foi
    cumprido** e não é afetado por esta decisão (Lote 7 `Validado`, sem
    dependência de T-38/T-39).
  - O marco "O-11" do Dia 4, por outro lado, **muda de significado**: deixa
    de ser "integração real confirmada com o Vendas" e passa a ser
    "confiança técnica validada por suíte de integração **simulada**
    (harness HTTP real reaproveitando o padrão de T-34/T-37); integração
    real com o Delphi permanece pendência externa explícita". Essa mudança
    de escopo do marco **é sinalizada ao usuário para aprovação explícita**
    antes de o Executor ser disparado sobre os novos T-38/T-39 — não é uma
    decisão que o Coordenador aprova sozinho (guardrail: trade-off de alto
    impacto em escopo/entrega vai para o usuário).
  - `TASK.md` atualizado (Lote 8: T-38, T-39, T-40 redefinidas + nota de
    cabeçalho do lote; Seção 2: nota de risco; Seção 4.3: matriz de
    premissas; Seção 6: item 10, novo). Redecomposição cirúrgica — só as
    3 linhas de tarefa + notas de seção tocadas, sem redesenhar o resto do
    plano.
  - **Pendência residual, não fechada por esta decisão:** integração real
    com o sistema Vendas (Delphi) e aceite formal do `docs/contrato-v1.1.md`
    continuam **em aberto**, fora do alcance deste sandbox. Deve ser
    retomada em ambiente com acesso ao sistema Delphi real, e documentada
    como limitação conhecida em T-54 (README) e como risco/dívida técnica
    aceita no `SDD.md` antes da entrega final (Lote 13). Se o usuário
    entender que essa pendência é um gate obrigatório para "concluir o
    projeto" (não apenas para os agentes deste sandbox), isso é decisão de
    negócio do usuário/Gestor, não do Coordenador.
- **Atualização (23/09/2026, executor chapéu BE, T-40):** T-38/T-39 executados
  e `Concluída` — ambos reportaram **zero divergências** de comportamento nos
  cenários simulados (nada a corrigir por T-40). Confirmado que a "pendência
  residual" descrita acima já deixava claro que a integração real permanece em
  aberto; para dar a ela um registro dedicado e acionável (não só narrativo),
  foi criado `docs/integracao-simulada/PENDENCIA-INTEGRACAO-REAL.md`, listando
  objetivamente o que falta (aceite formal de `docs/contrato-v1.1.md` pelo
  time Vendas — T-03; acesso a um ambiente com o Vendas/Delphi real ou
  homologação conjunta; responsáveis/próximos passos conhecidos e
  desconhecidos). Este documento é o que `T-54` (README final, ainda não
  criado nesta sessão) deve linkar/incorporar. Bloqueio **não reaberto** — a
  pendência residual continua sendo tratada como item explícito em aberto, não
  como bloqueio ativo de execução (nenhum lote/tarefa Tier A depende dela
  além do já concluído `T-40`).
- Status: **Resolvido (redesenho de escopo) — pendência residual de
  integração real com o Vendas permanece aberta, fora do alcance deste
  sandbox; suíte simulada T-38/T-39/T-40 concluída sem divergências; detalhe
  acionável da pendência registrado em
  `docs/integracao-simulada/PENDENCIA-INTEGRACAO-REAL.md`.**

## Bloqueio 003 — 23/09/2026
- Reportado por: orquestrador (`/executar --continuar`), ao recalcular a fila
  após T-41 (Lote 9) fechar `Concluída`
- Escalado para: usuário (orquestrador) — limitação de ambiente sem dono de
  artefato claro (não é um problema de SDD.md/UX-SPEC.md/TASK.md em si)
- Artefato/trecho afetado: `.md/TASK.md`, Lotes 9-12 (T-42 a T-52) — praticamente
  todo o restante do Frontend/Desktop do projeto
- Descrição: **DevExpress WinForms trial não está instalado/instalável neste
  sandbox** (já confirmado em T-02: "sandbox sem GUI/instalador interativo"; em
  T-04: "feed licenciado nuget.devexpress.com, não instalável neste ambiente";
  reconfirmado agora em T-41, que só conseguiu implementar a fatia sem
  DevExpress do seu escopo — `Formatadores`, lógica pura — deixando a parte de
  skin/ícones SVG documentada como pendência, sem simular a API). T-42
  (`FrmConsulta`, `GridControl` DevExpress somente leitura) é a próxima tarefa
  elegível do Lote 9 e **não tem fatia sem-DevExpress equivalente à de T-41** —
  é fundamentalmente uma tela WinForms com `GridControl` real. O mesmo vale
  para praticamente todo o restante: T-43/T-44 (Lote 9), T-45/T-46/T-47
  (Lote 10), T-49/T-50 (Lote 11, FastReport/preview), T-51/T-52 (Lote 12,
  acessibilidade de tela real) — todas dependem de renderizar/testar UI
  WinForms de verdade, sem GUI interativa neste ambiente. T-48 (Lote 11,
  `RelatorioDataSource`) é a exceção parcial: é lógica de Application/Reports
  sem UI direta, pode não sofrer do mesmo bloqueio (a confirmar quando chegar a
  vez dela).
- Impacto se não resolvido: nenhuma tela real pode ser implementada nem
  verificada por execução real (Diretriz 16) neste sandbox — só inspeção
  estrutural/código sem renderização, o que quebraria o padrão de evidência
  real já estabelecido em todo o projeto até aqui (Lotes 1-8, todos com
  execução real confirmada). Forçar a implementação sem poder rodar/ver a UI
  arriscaria produzir código não verificado apresentado como "concluído".
- Sugestão: usuário decide entre (a) disponibilizar um ambiente com GUI
  interativa e o instalador do DevExpress WinForms trial para uma sessão
  futura de `/executar` continuar o Frontend, (b) autorizar uma abordagem
  alternativa (ex.: Coordenador reavalia se algum subconjunto de T-42+ pode
  ser implementado/testado sem DevExpress real — improvável dado que a
  diretriz 12 da Seção 1 do TASK.md exige "só controles DevExpress padrão"),
  ou (c) pausar a fila de execução no Frontend e priorizar outras frentes
  (ex. T-48/T-53+ que não dependam de UI renderizada) até o ambiente estar
  disponível.
- **Atualização (23/09/2026, orquestrador):** usuário escolheu (c) e, depois,
  ao consultar a página oficial de trial da DevExpress, foi identificado que os
  pacotes DevExpress WinForms trial (30 dias) estão disponíveis no **nuget.org
  público** (ex.: `DevExpress.Win.Grid` 25.1.x/26.1.x, verificado pela
  DevExpress). Teste no sandbox: projeto `net48` descartável com
  `PackageReference Include="DevExpress.Win.Grid"` **restaurou e compilou**
  (`GridControl` resolvido), só com warnings `DX1000/DX1001` ("For evaluation
  purposes only", esperados no trial). Isso **corrige a premissa de T-02/T-04**
  ("feed licenciado nuget.devexpress.com, não instalável aqui"). A limitação
  real que permanece é apenas **verificação visual**: este sandbox não tem
  display, então layout/cores/ícones/acessibilidade (incl. T-52, Narrador)
  exigem conferência manual do usuário numa máquina com GUI. Compilação e
  testes não-visuais (configuração de colunas, binding, formatação,
  eventualmente instanciação headless de controles) são viáveis aqui.
  Nota de cronograma: trial de 30 dias; início do relógio no modo NuGet não
  confirmado.
- Status: Resolvido (parcialmente) — compilação/testes não-visuais liberados;
  verificação visual permanece etapa manual do usuário. Fila de Frontend
  (T-42 em diante) retomada em 23/09/2026.

## Nota operacional 004 — 23/09/2026 (causa provável da intermitência da suíte; ver RL10-06)
- Reportado por: orquestrador, ao integrar Lotes 7-10 na `main`.
- Sintoma: falhas em cadeia e não determinísticas na suíte (dezenas de testes, inclusive
  `DbInitializerTests`, sem relação com o código alterado), com
  `FbException: Your user name and password are not defined`, ou `WaitForSingleObject failed`.
  Reproduzido no mesmo binário que antes passava 224/224; passa a falhar depois de algumas
  rodadas e não se corrige apagando `.fdb`/copiando a pasta de teste de novo.
- Causa provável (evidência): esta máquina tem o **serviço Windows `FirebirdServerDefaultInstance`
  (Automático, sempre ativo)**. O Firebird embarcado dos testes divide com ele os arquivos de lock
  em `C:\ProgramData\firebird` (`fb_lock_*`, `fb12_monitor_*`, `fb_user_mapping`); o serviço mantém
  esses arquivos em uso, e o embarcado passa a falhar ao criar/abrir bancos.
- Contorno validado (não altera o serviço, só o processo de teste): isolar o motor embarcado com
  as variáveis oficiais do Firebird, apontando `FIREBIRD` para a pasta de teste (que já contém
  `firebird.conf`, `security3.fdb`, `plugins`, `intl`) e `FIREBIRD_LOCK` para uma pasta de lock
  exclusiva, criada antes:
  `FIREBIRD='C:\temp_erp_x' FIREBIRD_LOCK='C:\temp_fb_lock_x' dotnet vstest ERPFinanceiro.Tests.dll`
  Resultado: com isolamento, 224/224 em duas rodadas seguidas, após 3 rodadas sem isolamento que
  falharam (72 e 96 falhas; e `DbInitializerTests` 0/3). Recomenda-se usar sempre esta forma nas
  execuções da suíte nesta máquina (e considerar fixá-la num script de teste do repositório).
- Status: Contornado. Pendente a decisão de tornar o isolamento parte do repositório (script ou
  `runsettings`), para não depender de quem roda os testes.

## Bloqueio 005 — 23/09/2026
- Reportado por: orquestrador (`/executar lote 12`), ao recalcular a fila após T-51 fechar `Concluída`
- Escalado para: usuário (orquestrador) — verificação manual que exige máquina com GUI e leitor de
  tela; sem dono de artefato (não é problema de SDD.md/UX-SPEC.md/TASK.md)
- Artefato/trecho afetado: `.md/TASK.md`, Lote 12, T-52 (e `docs/acessibilidade.md`, ainda não criado)
- Descrição: **T-52** ("Verificação manual com Narrador: checklist de 8 itens do UX-SPEC 5,
  resultado registrado em `docs/acessibilidade.md`") exige executar o app real e ouvir o Narrador do
  Windows. O sandbox não tem display nem leitor de tela, então nenhum agente consegue marcar os 8
  itens ok/nok com evidência real; preencher o checklist sem executar seria evidência fabricada.
  Pendências manuais acumuladas de T-51 que entram no mesmo checklist: DPI 125/150% real, Tab/
  Shift+Tab ao vivo e foco visível, contraste renderizado/alto contraste, leitura no Narrador,
  `SetDPIAware` no `Main` (só por inspeção).
- Impacto se não resolvido: Lote 12 não fecha (T-52 `A fazer`); T-53 (Lote 13, empacotamento)
  depende de T-52 (Seção 4 do TASK.md). T-51 está `Concluída`, verificada por código (234/234).
- Sugestão: (a) o usuário executa o checklist de 8 itens do UX-SPEC 5 numa máquina com GUI e
  registra o resultado (o executor pode gerar antes o esqueleto de `docs/acessibilidade.md` para
  preenchimento manual); (b) aceitar T-52 como pendência manual explícita e seguir para
  `/validar` do Lote 12; (c) redefinir T-52 com o Coordenador.
- **Decisão do usuário (23/09/2026):** ignorar o Lote 12 e considerá-lo concluído. T-52 marcada
  `Concluída` como dispensada; a verificação com Narrador não foi feita e segue como risco aceito.
- Status: Resolvido (risco aceito pelo usuário) — verificação manual de acessibilidade não realizada.

## Bloqueio 006 — 24/09/2026
- Reportado por: executor (T-53, `/executar_tarefa`)
- O quê: o `.fdb` de demonstração não pôde ser gerado: um `testhost.net48.exe` órfão (PID 31468) mantinha um Firebird embarcado mapeado e o isql embarcado falhava/travava. Pendência adicional (RL9-04): DevExpress em evaluation, redistribuição proibida.
- Escala para: usuário.
- **Decisão do usuário (24/09/2026):** a entrega é só o código-fonte (processo seletivo). T-53 redefinida para manter apenas o script de empacotamento; `.fdb` e pasta de entrega não são mais exigidos e a licença DevExpress deixa de ser impeditiva. T-56 passa a testar clone + compilação + README.
- Status: Resolvido (redesenho de escopo)
