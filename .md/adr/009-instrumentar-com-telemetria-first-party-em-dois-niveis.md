# ADR-009: Instrumentar com telemetria first-party em dois níveis — agregado anônimo antes do consentimento, evento identificado depois

- **Data**: 2026-09-07
- **Status**: Proposed — vira `Accepted` na aprovação do conjunto SDD.md + UX-SPEC.md pelo usuário (orquestrador)
- **Decisores**: coordenador (chapéu Software Architect)
- **Tags**: telemetria, lgpd, privacidade, custo

## Contexto e Problema

RF-18 é **Must** e transversal: sem ele o MVP não prova nem refuta a própria hipótese
(`PRD.md` §3), e as coortes iniciais se perdem para sempre se a instrumentação entrar
depois. Ao mesmo tempo, três restrições colidem frontalmente com a forma padrão de
instrumentar um produto:

1. **CA-18.4 / RNF-05** — os eventos são dado pessoal **sensível**: "concluiu o dia 12
   do plano de leitura bíblica" revela convicção religiosa (LGPD art. 5º, II).
2. **E-07** — restrição dura sobre uso de analytics de terceiro com finalidade
   publicitária. Um SDK de analytics comercial é, por desenho, um coprocessador de
   perfil publicitário.
3. **CA-10.2** — nenhum dado pessoal é persistido no servidor **antes** do
   consentimento.

E há um conflito interno de requisitos que precisa ser resolvido, não contornado:
**M-06 (conversão do soft gate)** exige medir os visitantes que **recusaram** criar
conta. Esses são exatamente os titulares que nunca consentiram. Sob a regra (3), não
se pode persistir nada pessoal deles; sob RF-18/CA-09.5, é obrigatório registrar o
resultado de cada acionamento do gate.

## Decision Drivers

- Nenhum dado que revele convicção religiosa pode sair para terceiro.
- Nada pessoal no servidor antes do consentimento — sem exceção.
- M-06 e M-07 precisam ser apuráveis mesmo assim.
- Custo por evento = zero (RNF-06).
- Exclusão a pedido do titular alcança os eventos (CA-18.4, RN-11).

## Opções Consideradas

- **A. Telemetria first-party própria, em dois níveis de identificabilidade** ✅
  escolhida
- **B. Ferramenta de analytics de produto de terceiro com contrato de operador**
- **C. Analytics de terceiro sem cookie/sem identificador, tipo Plausible**

## Decision Outcome

Escolhida a opção **A**, com uma separação estrita por identificabilidade:

**Nível 1 — agregado anônimo, sem sujeito (pré-consentimento).** Um único ponto de
escrita (`rpc.bump_metric`, `SECURITY DEFINER`) incrementa contadores numa tabela
`analytics_daily_aggregate (data, metrica, contagem)`. Não existe coluna de usuário,
de sessão, de dispositivo, de IP ou de user-agent. **Não há sujeito identificável,
logo não há dado pessoal** — e por isso pode ser gravado sem consentimento. Cobre
`soft_gate_exibido`, `soft_gate_aceito`, `soft_gate_recusado` (CA-09.5, M-06),
`vitrine_aberta` e `falha_offline_apresentacao` (M-04). Nenhuma correlação entre
contadores é possível, o que é a garantia de anonimato — e também o limite do nível 1:
ele responde "quantos", nunca "quem" nem "em que sequência".

**Nível 2 — evento identificado (pós-consentimento).** Eventos com `user_id`, tipo,
`occurred_at` (relógio do dispositivo) e payload tipado, em `analytics_event`,
protegidos por RLS igual a qualquer outro dado do titular. Cobrem o resto de CA-18.1:
início de plano, conclusão de dia (número do dia, data, classificação do conteúdo AT),
lembrete enviado e desfecho, criação e duplicação de esboço, conclusão do plano.

**Ponte entre os dois níveis, que é o que salva M-07.** Enquanto o visitante está sem
conta, seus eventos de nível 2 ficam **apenas na outbox local** — nada sai do
dispositivo. Ao consentir (CA-10.2), a mesma migração que leva o progresso anônimo
(RN-07) leva a fila de eventos, com os `occurred_at` originais preservados. Resultado:
para todo usuário que cria conta, a história completa desde a sessão 1 existe — que é
exatamente o que M-07 (dia 1 → dia 2 em 48 h) e a coorte D30 precisam. Para quem nunca
cria conta, só existe o agregado anônimo. Nenhuma métrica declarada no `PRD.md` §3 é
perdida por essa fronteira; verificado métrica a métrica no `SDD.md` §2.6.

**Apuração**: as métricas são views SQL versionadas no repositório (`m01_d30_coorte`,
`m02_muro_arido`, `m03_recorrencia`, `m04_falha_offline`, `m06_soft_gate`,
`m07_sessao2`, `m08_gancho`), não um painel de terceiro. O consumo é por consulta
autenticada do próprio stakeholder.

**Exclusão (RN-11)**: os eventos do nível 2 são apagados em cascata junto da conta. Os
agregados do nível 1 não são afetados — não há como identificá-los, e é exatamente
isso que os torna legítimos.

**Proibição associada**: nenhum script de terceiro é carregado em nenhuma página. A
CSP `default-src 'self'` é o mecanismo que garante isso mecanicamente, e ela cumpre de
quebra CA-08.2 (nenhum anúncio de terceiro em nenhuma tela).

### Consequências Positivas

- E-07 satisfeita de forma estrutural: não existe destino de terceiro para vazar.
- M-06 apurável sem tratar dado de quem recusou — o conflito de requisitos é
  resolvido, não adiado.
- Custo por evento zero; os eventos moram na mesma base já paga (zero) do produto.
- Exclusão a pedido do titular é `DELETE ... CASCADE`, não um pedido a um fornecedor.

### Consequências Negativas

- **Nenhum painel pronto.** Funil, retenção e coorte saem de SQL escrito à mão. Custo
  real de tempo do dev solo — mitigado por views prontas desde o início, mas é
  trabalho que uma ferramenta daria de graça.
- **Cega para quem nunca cria conta**, além dos contadores. Não há como saber por onde
  o visitante anônimo navegou. Aceito: é a contrapartida direta da restrição legal.
- Eventos com relógio do dispositivo podem chegar com data errada; a janela de 48 h de
  M-07 é sensível a isso. Mitigação: gravar também `received_at` e descartar da
  apuração eventos com divergência absurda.
- Telemetria descartável sob pressão de espaço (ADR-007) enviesa a favor de
  dispositivos com espaço livre.

## Links

- `PRD-TECNICO.md` RF-18, CA-09.5, CA-18.1 a CA-18.4, RNF-05, E-07; `PRD.md` §3
- Relacionado: [ADR-007](007-sincronizar-por-outbox-local-com-regra-por-entidade.md),
  [ADR-008](008-usar-supabase-como-backend-gerenciado-e-cloudflare-pages-como-entrega.md)
