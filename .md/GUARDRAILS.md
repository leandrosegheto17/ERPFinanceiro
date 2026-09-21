# GUARDRAILS.md — ERP Financeiro (C#)

> **VEREDITO GESTOR (21/09/2026): APROVADO COM RESSALVAS.** Regras consistentes com PRD/PRD-TECNICO/CTO-REVIEW (prazo 25/09, custo zero, Tiers A/B/C, Firebird com spike/fallback, itens "a validar"); sem contradições ou regras inexequíveis. Ressalvas (sem reescrita das regras): (a) confirmar horas/dia no Dia 1 (CTO-REVIEW rodada 2): sem 10h/dia só o P0 cabe, então Tier B fica condicionado; (b) G-1.3 cita O-12 como "nunca cortar", mas o CTO-REVIEW estima P0=43h "sem O-12": Coordenador deve esclarecer no TASK.md se O-12 é P0; (c) G-5.20 (v1.1 só com aceite do Vendas) e CTO-REVIEW achado 2 (congelar v1.1 no Dia 1): tratar o congelamento como interno, sem preencher a linha v1.1; (d) confirmar limites dos trials (D-09) no Dia 1, antes de O-02. Fonte da proposta: `coordenador` (PIPELINE-CONVENTIONS §5). Extraído de `CTO-REVIEW.md`, `SDD.md`, ADR 001-008, `TASK.md` e restrições do usuário.
> Regras inegociáveis; exceção só com aprovação do Gestor, registrada no Log de Alterações.

## G-1 Escopo e prazo
1. Desenvolvimento termina em **25/09/2026 (Dia 5)**. 26 e 27/09: só entrega (README, empacotamento, máquina limpa) e correção de bug **bloqueante**; nenhuma funcionalidade nova. (D-07, RNF-15)
2. Escopo por tiers: Tier A obrigatório; Tier B só depois de T-40 (O-11) concluída e com folga; Tier C (S-03 tela, S-10, S-12, S-13, resto de S-11) **não implementar** neste ciclo.
3. Nunca cortar: O-01…O-12, S-01, S-04, S-05, S-08. Se apertar, cai acabamento de tela/relatório, nunca integração nem persistência.
4. Marcos: API utilizável pelo Vendas até o fim do Dia 3; integração real (O-11) até o fim do Dia 4.

## G-2 Stack e custo
5. Stack obrigatória: C# .NET Framework 4.8, EF6 (**não** EF Core), DevExpress WinForms, FastReport, Firebird 3.0 embarcado. Substituir só com ADR e aviso.
6. **Custo zero**: nenhum pacote/licença paga; DevExpress e FastReport em trial/edição gratuita; versões e licenças no README.
7. Firebird embarcado depende do **go/no-go de T-01**. Sem GO registrado (ADR-009), nada de T-04, T-05, T-11, T-12 começa. No-go = fallback SQL Server Express/LocalDB por ADR.
8. Proibido: AutoMapper, EF Core, Serilog em Tier A, pacotes não oficiais. `PlatformTarget` fixo, nunca AnyCPU.

## G-3 Arquitetura
9. Monólito em camadas, dependências para dentro: Domain sem dependências; Application sem EF/Web API/DevExpress; Api não referencia Infrastructure; Desktop só referencia Infrastructure no composition root.
10. Toda regra de negócio em Domain/Application; controllers só traduzem DTO <-> comando. DTO da API nunca é entidade EF.
11. Tela **não chama a API por HTTP**: usa serviços em processo (ADR-008). Um único processo abre o `.fdb`.
12. Banco próprio, sem FK/credenciais compartilhadas com o Vendas (ADR-003).
13. ADRs são imutáveis: mudança de decisão = novo ADR que supersede o anterior.

## G-4 Dados e integridade
14. Dinheiro sempre `decimal`/DECIMAL, nunca `float`/`double`. Datas UTC.
15. Histórico **append-only** (só INSERT). Venda + itens + histórico na mesma transação.
16. Concorrência: `UNIQUE(VENDA_ID)` + `VERSAO` incrementada pela aplicação (sem trigger de versão) + releitura máx. 3 tentativas.
17. Idempotência: repetição devolve o resultado original sem novo histórico.
18. DDL manual em `database/`, sem migrations; identificadores <= 31 caracteres; DECIMAL <= 18 dígitos. Entrega = `.sql` + `.fdb`.
19. Consultas somente via EF/parâmetros; nunca concatenar SQL.

## G-5 Contrato e API
20. Contrato v1.0 preservado; mudanças **só aditivas**. Toda alteração aprovada registrada em VISAO-PRODUTO 2.4 e comunicada ao Vendas (a linha v1.1 só é preenchida com aceite do Vendas).
21. Envelope de erro `{erro:{codigo,mensagem}}` sem stack trace; códigos do SDD 2.4.
22. `X-Api-Key` em todos os endpoints exceto `GET /api/health`; comparação em tempo constante; 401 sem revelar o motivo.
23. Pontos **A VALIDAR** (D-03, D-04, D-05, D-06, D-08, D-10, D-11, contrato v1.1) são premissas: nunca tratar como decididos em documentação externa; divergência gera BLOCKERS.md + novo ADR.

## G-6 Segurança
24. `ApiKey` e senha do banco fora do código-fonte; `App.config.example` sem segredo real; nunca em log.
25. Bind padrão `http://localhost:{porta}/`; abrir para a rede só por configuração explícita e documentada.
26. Limitações aceitas documentadas no README: API Key em texto simples no `.config`, sem TLS, `.fdb` sem criptografia, tela sem login.

## G-7 UX e acessibilidade
27. Apenas controles DevExpress padrão; único componente novo: `CustomDrawEmptyForeground`.
28. Toda tela [A] com os 4 estados (vazio, carregando, erro, sucesso) ou justificativa do UX-SPEC.
29. Acessibilidade não negociável: teclado completo, `AccessibleName`, status sempre ícone + texto (cor só reforço), DPI 125/150%, verificação com Narrador (T-52).
30. Tela somente leitura (sem quitar/cancelar manual).

## G-8 Processo de trabalho
31. Toda tarefa entra com critério de aceite verificável e evidência registrada; tarefa que projetar > ~300k tokens de contexto ou > ~1 dia-pessoa deve ser dividida e não executada como está.
32. Nada de Tier B/C antes de T-40. Nenhum trabalho novo sobre artefato com bloqueio `Aberto` que o afete.
33. Toda mudança neste arquivo passa pelo Gestor e entra no Log de Alterações.

## Log de Alterações
| Data | Proposto por | Aprovado por | Mudança | Motivo |
|---|---|---|---|---|
| 2026-09-21 | coordenador | gestor (Aprovado com ressalvas) | Criação do rascunho inicial (G-1 a G-8) | Loop C, rodada 1; aprovação registrada em /definir_organizar Seção 4, ressalvas a-d no cabeçalho |
