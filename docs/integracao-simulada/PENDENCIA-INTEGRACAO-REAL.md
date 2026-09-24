# Pendência — Integração real com o Vendas (Delphi)

## Status: em aberto, fora do alcance deste sandbox (não resolvida por T-38/T-39/T-40)

Este documento é a referência única e durável da pendência de **integração real**
com o sistema Vendas (Delphi), para ser linkada/incorporada pelo README final do
projeto (T-54, empacotamento/entrega — ver `.md/TASK.md`, Lote 13). Ele não
substitui `.md/BLOCKERS.md` (Bloqueio 002) nem
`.md/adr/010-integracao-vendas-simulada-sem-acesso-delphi.md` — complementa os
dois com uma lista objetiva e acionável do que falta.

## O que já foi feito (não é o que falta)

- `docs/contrato-v1.1.md` (T-03) escrito, com exemplos JSON reais por endpoint e
  código de erro, e a lista de pontos "A VALIDAR" (D-03, D-05, D-06, D-08,
  P-1…P-9).
- Suíte de integração **simulada** (T-38/T-39, ADR-010): harnesses HTTP reais
  (`tools/T-38-integracao-simulada/`, `tools/T-39-integracao-simulada/`) fazendo
  o papel do Vendas contra a API real (`ApiHost`+`Startup`+
  `CompositionRoot.Construir()` + Firebird embarcado real), cobrindo
  tecnicamente os mesmos cenários do critério de aceite original (idempotência
  P-2, cancelamento em cada estado, reenvio pós-timeout, 400/401/409). Evidência
  em `docs/integracao-simulada/evidencia-execucao-T-38.txt` e
  `evidencia-execucao-T-39.txt`, roteiro em `docs/integracao-simulada/README.md`.
- Essa suíte simulada dá confiança técnica de que a API se comporta conforme o
  contrato quando chamada por um cliente HTTP real. **Ela não prova que o
  sistema Vendas (Delphi) real, com sua própria lógica de retry/timeout/parsing,
  de fato se integra corretamente** — só um agente Delphi real pode confirmar
  isso.

## O que falta acontecer para a integração real ser executada

1. **Aceite formal do `docs/contrato-v1.1.md` pelo time Vendas** (T-03).
   - Envio do documento ao time/responsável Vendas ainda não foi feito
     ("sem acesso ao Vendas neste ambiente" — nota de T-03 no `TASK.md`).
   - Aceite deve ser registrado ponto a ponto (cada item de "A VALIDAR":
     D-03, D-05, D-06, D-08, P-1…P-9 — aceito/rejeitado/renegociado), não como
     "ok" genérico.
   - Pontos rejeitados exigem nova rodada de ajuste do contrato (mesmo padrão já
     aplicado por T-37 quando um ponto precisou de correção).

2. **Acesso a um ambiente com o sistema Vendas (Delphi) real, ou um ambiente de
   homologação conjunto** — nenhum dos dois existe neste sandbox.
   - Alternativa mínima viável: ambiente de homologação onde o Vendas real (ou
     uma build de teste dele) consegue disparar HTTP contra uma instância desta
     API (mesmos endpoints de `docs/contrato-v1.1.md`), com rede/DNS/certificado
     resolvidos entre os dois lados.
   - Se não houver ambiente de homologação, o mínimo aceitável é uma sessão
     conjunta (time Vendas + time desta API) rodando os mesmos cenários de
     T-38/T-39 (quitação, repetição idempotente, cancelamento em cada estado,
     reenvio pós-timeout, erros 400/401/409) a partir do Vendas real, com
     evidência de request/response registrada da mesma forma que
     `evidencia-execucao-T-38.txt`/`evidencia-execucao-T-39.txt`.

3. **Responsáveis e próximos passos.**
   - Do lado desta API (ERP Financeiro): time/execução que produziu este
     projeto (ver `.md/GESTOR.md`/histórico de commits) — disponível para apoiar
     a sessão de integração real assim que o item 2 acima estiver disponível.
   - Do lado Vendas (Delphi): **não identificado neste sandbox** — não há
     contato/responsável nomeado nos artefatos do projeto (`SDD.md`,
     `TASK.md`, `VISAO-PRODUTO.md`); é uma decisão/ação do usuário/Gestor do
     projeto identificar e acionar esse responsável.
   - Próximo passo concreto, nesta ordem: (a) usuário/Gestor identifica o
     responsável Vendas e agenda o envio do contrato (item 1); (b) em paralelo,
     avaliar se existe ou pode ser criado um ambiente de homologação (item 2);
     (c) quando ambos estiverem prontos, repetir os cenários de T-38/T-39 contra
     o Vendas real e registrar a evidência, atualizando este documento e
     `.md/BLOCKERS.md` (Bloqueio 002) com o resultado.

## Quando este documento pode ser considerado resolvido

Quando existir evidência de execução real (request/response) dos cenários de
T-38/T-39 disparados pelo sistema Vendas (Delphi) real — não simulado — contra
esta API, com aceite formal do contrato v1.1 registrado. Até lá, qualquer
"concluído" reportado sobre integração com o Vendas se refere apenas à suíte
simulada (T-38/T-39, ADR-010), nunca à integração real.

## Referências

- `.md/BLOCKERS.md`, Bloqueio 002 (relato original, decisão do Coordenador,
  status).
- `.md/adr/010-integracao-vendas-simulada-sem-acesso-delphi.md` (decisão e
  alternativas consideradas).
- `docs/contrato-v1.1.md` (T-03 — contrato a ser aceito).
- `docs/integracao-simulada/README.md`,
  `docs/integracao-simulada/evidencia-execucao-T-38.txt`,
  `docs/integracao-simulada/evidencia-execucao-T-39.txt` (o que já foi validado
  de forma simulada).
