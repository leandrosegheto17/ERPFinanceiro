# Licenças — DevExpress WinForms e FastReport (T-02)

> Origem: tarefa T-02 (Lote 1), spike parte de T-02 (ver `.md/TASK.md` Seção 2). Alimenta ADR-006/ADR-008 (fluxo do relatório: preview ou fallback PDF) e G-2/G-6 do `GUARDRAILS.md` (custo zero).

## Situação atual (23/09/2026)

- **DevExpress vem do nuget.org público.** Em 23/09/2026 verificou-se que o pacote `DevExpress.Win.Grid` (versões 25.1.x e 26.1.x; publicado e verificado pela DevExpress, owner `devexpress`) está no nuget.org público e **restaura e compila** neste sandbox (projeto `net48`, `PackageReference Include="DevExpress.Win.Grid" Version="25.1.9"`, ver `src/ERPFinanceiro.Desktop/ERPFinanceiro.Desktop.csproj`, T-42). A página oficial de trial (https://www.devexpress.com/products/try/) lista a opção "NuGet.org (for all .NET products/libraries)". Não é necessário instalador interativo nem feed licenciado.
- **Modo evaluation.** O build emite os warnings `DX1000` ("For evaluation purposes only. Redistribution prohibited. Please register an existing license (devexpress.com/DX1000) or purchase a new license (devexpress.com/BUY)...") e `DX1001` (baixar a chave pessoal `DevExpress_License.txt` e colocá-la em `%AppData%\DevExpress`, ou rodar o Unified Component Installer para ativar).
- **Pendência a confirmar:** quando começa o relógio do trial de 30 dias no modo NuGet (primeiro restore? primeiro build? registro de conta?). Não confirmado; não assumir.
- **Pendência de confirmação VISUAL (RL1-03):** ainda não foi possível ver, numa máquina com GUI, o splash/marca d'água de trial reais em runtime e nos relatórios exportados. Confirmar antes de T-49/T-50 e ajustar esta página se algo divergir. (As descrições de splash/watermark abaixo vêm da documentação oficial.)
- **Proibição de redistribuição.** A licença de avaliação proíbe redistribuir o build trial; o splash/marca d'água de trial persistem em runtime e nos relatórios exportados. O guardrail G-2 (custo zero) entra em conflito com isso se a entrega final usar DevExpress em evaluation.
- **Caminho possível para licença válida:** o usuário já usa DevExpress no Delphi (VCL), que é linha de produto separada. Se a assinatura for **Universal**, ela cobre WinForms sem custo adicional. Isso **não foi confirmado**; conferir na conta em devexpress.com.
- **Checklist objetivo para T-53 (empacotamento Release/entrega):** confirmar licença DevExpress válida/registrada (ou decisão explícita do usuário aceitando entrega em modo evaluation) antes de empacotar. Reflexo também no README (T-54).
- **Nota para o Gestor:** conflito entre G-2 (custo zero) e a proibição de redistribuir o trial — decisão de negócio do usuário/Gestor, não do Executor.
- **FastReport:** a seção abaixo continua baseada em pesquisa documental (22/09/2026); a instalação real do FastReport .NET Trial segue pendente de confirmação visual. FastReport Open Source também está no NuGet público.

## DevExpress WinForms

| Campo | Resposta |
|---|---|
| Versão pesquisada | `DevExpress.Win.Grid` 25.1.9 via nuget.org público (em uso, T-42); 25.1.x e 26.1.x disponíveis |
| Tipo de licença | Trial/Evaluation (30 dias), sem custo, mas **"Redistribution prohibited"** (aviso DX1000): o build trial não pode ser entregue como produto final; ver "Situação atual" e checklist de T-53 |
| Duração do trial | **30 dias corridos** (termos "THIRTY (30) DAY EVALUATION (TRIAL) USE LICENSE", DevExpress EULA/Support Center); **início do relógio no modo NuGet não confirmado** (pendência) |
| Tem marca d'água / aviso? | **Sim.** Fontes oficiais da DevExpress (Support Center, docs de "Remove the Trial Version Message") confirmam que a versão trial exibe: (1) diálogo/splash "This application was created using the trial version of..." ao rodar o app; (2) watermark em relatórios/documentos exportados (XtraReports) durante o período de avaliação; (3) avisos no compilador/designer da IDE. Some watermarks (impressão/exportação de relatórios) exigem licença paga para remoção mesmo após registro do trial |
| Tem preview? | Não aplicável a WinForms controls em si (grid/controles rodam nativamente); ver FastReport abaixo para preview de relatório |

Fontes: [DevExpress — Download Free Trial](https://www.devexpress.com/products/try/), [DevExpress Docs — Remove the Trial Version Message](https://docs.devexpress.com/GeneralInformation/403732/trial-register/remove-the-this-is-a-trial-version-splash-window), [DevExpress Support — trial notice on report printing](https://supportcenter.devexpress.com/ticket/details/t1243185/how-to-remove-trial-notice-from-printing-on-reports), [DevExpress Support — trial de 30 dias / EULA](https://supportcenter.devexpress.com/ticket/details/t296501/questions-about-the-use-of-the-trial-version).

## FastReport (.NET Trial vs. Open Source)

| Campo | Resposta |
|---|---|
| Versão pesquisada | FastReport.OpenSource mais recente publicada no NuGet à data da pesquisa (linha 2026.x); FastReport .NET Trial via instalador oficial fast-report.com (não baixado neste ambiente) |
| Tipo de licença | Duas opções, ambas de custo zero e compatíveis com G-2: (a) **FastReport Open Source** — licença MIT/open source, gratuita permanentemente, sem expirar; (b) **FastReport .NET Trial** — avaliação comercial por tempo limitado |
| Duração do trial (FastReport .NET) | Não encontrada uma duração fixa documentada de forma explícita na página de comparação oficial consultada; prática usual do fabricante é trial por tempo limitado com watermark nos relatórios gerados. **A confirmar** no momento da instalação real (ação pendente acima) |
| Tem marca d'água? | FastReport Open Source: não aplicável (não é trial, é MIT). FastReport .NET Trial: espera-se marca d'água nos relatórios gerados durante avaliação (padrão do fabricante para produtos trial), a confirmar na instalação real |
| Tem preview WinForms? | **Diferença central confirmada pela documentação oficial de comparação de features (FastReport Open Source Docs — COMPARISON):** apenas **FastReport .NET** (a linha comercial/trial) oferece preview **"In Application"** (componente visual de preview embutido no WinForms). **FastReport Open Source não tem componente visual de preview para WinForms** — para exibir o relatório em uma tela WinForms seria necessário renderizar como imagem; a via nativa do Open Source é exportar para arquivo (web browser ou viewer externo). Exportação PDF: no FastReport Open Source, o export para PDF é um **plugin separado**, não incluído no pacote base (`FastReport.dll`/`FastReport.OpenSource.nupkg`); no FastReport .NET (comercial/trial), PDF export é nativo/incluído |

Fontes: [FastReport Open Source — Feature Comparison Table](https://fastreports.github.io/FastReport.Documentation/COMPARISON.html), [FastReport Open Source Blog — Comparison](https://opensource.fast-report.com/p/the-feature-comparison-table-for.html), [NuGet — FastReport.OpenSource](https://www.nuget.org/packages/FastReport.OpenSource), [FastReport — Installing on .NET 8.0](https://www.fast-report.com/blogs/simple-report-net).

## Decisão de caminho — insumo para ADR-006/ADR-008 (preview vs. PDF)

Com base na pesquisa acima:

1. **FastReport Open Source** (MIT, sem expiração, custo zero permanente) **não tem preview nativo em WinForms** e exige plugin extra para PDF — não atende bem ao fluxo "clique em Emitir relatório -> preview" do UX-SPEC 2.3 sem esforço extra de renderização manual.
2. **FastReport .NET Trial** tem preview "In Application" (WinForms) e PDF export nativo, mas é uma avaliação por tempo limitado, sujeita a expirar (~30 dias é a prática usual da FastReport para produtos trial, típica no mercado; a confirmar com instalação real) e a exibir marca d'água nos documentos gerados enquanto não licenciado.
3. **Caminho recomendado para T-49/T-50, já consistente com o que o ADR-008 assumiu por precaução:** implementar **preview via FastReport .NET Trial como caminho preferencial** (melhor experiência, coerente com UX-SPEC 2.3), com **fallback automático para exportação em PDF** (abrindo no visualizador do sistema) quando o preview não estiver disponível — seja por o trial ter expirado, seja pela equipe optar por trocar para o Open Source em algum momento do projeto. A interface `IRelatorioService` já é definida de forma independente da escolha (ADR-008), então a troca de implementação não exige mudança de contrato.
4. Este documento **não substitui** a confirmação visual real (watermark exato, comportamento exato do preview) — isso fica como ação pendente registrada acima, a ser fechada assim que houver ambiente com GUI Windows disponível para o time, antes ou durante T-49/T-50. Cabe ao Coordenador ratificar este caminho em ADR-006/ADR-008 (já alinhado ao texto atual do ADR-008, que previa exatamente esta hierarquia: preview preferencial, fallback PDF).

## Resumo para o README (T-54)

- DevExpress WinForms: obtido do nuget.org público (`DevExpress.Win.Grid` 25.1.9) em modo evaluation/trial de 30 dias (início do relógio via NuGet ainda não confirmado), com avisos DX1000/DX1001 no build, splash de trial e watermark em impressão/exportação. A licença de avaliação **proíbe redistribuição**: a entrega final exige licença DevExpress válida/registrada (por exemplo, se a assinatura Universal existente cobrir WinForms, a confirmar) ou decisão explícita do usuário aceitando entrega em modo evaluation. Conflito com G-2 (custo zero) sinalizado ao Gestor.
- FastReport: usar FastReport .NET Trial para preview WinForms nativo (com marca d'água esperada durante avaliação) ou FastReport Open Source (MIT, sem preview WinForms nativo, PDF via plugin) — ambos custo zero; ver decisão acima. Documentar no README qual dos dois foi efetivamente empacotado na entrega (T-53) e o motivo.
