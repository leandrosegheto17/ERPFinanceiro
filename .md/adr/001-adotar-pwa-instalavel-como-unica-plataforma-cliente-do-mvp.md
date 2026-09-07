# ADR-001: Adotar PWA instalável como única plataforma cliente do MVP

- **Data**: 2026-09-07
- **Status**: Proposed — vira `Accepted` na aprovação do conjunto SDD.md + UX-SPEC.md pelo usuário (orquestrador)
- **Decisores**: coordenador (chapéu Software Architect), a partir de decisão de produto do `PRD.md` §1.4 e §4.5 F-12
- **Tags**: plataforma, mobile, offline, custo

## Contexto e Problema

O `PRD.md` já fixa "web responsivo / PWA primeiro; nativo depois" como decisão de
produto. Ainda assim, três forças técnicas poderiam invalidar essa decisão herdada e
precisam ser testadas antes de fechá-la, porque revertê-la depois é caro:

1. **RNF-01 (offline duro)** — o modo apresentação precisa abrir o primeiro cartão em
   ≤ 1 s **sem rede**. Qualquer requisição de rede em tempo de apresentação reprova o
   requisito. Historicamente é o argumento mais forte a favor de nativo.
2. **RF-05 (lembrete diário) virou Must** e o iOS restringe Web Push a PWAs
   **instalados** na tela de início (Safari 16.4+). Push é o segundo argumento
   clássico a favor de nativo.
3. **R-01 do Gate 1** — um desenvolvedor solo. Duas plataformas nativas significam
   dois runtimes, duas contas de loja, dois ciclos de revisão e duas superfícies de
   bug para uma pessoa manter.

Esta decisão foi tomada rodando `mobile-platform-strategy` de forma completa, não
herdando a conclusão do PRD.

## Decision Drivers

- Uma pessoa constrói, opera e depura o produto sozinha (R-01, restrição dominante).
- Custo marginal por usuário ≈ zero (RNF-06 / R-05) — loja de aplicativos introduz
  taxa de conta de desenvolvedor e regras de cobrança in-app.
- Offline duro é exigido **só no modo apresentação** (RNF-01), não no produto inteiro
  (F-14 mantém a leitura do Módulo 1 fora do offline obrigatório).
- Distribuição do MVP é por link, não por loja: não há site institucional (F-13) e a
  meta de volume é 200 iniciantes (PRD §3.3) — escala em que a loja não ajuda.
- A porta para nativo não pode ser fechada de forma cara.

## Opções Consideradas

- **A. PWA instalável (Vite + Workbox + IndexedDB)** ✅ escolhida
- **B. Nativo (Swift + Kotlin)**
- **C. Cross-platform (React Native ou Flutter) desde o MVP**
- **D. PWA agora + wrapper Capacitor já no MVP**

## Decision Outcome

Escolhida a opção **A — PWA instalável, sem shell nativo no MVP**, porque as duas
objeções técnicas que normalmente derrubam PWA são endereçáveis sem nativo neste
produto:

- O offline duro é satisfeito por Service Worker + IndexedDB com **esboço
  autocontido** (ADR-006). O conteúdo do cartão não é resolvido por lookup em tempo
  de apresentação: ele já está materializado no dispositivo.
- O push é satisfeito pela Web Push API com chaves VAPID próprias (ADR-010) em
  Chrome/Android e desktop; no iOS, exige PWA instalado — o que vira um requisito de
  UX explícito (convite de instalação antes da primeira apresentação, `UX-SPEC.md`
  §1.7), e não uma falha silenciosa.

**Porta aberta para nativo, ao custo mais baixo**: todo o domínio (corpus, parser de
referência, plano, regras de progresso e conciliação) é escrito como TypeScript puro,
sem dependência de React nem de DOM (ADR-012). O caminho de menor custo para nativo,
quando existir, é um wrapper Capacitor reaproveitando o mesmo app e os mesmos
artefatos de corpus, ganhando APNs e armazenamento sem cap de 7 dias — não uma
reescrita.

### Consequências Positivas

- Um runtime, um build, um deploy, um conjunto de testes para uma pessoa manter.
- Zero taxa de loja, zero ciclo de revisão, correção em produção em minutos.
- Alcance imediato pelo link — coerente com um MVP sem canal de aquisição definido.

### Consequências Negativas

- **iOS impõe o caminho da instalação**: sem "adicionar à tela de início", não há push
  e o armazenamento gravável por script fica sujeito ao cap de 7 dias de inatividade
  do Safari — que é uma ameaça direta a RNF-01. Mitigação em ADR-006 e risco RT-01 do
  `SDD.md` §6.
- Sem presença em loja, o produto perde a descoberta orgânica da App Store/Play — o
  que agrava Q-07 (canal de aquisição), já em aberto no `PRD.md`.
- O teto de desempenho é menor que o de nativo. Aceitável: o produto renderiza texto,
  não vídeo nem 3D.

## Prós e Contras das Opções

### A. PWA instalável ✅ Escolhida

- ✅ Um runtime, uma linguagem, um deploy — casa com R-01.
- ✅ Custo marginal por usuário ≈ zero; nenhuma taxa fixa de loja.
- ✅ Offline e push atingíveis com API padrão de navegador.
- ❌ Push no iOS só com PWA instalado.
- ❌ Armazenamento local sujeito a política de expurgo do navegador.

### B. Nativo (Swift + Kotlin)

- ✅ Offline e push sem ressalva; melhor integração com o SO.
- ❌ Dois códigos-base para um dev solo — inviável dentro de R-01.
- ❌ Ciclo de revisão de loja atrasa correção do defeito de severidade máxima (M-04).

### C. Cross-platform (React Native / Flutter)

- ✅ Um código-base para duas lojas; offline e push nativos.
- ❌ Ainda exige contas de loja, ciclos de revisão e build nativo — sobrecarga de
  operação que R-01 não comporta.
- ❌ Não elimina a necessidade da versão web (a vitrine sem login, RF-08, é web por
  natureza) — seriam **dois** clientes, não um.

### D. PWA + Capacitor já no MVP

- ✅ Resolve push e armazenamento no iOS desde o dia 1.
- ❌ Traz ciclo de loja e build nativo para dentro do MVP sem que a base de usuários
  justifique — antecipa custo operacional para ganhar garantia que a instalação do
  PWA já dá.
- ➡️ Mantida como **evolução planejada**, não descartada.

## Links

- `PRD.md` §1.4, §4.5 (F-12), `PRD-TECNICO.md` RNF-01, RNF-07, E-03
- Relacionado: [ADR-006](006-garantir-o-offline-duro-por-esboco-autocontido.md),
  [ADR-010](010-entregar-o-lembrete-por-web-push-com-chaves-proprias.md),
  [ADR-012](012-separar-fase-1-e-fase-2-em-modulos-com-dependencia-unidirecional.md)
