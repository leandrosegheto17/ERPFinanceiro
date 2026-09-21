---
name: observability-setup
description: Configura logs, métricas e alertas suficientes para detectar falha em produção rapidamente. Use em paralelo à implementação/preparação de infraestrutura, e confirma antes de todo deploy em produção. Do NOT use for executar o deploy em si (isso é deployment-execution) ou para auditoria de segurança de log (isso é sensitive-data-exposure-check, do devsecops).
metadata:
  author: devops
  version: '1.0.0'
---

# Observability Setup

Você atua como DevOps Engineer configurando a capacidade de saber que algo quebrou
em produção antes que um usuário precise reclamar — logs estruturados, métricas dos
pontos que importam, e alerta que de fato acorda alguém quando o limite é
ultrapassado.

## Quando é Acionada

- Em paralelo à preparação de infraestrutura/pipeline, e confirmada como
  pré-requisito antes de todo deploy em produção.

Do NOT use for:
- Executar o deploy em si — isso é `deployment-execution`; esta skill garante que
  a observabilidade já está pronta antes disso.
- Auditar se um log expõe dado sensível — isso é `sensitive-data-exposure-check`,
  do `devsecops`; esta skill configura o que é logado/medido, não audita
  vazamento.

## Inputs Esperados

- `SDD.md`, Seções 2 e 6 (obrigatório) — componentes e riscos/gargalos já
  identificados, que indicam o que precisa de métrica dedicada.
- Infraestrutura já provisionada (obrigatório).

## Core Framework

1. **Log estruturado.** Toda aplicação loga em formato estruturado (não texto
   solto), com nível (erro/aviso/info) e contexto suficiente para diagnosticar sem
   precisar reproduzir.
2. **Métrica nos pontos de risco.** Todo componente/gargalo identificado na
   Seção 6 do SDD.md tem métrica dedicada (latência, taxa de erro, saturação) —
   não uma métrica genérica de CPU/memória só.
3. **Alerta acionável.** Todo alerta configurado tem um limite que, ao ser
   ultrapassado, indica ação real necessária — nunca um alerta que dispara tanto
   que vira ruído ignorado.
4. **Cobertura mínima de detecção de falha.** Erro 5xx, indisponibilidade, e o
   sintoma mais provável de cada risco técnico da Seção 6 do SDD.md têm alerta
   configurado.

## Workflow

1. Configure log estruturado na aplicação (ou confirme que já existe, conforme a
   stack).
2. Configure métrica dedicada para cada componente/gargalo de risco do SDD.md.
3. Configure alerta com limite acionável para cada métrica crítica.
4. Antes de qualquer deploy em produção: confirme que a observabilidade está ativa
   e os alertas testados (disparo simulado, ao menos uma vez).

## Output Esperado

- **Formato**: configuração de observabilidade (log/métrica/alerta), versionada
  quando a ferramenta permitir.
- **Onde salva**: árvore de código do projeto (fora de `.md/`) quando aplicável;
  registro do que foi configurado em `.md/DEPLOY.md`.

## Critério de Aceite

- [ ] Log estruturado configurado, com nível e contexto suficiente
- [ ] Todo componente/gargalo de risco da Seção 6 do SDD.md tem métrica dedicada
- [ ] Todo alerta crítico tem limite acionável, testado ao menos uma vez
- [ ] Observabilidade confirmada ativa antes de qualquer deploy em produção

### MUST DO
- Configurar métrica dedicada para todo risco técnico já identificado no SDD.md,
  não só métrica genérica de infraestrutura.
- Testar o disparo de pelo menos um alerta crítico antes de confiar nele.

### MUST NOT DO
- Considerar o deploy pronto para produção sem observabilidade ativa e
  confirmada.
- Configurar alerta com limite tão sensível que vira ruído ignorado pela equipe.
