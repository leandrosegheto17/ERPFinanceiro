# ADR-011: Adotar React, TypeScript e Vite com Workbox, Dexie e tokens de design em CSS custom properties

- **Data**: 2026-09-07
- **Status**: Proposed — vira `Accepted` na aprovação do conjunto SDD.md + UX-SPEC.md pelo usuário (orquestrador)
- **Decisores**: coordenador (chapéu Software Architect)
- **Tags**: stack, frontend, acessibilidade, desempenho

## Contexto e Problema

Definidas a plataforma (ADR-001) e a divisão cliente/servidor (ADR-002), falta a stack
de interface. Ela é decidida por quatro exigências concretas, não por preferência:

- **RNF-02** — texto legível em ≤ 2,5 s (LCP) em 4 Mbps num Android de gama média.
- **RNF-04** — WCAG 2.2 AA em toda tela, 200% de ampliação sem perda de função, e
  contraste ≥ 7:1 no modo apresentação. Depois do corte do áudio (F-03), a semântica
  para leitor de tela **é** o mecanismo de acessibilidade auditiva do MVP: não é um
  detalhe de implementação, é um requisito de produto.
- **RNF-01** — o modo apresentação precisa renderizar o primeiro cartão em ≤ 1 s a
  partir de um registro local, sem rede.
- **R-01** — uma pessoa mantém tudo; e o domínio precisa continuar reutilizável se um
  dia houver um shell nativo (ADR-001).

## Decision Drivers

- Menor superfície de dependência possível, porque cada dependência é manutenção.
- Acessibilidade de componentes de sobreposição (diálogo, popover, toast) não pode ser
  escrita à mão — é a fonte clássica de violação de WCAG em projeto solo.
- Tema escuro com contraste verificável precisa de tokens inspecionáveis, não de
  classes utilitárias espalhadas.
- Domínio sem dependência de framework.

## Opções Consideradas

- **A. React + TypeScript + Vite (SPA com rotas pré-renderizadas) + Workbox +
  Dexie + Radix Primitives + CSS Modules com custom properties** ✅ escolhida
- **B. Next.js com renderização no servidor**
- **C. SvelteKit ou Astro**
- **D. React + Tailwind CSS como sistema de estilo**

## Decision Outcome

Escolhida a opção **A**, peça a peça:

- **TypeScript em todo lugar** — cliente, rotinas agendadas (Deno) e CLIs de
  importação/validação. Uma linguagem para uma pessoa; e o domínio compilado é
  compartilhado literalmente entre os três, sem reescrita.
- **React + Vite, SPA com pré-renderização estática das rotas públicas**. Vite dá
  build rápido e code-splitting por rota sem configuração — o que permite manter o
  Módulo 2 fora do bundle da Fase 1 (ADR-012). As rotas públicas (vitrine, dia 1 do
  plano, leitura livre) são pré-renderizadas em build para HTML estático: o primeiro
  paint não espera JavaScript, que é como RNF-02 é atingido sem servidor de
  renderização. O versículo do dia (CA-08.3) é escolhido no cliente por função
  determinística da data sobre uma tabela pré-computada — determinístico, igual para
  todos, e sem chamada de rede.
- **Workbox (via `vite-plugin-pwa`)** — precache do app shell, do índice do corpus e
  do bundle de conteúdo; runtime caching por capítulo lido; e a garantia estrutural de
  que **nenhuma** rota do modo apresentação passa pela rede.
- **Dexie sobre IndexedDB** — a store local precisa de versionamento e migração de
  schema desde o dia 1 (outbox, snapshots, corpus). Escrever isso sobre `idb` cru é
  economia falsa para um dev solo.
- **Radix Primitives** apenas para diálogo, popover, alternador e toast — componentes
  cujo comportamento acessível (armadilha de foco, `aria-modal`, restauração de foco,
  anúncio em região viva) é a parte difícil de WCAG. Todo o resto é HTML semântico
  nativo. Nenhum kit visual: nenhum componente pronto casaria com os tokens de
  contraste do modo apresentação de qualquer forma.
- **CSS Modules + tokens em custom properties**. Os tokens de cor, tipografia e
  espaçamento vivem num arquivo único, com o valor de contraste calculado anotado ao
  lado de cada par de cores. Isso torna o ≥ 7:1 do modo apresentação **verificável por
  teste automatizado sobre os tokens**, e não uma inspeção visual. Tailwind foi
  descartado por isto: contraste vira uma propriedade de combinações de classes
  espalhadas pelo JSX, difícil de auditar; e o modo apresentação tem um tema próprio,
  não uma variação de escala utilitária.
- **Vitest** (unidade — parser, validador de conteúdo, conciliação, tokens) e
  **Playwright** (e2e — incluindo o teste inegociável: modo apresentação com rede
  desligada e cache de corpus limpo, medindo o tempo até o primeiro cartão).

**Orçamento de desempenho, fixado aqui e verificado em CI**: JS crítico da rota de
leitura ≤ 150 KB comprimido; CSS crítico embutido; nenhuma fonte web bloqueante
(pilha de fontes do sistema); imagem só de ícone, em SVG.

### Consequências Positivas

- Uma linguagem, um build, um comando de teste; nenhum servidor de renderização para
  operar.
- Acessibilidade dos pontos difíceis vem de biblioteca testada, não de código próprio.
- Contraste vira teste, e não opinião.
- Domínio em TypeScript puro segue portátil para um shell nativo futuro.

### Consequências Negativas

- **Sem renderização no servidor**, o conteúdo dinâmico não é indexável por buscador
  além do que for pré-renderizado. Com o site institucional fora do escopo (F-13) e a
  aquisição já em aberto (Q-07), isso empobrece a única superfície pública do produto.
  Aceito nesta rodada; se a aquisição orgânica virar prioridade, é o primeiro item a
  reabrir — e migrar rotas públicas para geração estática mais rica é caminho barato,
  enquanto adotar SSR completo não é.
- React é mais pesado que Svelte/Preact no orçamento de bytes. Compensado por
  code-splitting e pré-renderização; escolhido por ecossistema de acessibilidade,
  volume de material de apoio e continuidade com um eventual React Native.
- Dexie e Radix são duas dependências a acompanhar em segurança e atualização.
- CSS Modules exige disciplina manual de nomenclatura que um framework utilitário
  dispensaria.

## Links

- `PRD-TECNICO.md` RNF-01, RNF-02, RNF-04, RNF-07, CA-08.3, CA-14.6
- Relacionado: [ADR-001](001-adotar-pwa-instalavel-como-unica-plataforma-cliente-do-mvp.md),
  [ADR-006](006-garantir-o-offline-duro-por-esboco-autocontido.md),
  [ADR-012](012-separar-fase-1-e-fase-2-em-modulos-com-dependencia-unidirecional.md)
