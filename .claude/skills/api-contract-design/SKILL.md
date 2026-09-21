---
name: api-contract-design
description: Define e documenta contratos de API (endpoints, payloads, códigos de erro) a partir do TASK.md e SDD.md, publicando cedo para desbloquear Frontend/Mobile. Use assim que uma tarefa de backend expõe endpoint, mesmo antes da implementação completa terminar. Do NOT use for implementar a lógica por trás do endpoint (isso é business-logic-implementation) ou para definir o modelo de dados (isso é data-model-implementation).
metadata:
  author: backend
  version: '1.0.0'
---

# API Contract Design

Você atua como Backend Developer definindo o contrato de um endpoint — método,
caminho, payload de entrada/saída, códigos de erro — cedo o suficiente para Frontend
e Mobile começarem a integrar sem esperar a implementação inteira do backend
terminar. O contrato é o compromisso público; a implementação por trás dele pode
continuar evoluindo desde que o contrato publicado se mantenha.

## Quando é Acionada

- Assim que uma tarefa de backend do TASK.md envolve expor um endpoint — idealmente
  antes ou no início da implementação, não no final.

Do NOT use for:
- Implementar a lógica de negócio por trás do endpoint — isso é
  `business-logic-implementation`; esta skill define o contrato, não o comportamento
  interno.
- Definir o modelo de dados persistido — isso é `data-model-implementation`; o
  payload do contrato pode ser uma projeção do modelo, não o modelo em si.

## Inputs Esperados

- Tarefa do `TASK.md` que envolve endpoint (obrigatório).
- `SDD.md`, Seções 2 (Fluxo de Dados) e 5 (Modelo de Dados de Alto Nível)
  (obrigatório) — para saber que dado entra e sai de cada componente.
- `UX-SPEC.md` (contexto, se a tarefa tem contraparte de tela) — para garantir que o
  contrato fornece o que a tela precisa consumir.

## Core Framework

Para cada endpoint:

1. **Método e caminho.** Segue a convenção REST (ou o padrão já definido no
   SDD.md/ADRs) — verbo HTTP correto para a operação, caminho consistente com os
   outros endpoints já publicados.
2. **Payload de entrada.** Todo campo, tipo, obrigatoriedade e validação.
3. **Payload de saída.** Todo campo do sucesso, e o formato de erro (código HTTP +
   corpo de erro padronizado, consistente entre todos os endpoints do projeto).
4. **Códigos de erro.** Todo caso de exceção já mapeado pelo BA (PRD-TECNICO.md)
   que se aplica a este endpoint tem um código de erro correspondente.
5. **Versionamento do contrato.** Todo `API-CONTRACT.yaml` tem um `info.version`
   (semver). Mudança compatível (novo endpoint, novo campo opcional) não exige
   incrementar a versão. Mudança incompatível (campo removido/renomeado, tipo
   alterado, código de erro removido) sempre incrementa a versão — é assim que
   Frontend/Mobile detectam que precisam revisar a integração, não por inspeção
   manual do diff.

## Workflow

1. Para cada tarefa que expõe endpoint, defina o contrato usando o framework acima.
2. Publique o contrato assim que estável — não espere a implementação da lógica
   interna terminar.
3. Se precisar alterar um contrato já publicado de forma incompatível, incremente
   `info.version` e marque a mudança explicitamente (não sobrescreva em silêncio)
   — Frontend/Mobile que já consomem precisam saber.
4. Escreva/atualize `API-CONTRACT.yaml` em formato OpenAPI 3.x.

## Output Esperado

- **Formato**: OpenAPI 3.x (YAML) — um path/operation por endpoint, com schemas de
  request/response e componentes de erro reutilizáveis.
- **Onde salva**: `.md/API-CONTRACT.yaml` (arquivo único, atualizado
  incrementalmente — ver PIPELINE-CONVENTIONS.md, exceção de nomenclatura).

## Critério de Aceite

- [ ] Todo endpoint tem payload de entrada e saída completamente tipado
- [ ] Todo caso de exceção relevante do PRD-TECNICO.md tem código de erro
      correspondente no contrato
- [ ] Contrato publicado assim que estável, não represado até o fim da
      implementação
- [ ] Mudança incompatível em contrato já publicado está marcada explicitamente,
      nunca silenciosa

### MUST DO
- Publicar o contrato assim que estável, mesmo com a lógica interna ainda em
  desenvolvimento.
- Manter formato de erro consistente entre todos os endpoints do projeto.

### MUST NOT DO
- Represar a publicação do contrato até a tarefa inteira estar implementada —
  isso bloqueia Frontend/Mobile desnecessariamente.
- Alterar um contrato já publicado de forma incompatível sem sinalizar a mudança.
