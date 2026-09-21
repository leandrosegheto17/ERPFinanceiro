---
name: api-integration
description: Integra o cliente (Frontend ou Mobile) com os endpoints do contrato de API do Backend, tratando corretamente os códigos de erro documentados e, no Mobile, cenários de conectividade instável/offline. Se o endpoint está publicado mas ainda não implementado de verdade, implementa contra mock gerado do contrato, sem bloquear. Use depois que a estrutura visual (ui-implementation ou app-screen-implementation) já existe. Do NOT use for construir a tela em si ou para decidir o formato do contrato (isso é do backend).
metadata:
  author: frontend, mobile
  version: '1.1.0'
---

# API Integration

Você atua como Frontend ou Mobile Developer (conforme o time da tarefa) conectando a
interface já construída aos dados reais — consumindo o contrato publicado pelo
Backend, tratando todo código de erro documentado, e usando mock gerado do próprio
contrato quando a implementação real ainda não está pronta, sem bloquear o andamento
da tarefa.

## Quando é Acionada

- Depois que a estrutura visual da tela/componente já existe (`ui-implementation` no
  Frontend, `app-screen-implementation` no Mobile), para toda tarefa que consome
  dado de um endpoint.

Do NOT use for:
- Construir a estrutura visual da tela — isso roda antes desta skill.
- Decidir o formato do contrato de API — isso é `api-contract-design`, do Backend;
  esta skill consome o contrato já publicado, não o define.

## Inputs Esperados

- `API-CONTRACT.yaml` (obrigatório) — endpoint(s) que a tarefa consome, com payload
  de entrada/saída e códigos de erro.
- Estrutura visual já construída (obrigatório).
- No Mobile: requisito de comportamento offline/conectividade instável, se aplicável
  ao projeto (do PRD-TECNICO.md ou UX-SPEC.md).

Se o endpoint necessário **não existe** no `API-CONTRACT.yaml`, esta skill não roda
— a tarefa aguarda o Backend publicar o contrato (ver guardrail do agente
`frontend`/`mobile`).

## Core Framework

1. **Endpoint publicado, implementação real pronta.** Integra direto contra a API
   real.
2. **Endpoint publicado, implementação real ainda não pronta.** Gera um mock a
   partir do schema do contrato (payload de sucesso e de cada erro documentado) e
   integra contra ele — a tarefa segue `Em andamento`, nunca `Concluída`, com nota
   explícita de que está em mock.
3. **Tratamento de erro completo.** Todo código de erro documentado no contrato
   para este endpoint tem tratamento correspondente na interface (não só o
   sucesso) — inclusive contra o mock, que deve simular os erros também, não só o
   caminho feliz.
4. **Conectividade instável/offline (Mobile, quando aplicável).** Se o projeto
   exige comportamento offline, a integração trata: fila de ação pendente,
   indicação visual de "sem conexão", e sincronização quando a conexão volta —
   conforme especificado no UX-SPEC.md/PRD-TECNICO.md, nunca assumido por padrão.
5. **Troca de mock para real.** Quando o Backend publica a implementação real do
   endpoint (sinalizado pelo status da tarefa correspondente de backend no
   `TASK.md`), a integração troca do mock para a chamada real e valida que o
   comportamento (inclusive erros) bate com o que o mock simulava.
6. **Detecção de contrato alterado.** O `info.version` do `API-CONTRACT.yaml` é o
   sinal de que o contrato mudou de forma incompatível (convenção definida em
   `api-contract-design`, do Backend) — se a versão consumida por uma integração já
   feita mudou, revalide contra o novo contrato antes de considerar a tarefa
   inalterada.

## Workflow

1. Verifique se o endpoint necessário está publicado no `API-CONTRACT.yaml`.
2. Se a implementação real já está pronta: integra direto.
3. Se não está pronta: gera mock a partir do schema (sucesso + todos os erros
   documentados) e integra contra ele; marca a tarefa `Em andamento` com nota de
   mock.
4. Implemente o tratamento de cada código de erro documentado, contra o mock ou a
   API real.
5. No Mobile, se o projeto exige comportamento offline: implemente o tratamento de
   conectividade instável conforme especificado, não por suposição.
6. Quando a API real ficar disponível: troca a integração, revalida o
   comportamento, remove o mock.

## Output Esperado

- **Formato**: código-fonte (camada de integração/chamada de API), incluindo o mock
  gerado do contrato quando aplicável (marcado claramente como mock no código, não
  misturado com a chamada real).
- **Onde salva**: árvore de código do projeto, conforme convenção de pastas do
  TASK.md Seção 1.

## Critério de Aceite

- [ ] Todo código de erro documentado no contrato tem tratamento correspondente na
      interface
- [ ] Mock (quando usado) simula sucesso e todos os erros documentados, não só o
      caminho feliz
- [ ] Mock está claramente marcado como tal no código — nunca misturado de forma
      ambígua com a chamada real
- [ ] No Mobile, quando aplicável: comportamento de conectividade instável/offline
      implementado conforme especificado, não assumido
- [ ] Tarefa só é considerada pronta para `Concluída` depois de trocar mock por API
      real e revalidar o comportamento
- [ ] Integração já feita foi revalidada sempre que o `info.version` do endpoint
      consumido mudou desde a última checagem

### MUST DO
- Implementar tratamento para todo código de erro documentado no contrato, não só o
  sucesso.
- Marcar claramente no código quando a integração está contra mock.

### MUST NOT DO
- Marcar a tarefa como `Concluída` enquanto a integração ainda roda contra mock.
- Aguardar passivamente a implementação real do Backend quando o contrato já está
  publicado — isso é exatamente o que o mock existe para evitar.
- Assumir comportamento offline "por precaução" quando o projeto não especificou
  esse requisito.
