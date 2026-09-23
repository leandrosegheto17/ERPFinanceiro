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
