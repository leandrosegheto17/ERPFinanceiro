# ADR-008: Usar Supabase como backend gerenciado e Cloudflare Pages como entrega estática

- **Data**: 2026-09-07
- **Status**: Proposed — vira `Accepted` na aprovação do conjunto SDD.md + UX-SPEC.md pelo usuário (orquestrador)
- **Decisores**: coordenador (chapéu Software Architect)
- **Tags**: stack, infraestrutura, custo, lgpd, vendor

## Contexto e Problema

Depois de ADR-002, o servidor deste produto tem escopo pequeno e bem delimitado:
identidade (E-04), persistência e sincronização por titular (E-05), hospedagem/entrega
do PWA (E-06) e coleta de telemetria própria (E-07). Nenhuma lógica de negócio.

Sobre isso incidem duas restrições que eliminam categorias inteiras de solução:

- **RNF-06 / R-05** — nenhuma dependência cujo custo cresça por usuário ativo. Isso
  descarta a maior parte dos serviços de autenticação por MAU, de analytics por evento
  e de banco por requisição, na faixa em que o produto pretende crescer.
- **R-01** — uma pessoa opera isso. Construir autenticação com verificação de e-mail,
  recuperação de acesso, rotação de refresh token e isolamento por linha "na mão"
  custa semanas e produz exatamente a classe de bug que um dev solo não tem como
  auditar sozinho — num produto que trata **dado pessoal sensível** (RNF-05).

## Decision Drivers

- Custo total ≈ zero na faixa do MVP (200–2.000 usuários), com teto conhecido acima
  dela.
- Autenticação e isolamento por titular prontos e auditáveis, não escritos do zero.
- Portabilidade real: dado pessoal sensível não pode ficar preso a um formato
  proprietário.
- Banda de saída do corpus não pode virar custo variável.

## Opções Consideradas

- **A. Supabase (Postgres + GoTrue + PostgREST + RLS + Edge Functions + pg_cron) +
  Cloudflare Pages para o estático** ✅ escolhida
- **B. Cloudflare Workers + D1 + autenticação própria (tudo num só fornecedor)**
- **C. Firebase (Auth + Firestore + Hosting)**
- **D. VPS própria com Postgres e API em Node**

## Decision Outcome

Escolhida a opção **A**, com divisão deliberada de papéis:

- **Cloudflare Pages** serve o PWA e **todos os artefatos estáticos de corpus e
  conteúdo** (ADR-003, ADR-004). Motivo determinante: banda de saída não medida no
  plano gratuito. O corpus é o maior volume de bytes do produto e é justamente o que
  escalaria com adoção — colocá-lo num CDN sem cobrança de banda **remove** a única
  fonte plausível de custo variável restante depois do corte do áudio.
- **Supabase** cobre identidade, persistência por titular, telemetria própria e as
  duas rotinas agendadas (envio de lembrete e execução de exclusões). Postgres com
  **Row Level Security** implementa RNF-09 de forma declarativa — a política de
  isolamento fica no banco, não espalhada em código de aplicação, que é a diferença
  entre auditável e torcer para não ter esquecido um endpoint.

**Contenção de custo (exigida por RNF-06)**, declarada aqui como parte da decisão:

| Recurso | Plano gratuito | Consumo estimado no MVP | Gatilho de ação |
|---|---|---|---|
| Usuários ativos mensais (Auth) | 50.000 | 200–2.000 | Reavaliar em 25.000 MAU |
| Banco Postgres | 500 MB | < 50 MB com 2.000 usuários (dado é texto curto) | Reavaliar em 300 MB |
| Banda de API | 5 GB/mês | Só metadados de sincronização; corpus não passa por aqui | Reavaliar em 3 GB/mês |
| Banda de conteúdo estático | ilimitada (Cloudflare Pages) | ~2 MB por usuário na vida | — |
| E-mail transacional | 3.000/mês (SMTP externo) | Só recuperação e confirmação de exclusão | Nunca usar para lembrete diário (ADR-010) |

Acima desses gatilhos o custo é um plano **fixo** (US$ 25/mês), não uma tarifa por
usuário — e a faixa em que a cobrança do provedor volta a ser por MAU (100.000+) é
duas ordens de grandeza acima da meta declarada de 200 iniciantes. O compromisso desta
decisão é: **atingido qualquer gatilho da tabela, o assunto volta ao Gestor antes de
crescer**, conforme R-05.

**Contenção de lock-in**: o schema é SQL padrão; nenhuma extensão proprietária além de
RLS (que é Postgres nativo) e `pg_cron`; nenhuma lógica de negócio em Edge Function
(ADR-002) — as duas que existem são operacionais e substituíveis por um cron em
qualquer lugar. GoTrue é software livre e auto-hospedável. `pg_dump` semanal
versionado é backup independente do fornecedor. Sair do Supabase é migrar um banco
Postgres e trocar o cliente de autenticação, não reescrever o produto.

### Consequências Positivas

- Autenticação completa e recuperação de acesso sem código próprio — semanas
  economizadas no caminho crítico de um dev solo.
- RNF-09 implementado por política declarativa no banco, testável com SQL.
- Custo real de operação do MVP: **zero**, com teto conhecido e gatilhos declarados.
- Duas ferramentas, um único runtime de backend (Deno, só para as rotinas agendadas).

### Consequências Negativas

- **Dois fornecedores** em vez de um: dois painéis, dois modos de falha, duas
  configurações de domínio. Custo de operação aceito em troca da banda ilimitada.
- Dependência de um provedor gerenciado para dado pessoal **sensível** — exige
  contrato/DPA e verificação de região de hospedagem, ponto que fica registrado para o
  Validador (chapéu DevSecOps) e que G-03 (ausência de papel jurídico) não cobre.
- Projeto Supabase no plano gratuito pode ser pausado por inatividade prolongada —
  irrelevante com usuários reais, relevante durante o desenvolvimento; mitigado por um
  ping agendado.
- PostgREST expõe o schema como API: qualquer tabela sem RLS habilitada é um vazamento
  imediato. Vira regra inegociável de `GUARDRAILS.md` e item obrigatório da revisão do
  Validador.

## Links

- `PRD-TECNICO.md` RNF-05, RNF-06, RNF-09, RN-11, E-04, E-05, E-06, E-07;
  `CTO-REVIEW.md` R-01, R-05
- Relacionado: [ADR-002](002-concentrar-a-logica-de-negocio-no-cliente-com-servidor-de-sincronizacao.md),
  [ADR-009](009-instrumentar-com-telemetria-first-party-em-dois-niveis.md),
  [ADR-010](010-entregar-o-lembrete-por-web-push-com-chaves-proprias.md)
