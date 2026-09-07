# ADR-007: Sincronizar por outbox local com regra de conciliação por entidade

- **Data**: 2026-09-07
- **Status**: Proposed — vira `Accepted` na aprovação do conjunto SDD.md + UX-SPEC.md pelo usuário (orquestrador)
- **Decisores**: coordenador (chapéu Software Architect)
- **Tags**: sincronizacao, dados, offline, resiliencia

## Contexto e Problema

RN-08 já fixa a **política** de conciliação por entidade: progresso é união monotônica;
constância é derivada e nunca sincronizada; preferência é última escrita; esboço é
última escrita no nível do esboço com a versão perdedora recuperável por ≥ 30 dias
(RNF-12); posição de apresentação nunca sai do dispositivo (RN-14). O que falta
decidir é o **mecanismo** que implementa essa política num app que escreve offline
(CA-12.3, CA-12.4) e que precisa migrar progresso anônimo ao criar conta (RN-07).

A tentação, num app com Postgres gerenciado e cliente reativo, é usar a sincronização
em tempo real do provedor e escrever direto na tabela. Isso quebra em dois pontos:
não existe escrita direta sem rede, e a "última escrita" passa a ser decidida pelo
relógio de chegada, não pelo momento em que o usuário de fato editou — o que
contradiz RN-08 ("por data-hora da alteração").

## Decision Drivers

- Escrita local nunca pode falhar nem esperar rede (CA-12.3, CA-12.4).
- "Nunca reduzir o número de dias concluídos" (CA-11.3) precisa ser garantia
  estrutural, não cuidado de código.
- Perder um esboço é dano desproporcional (RNF-12) — a versão perdedora tem de
  sobreviver.
- Um dev solo depura isso sozinho: o mecanismo precisa ser inspecionável.

## Opções Consideradas

- **A. Outbox local de mutações + conciliação por entidade no servidor** ✅ escolhida
- **B. Escrita direta na API com fila de repetição genérica**
- **C. CRDT de texto por bloco (merge automático de esboço)**

## Decision Outcome

Escolhida a opção **A**.

**Escrita local primeiro, sempre.** Toda mutação escreve na store local e enfileira um
registro na `outbox` do IndexedDB: `{ id, entidade, operação, payload, criadoEm
(relógio do dispositivo), baseRev, tentativas, estado }`. A UI reflete o estado local
imediatamente; a sincronização é um processo de fundo, disparado por reconexão, foco
de janela e sucesso de login.

**Idempotência por chave de mutação**: o `id` da mutação é gerado no cliente (UUID) e
enviado ao servidor, que o registra. Reenvio da mesma mutação após timeout não duplica
— resolve CA-03.5 ("não duplicar a conclusão") por construção, e não por uma checagem
de "já existe?" sujeita a corrida.

**Conciliação por entidade**, implementada no schema antes de ser implementada em
código:

| Entidade | Mecanismo |
|---|---|
| Progresso do plano | Tabela append-only com `UNIQUE (user_id, plan_id, day_number)` e `INSERT ... ON CONFLICT DO NOTHING`, preservando o `completed_at` mais antigo. União monotônica vira propriedade do schema; não existe `DELETE` exposto. |
| Constância | Não existe no servidor. Derivada no cliente a partir do progresso conciliado (RN-02). |
| Preferências | Coluna por preferência com `updated_at` do **dispositivo**; escrita aplicada só se `updated_at` recebido > `updated_at` armazenado. |
| Esboço | LWW no nível do esboço, por `updated_at` do dispositivo. O cliente envia `baseRev`; se `baseRev` ≠ `rev` atual do servidor, houve concorrência: o **lado perdedor** é gravado em `outline_version` com `expires_at = agora + 30 dias` antes de o vencedor ser aplicado. Empate de `updated_at` resolve pelo `rev` maior. |
| Posição de apresentação | Store local dedicada, fora da outbox. Nenhuma mutação é criada. |
| Eventos de telemetria | Outbox própria, envio em lote, descartável sob pressão de espaço (ADR-009). |

**Relógio**: `updated_at` é o do dispositivo (é o que RN-08 pede: a última *intenção*),
mas o servidor grava também `received_at`. Divergência acima de um limiar razoável é
registrada para diagnóstico; nunca corrige o dado do usuário em silêncio.

**Migração de progresso anônimo (RN-07)**: o progresso anônimo vive numa store local
separada. No primeiro login do dispositivo, ele é convertido em mutações da outbox
com os `completed_at` originais preservados, enviado pelo mesmo caminho de união e,
só após confirmação do servidor, descartado localmente. Falha de rede no meio deixa o
progresso anônimo intacto — nunca há janela em que ele já foi apagado e ainda não
chegou.

**Recuperação da versão perdedora** é funcionalidade de usuário, não de suporte: a
tela do esboço expõe "versões recuperáveis" enquanto houver registro não expirado
(`UX-SPEC.md` §2.16).

### Consequências Positivas

- Escrita offline funciona por desenho, não por exceção tratada.
- "Nunca reduzir progresso" é garantido por restrição de banco — o pior bug possível
  de cliente não consegue apagar um dia lido.
- Mecanismo inspecionável: a outbox é uma tabela local que dá para abrir no devtools.
- Merge de texto — a fonte clássica de corrupção silenciosa — não existe (I-06).

### Consequências Negativas

- **LWW no nível do esboço perde a edição concorrente do outro dispositivo** da tela
  principal; ela só existe em "versões recuperáveis". É o custo aceito em I-06, e
  exige que a UI comunique isso quando acontecer, em vez de a mudança sumir.
- A outbox é estado adicional a manter, versionar e migrar entre versões do app.
- Relógio do dispositivo pode estar errado; um aparelho com data adiantada vence
  disputas indevidamente. Aceito no MVP (dado privado, sem valor econômico),
  registrado como risco RT-06.
- Descartar telemetria sob pressão de espaço enviesa métricas em dispositivos cheios.

## Links

- `PRD-TECNICO.md` RN-07, RN-08, RN-14, RNF-12, CA-03.5, CA-11.1 a CA-11.3, CA-12.3,
  CA-12.4, I-06
- Relacionado: [ADR-002](002-concentrar-a-logica-de-negocio-no-cliente-com-servidor-de-sincronizacao.md),
  [ADR-009](009-instrumentar-com-telemetria-first-party-em-dois-niveis.md)
