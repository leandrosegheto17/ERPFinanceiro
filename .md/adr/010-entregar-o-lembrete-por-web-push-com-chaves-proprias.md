# ADR-010: Entregar o lembrete por Web Push com chaves próprias e não adotar e-mail como canal diário

- **Data**: 2026-09-07
- **Status**: Proposed — vira `Accepted` na aprovação do conjunto SDD.md + UX-SPEC.md pelo usuário (orquestrador)
- **Decisores**: coordenador (chapéu Software Architect) — **resolve a premissa P-08**, cujo dono declarado é o Coordenador no `SDD.md`
- **Tags**: notificacao, custo, ios, produto

## Contexto e Problema

RF-05 subiu para **Must** na rodada 2 porque o gancho de continuidade (RN-04) é a
alavanca declarada contra o abandono precoce — o funil que acontece **antes** do muro
do árido (`PRD.md` §1.1). O corpo da notificação tem de ser o gancho (CA-05.3), nunca
uma frase genérica.

P-08 está aberta e é minha: "existe canal capaz de entregar o gancho ao público-alvo?"
O plano B registrado no `PRD.md` é "e-mail e/ou retomada in-app". Duas forças em
tensão:

- **Cobertura**: no iOS, Web Push só funciona em PWA **instalado** na tela de início
  (Safari 16.4+). Uma fatia relevante do público-alvo (celular como dispositivo
  primário) não instala PWA espontaneamente.
- **Custo**: e-mail diário é a **única** fonte de custo recorrente por usuário que
  sobrou no MVP depois do corte do áudio. 200 usuários × 1 lembrete/dia = 6.000
  e-mails/mês, já acima do plano gratuito típico (3.000/mês); 2.000 usuários = 60.000.
  O custo cresce linearmente com adoção — que é exatamente o que RNF-06 e a ressalva
  R-05 do Gate 1 proíbem.

## Decision Drivers

- RNF-06 / R-05: nenhuma dependência cujo custo cresça por usuário ativo.
- CA-05.5: o gancho **nunca** pode depender exclusivamente de push — a última linha de
  defesa é in-app e é obrigatória em qualquer cenário.
- CA-05.7 + M-08: envio e desfecho de cada lembrete precisam ser registrados.
- R-01: um canal a operar, não três.

## Opções Consideradas

- **A. Web Push (VAPID) como único canal de envio + gancho in-app garantido + convite
  de instalação contextual** ✅ escolhida
- **B. Web Push + e-mail diário para quem não tem push**
- **C. Somente gancho in-app, sem push nenhum**
- **D. Serviço gerenciado de notificação (OneSignal e similares)**

## Decision Outcome

Escolhida a opção **A**, em três camadas de degradação, na ordem em que o usuário as
encontra:

1. **Web Push com chaves VAPID próprias.** O envio usa o serviço de push do próprio
   navegador (FCM/APNs via o navegador), que **não cobra do desenvolvedor** e não
   exige SDK de terceiro. Agendamento por `pg_cron` disparando uma Edge Function que
   seleciona os usuários com lembrete ativo, dia corrente não concluído (CA-05.4) e
   horário atingido no fuso `America/Sao_Paulo`, e envia o gancho do dia corrente como
   corpo. Cada tentativa grava linha em `reminder_log` (agendado, enviado, falhou,
   desfecho) — insumo de CA-05.7 e M-08.
2. **Convite de instalação, quando o push é impossível.** No iOS sem PWA instalado, a
   ativação do lembrete não finge que funcionou: explica em português claro que aquele
   navegador só entrega lembrete com o app na tela de início, ensina a instalar em um
   passo, e oferece continuar sem lembrete. O mesmo convite serve a ADR-006
   (armazenamento persistente) — é o mesmo ato do usuário resolvendo os dois
   problemas, e por isso vale insistir nele.
3. **Gancho in-app, sempre.** Independentemente de push, o gancho do dia corrente
   aparece na retomada in-app (CA-05.5) e ao concluir o dia (CA-03.6). A alavanca de
   continuidade não deixa de existir para ninguém — só o *empurrão* deixa.

**E-mail fica restrito a transacional de identidade**: verificação de conta,
recuperação de acesso e confirmação de exclusão (RN-11). Volume por conta, não por
dia; cabe no plano gratuito com folga e não escala com adoção.

**Por que não a opção B, que é o plano B literal do `PRD.md`**: e-mail diário é
recorrente e cresce com o número de usuários — viola RNF-06 e obrigaria a voltar ao
Gate 1 por R-05. Além disso, e-mail diário automático para uma base que não pediu
newsletter tem taxa de abertura baixa e risco de reputação de domínio que um dev solo
não tem como gerir. **Esta é a parte da decisão que precisa de confirmação explícita
do usuário**, porque RA-09 foi promovida a Must contando com a existência de um canal
alternativo — está sinalizada no `SDD.md` §6 (RT-02) e na devolutiva desta rodada.

### Consequências Positivas

- Custo de notificação exatamente zero, em qualquer escala.
- Nenhum SDK de terceiro na página — coerente com a CSP de ADR-009 e com E-07.
- O convite de instalação resolve, num único ato do usuário, push **e** persistência
  de armazenamento (RNF-01) — os dois pontos fracos do PWA no iOS.
- M-08 apurável por plataforma, o que transforma a incerteza de P-08 em número medido
  na primeira semana.

### Consequências Negativas

- **Cobertura desigual e conhecida**: usuário de iPhone que não instala o PWA não
  recebe lembrete algum. Se essa fatia for grande, M-08 fica baixa por motivo de
  plataforma, não de conteúdo do gancho — o que **contamina a leitura de P-13**. Vira
  requisito de análise: M-07/M-08 sempre segmentadas por "tem push / não tem push".
- Agendamento por cron de plano gratuito não tem garantia de pontualidade ao minuto;
  o lembrete pode atrasar alguns minutos. Aceitável para um lembrete devocional
  diário, inaceitável se um dia virar alarme.
- Sem canal alternativo, um usuário que desinstala o PWA perde o lembrete
  silenciosamente. Mitigação: o gancho in-app cobre a retomada, e a tela de
  configurações mostra o estado real do canal, não o estado desejado.
- Implementar envio Web Push exige lidar com chaves VAPID, expiração de assinatura e
  limpeza de assinaturas mortas — trabalho de infraestrutura que um serviço gerenciado
  daria pronto. Candidato natural a spike técnico no `TASK.md`.

## Links

- `PRD-TECNICO.md` RF-05, CA-05.1 a CA-05.7, RNF-06, RNF-07, E-03; `PRD.md` P-08,
  P-13, M-08; `CTO-REVIEW.md` R-05
- Relacionado: [ADR-001](001-adotar-pwa-instalavel-como-unica-plataforma-cliente-do-mvp.md),
  [ADR-006](006-garantir-o-offline-duro-por-esboco-autocontido.md)
