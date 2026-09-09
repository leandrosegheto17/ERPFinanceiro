// TASK-034 (Lote 5 — Backend: Schema e Segurança): headers de segurança
// exigidos pelo SDD.md §7.5 ("Superfície de exposição" — linha "PWA e
// artefatos estáticos (CDN)") e GUARDRAILS.md G-17/G-18.
//
// Fonte única de verdade dos headers: usada tanto para aplicar em runtime
// (dev/preview, via `securityHeadersPlugin` em `vite.config.ts`) quanto para
// gerar o artefato de deploy (`_headers`, formato nativo do Cloudflare
// Pages — ver SDD.md ADR-008/E-06, hospedagem escolhida para a entrega
// estática). Um único objeto evita divergência entre o que é testado
// localmente e o que de fato vai para o CDN.
//
// Decisão de portabilidade (hosting/CDN definitivo ainda não provisionado,
// ver TASK.md TASK-034): Cloudflare Pages lê automaticamente um arquivo
// `_headers` na raiz do diretório publicado (`dist/_headers` após o build,
// já que tudo em `public/*` e o que este módulo escreve em `writeBundle` vai
// para lá). É o mecanismo mais portável e "sem servidor" possível — não
// depende de Edge Function/Worker adicional, funciona com hospedagem
// estática pura, e é o formato do provedor decidido em ADR-008. Caso o
// provedor de hospedagem mude no futuro, só este módulo + o hook
// `writeBundle` do plugin precisam mudar de formato (ex.: `vercel.json`).
//
// Nota sobre `connect-src` (domínio do backend): o projeto/domínio Supabase
// definitivo ainda não está provisionado neste ponto do desenvolvimento
// (roda local via `supabase start`, ver TASK-005). Usamos o padrão de
// domínio de qualquer projeto Supabase hospedado (`https://*.supabase.co`)
// como valor portável e correto por construção — nenhum destino fora do
// backend é permitido, cumprindo G-17 (nenhum destino de terceiro) mesmo
// sem saber o `<project-ref>` exato ainda. Quando o projeto for
// provisionado, trocar o wildcard pelo domínio exato do projeto é uma
// tarefa de configuração (não de código) e deve ser feita antes do go-live
// de produção — deixado registrado aqui para não se perder.
export const SUPABASE_CONNECT_SRC = "https://*.supabase.co";

/**
 * Diretivas de Content-Security-Policy exigidas por SDD §7.5:
 * - `default-src 'self'`: nenhuma origem de terceiro por padrão (G-17).
 * - `connect-src`: `'self'` + domínio do backend, e nada mais.
 * - `script-src 'self'` SEM `'unsafe-inline'`: este é o risco real que CSP
 *   mitiga (XSS via injeção de script), e continua bloqueado sem exceção.
 * - `style-src 'self' 'unsafe-inline'`: correção pós-revisão inline de
 *   TASK-034. Os 5 primitivos do Design System (Lote 4 —
 *   `src/design-system/primitives/{Botao,CampoDeTexto,CampoDeSenha,
 *   SeletorDeHora,Alternador}.tsx`) usam a prop `style={{...}}` do React para
 *   aplicar `TOUCH_TARGET_MIN_STYLE` (alvo de toque mínimo, DI-08) e outros
 *   valores dinâmicos de token — isso vira um atributo `style=""` inline no
 *   DOM. Sob `style-src 'self'` estrito (sem `'unsafe-inline'`, sem nonce,
 *   sem hash), o navegador bloqueia esse atributo, quebrando o próprio DI-08
 *   assim que uma tela real (Lote 6+) consumir esses primitivos. O projeto
 *   não tem CSS-in-JS com suporte a nonce configurado, então a forma mais
 *   simples e correta é permitir `'unsafe-inline'` só em `style-src`: é uma
 *   concessão comum e de risco muito menor que em `script-src` (não permite
 *   execução de código, só aplicação de estilo).
 * - `frame-ancestors 'none'`: o app nunca é embutido em iframe de terceiro.
 * - `worker-src`/`manifest-src`: explícitos (mesmo caindo em `default-src`
 *   por herança) para deixar claro que o Service Worker (Workbox, DI-15) e o
 *   Web App Manifest (TASK-003) são same-origin por decisão, não por omissão.
 * - `object-src 'none'`, `base-uri 'self'`, `form-action 'self'`: reforço de
 *   segurança padrão de mercado, sem contradizer o mínimo exigido pelo SDD.
 */
export const CONTENT_SECURITY_POLICY = [
  "default-src 'self'",
  `connect-src 'self' ${SUPABASE_CONNECT_SRC}`,
  "script-src 'self'",
  "style-src 'self' 'unsafe-inline'",
  "img-src 'self' data:",
  "font-src 'self'",
  "manifest-src 'self'",
  "worker-src 'self'",
  "frame-ancestors 'none'",
  "base-uri 'self'",
  "form-action 'self'",
  "object-src 'none'",
].join("; ");

/**
 * HSTS: TLS 1.2+ obrigatório + redirecionamento permanente para HTTPS
 * (SDD §7.3/§7.5). `max-age` de 1 ano (valor padrão de mercado para HSTS
 * "de verdade", suficiente para elegibilidade de preload) +
 * `includeSubDomains` + `preload`.
 */
export const STRICT_TRANSPORT_SECURITY =
  "max-age=31536000; includeSubDomains; preload";

/** SDD §7.5: `Referrer-Policy: no-referrer`, sem exceção. */
export const REFERRER_POLICY = "no-referrer";

/**
 * SDD §7.5: nega câmera, microfone e geolocalização — nenhuma é usada pelo
 * produto (confirmado em PRD-TECNICO/SDD; nenhuma feature usa mídia captada
 * ou localização).
 */
export const PERMISSIONS_POLICY = "camera=(), microphone=(), geolocation=()";

/**
 * Reforço padrão de mercado (não exigido literalmente pelo SDD §7.5, mas
 * não contradiz o mínimo e evita sniffing de MIME em resposta estática).
 */
export const X_CONTENT_TYPE_OPTIONS = "nosniff";

/**
 * Conjunto completo de headers de segurança exigidos por TASK-034. Ordem
 * estável (usada tanto na geração do `_headers` quanto nos testes).
 */
export const SECURITY_HEADERS: ReadonlyArray<readonly [string, string]> = [
  ["Content-Security-Policy", CONTENT_SECURITY_POLICY],
  ["Strict-Transport-Security", STRICT_TRANSPORT_SECURITY],
  ["Referrer-Policy", REFERRER_POLICY],
  ["Permissions-Policy", PERMISSIONS_POLICY],
  ["X-Content-Type-Options", X_CONTENT_TYPE_OPTIONS],
];

/** Aplica todos os headers de segurança a uma resposta HTTP Node (Vite preview/produção). */
export function applySecurityHeaders(res: {
  setHeader: (name: string, value: string) => void;
}): void {
  for (const [name, value] of SECURITY_HEADERS) {
    res.setHeader(name, value);
  }
}

/**
 * Correção pós-fechamento de TASK-034 (regressão encontrada durante
 * TASK-035/Lote 6): headers aplicados pelo `vite dev` (HMR ativo), sem CSP.
 *
 * Causa raiz: em modo dev, `@vitejs/plugin-react` injeta um
 * `<script type="module">` inline (preamble do React Fast Refresh) na
 * página. Sob `Content-Security-Policy: script-src 'self'` (sem
 * `'unsafe-inline'`/`'unsafe-eval'` — o mínimo exigido por SDD §7.5 para
 * produção), o navegador bloqueia esse script inline e o React nunca monta,
 * quebrando o app inteiro em dev (e, por consequência, todo o e2e Playwright,
 * que roda contra `vite dev` — ver `playwright.config.ts`).
 *
 * Decisão: em dev, não aplicar CSP nenhuma — o `vite dev` nunca é o ambiente
 * de ameaça real que a CSP protege (não é o que é publicado/exposto a
 * usuário final), e isso evita a complexidade de manter duas CSPs "quase
 * iguais" sincronizadas (uma restrita para produção, outra relaxada para
 * dev) que divergiriam silenciosamente com o tempo. Os demais headers (HSTS,
 * Permissions-Policy, Referrer-Policy, X-Content-Type-Options) não têm esse
 * problema — nenhum deles bloqueia o preamble do HMR — e continuam
 * aplicados normalmente em dev, preservando o valor original de já testar
 * via HTTP real contra `localhost` (ver cabeçalho do arquivo).
 *
 * `configurePreviewServer` (que serve o build real, sem HMR/preamble) e o
 * `writeBundle` (arquivo `_headers` do build de produção) continuam usando
 * `applySecurityHeaders` (conjunto completo, com CSP) — nenhuma regressão no
 * critério de aceite original de TASK-034 para produção/preview.
 */
export function applyDevSecurityHeaders(res: {
  setHeader: (name: string, value: string) => void;
}): void {
  for (const [name, value] of SECURITY_HEADERS) {
    if (name === "Content-Security-Policy") continue;
    res.setHeader(name, value);
  }
}

/**
 * Gera o conteúdo do arquivo `_headers` no formato do Cloudflare Pages:
 * um bloco de regra `/*` (todas as rotas) seguido de uma linha por header,
 * indentada com 2 espaços (sintaxe exigida pelo provedor).
 */
export function buildHeadersFileContent(): string {
  const lines = SECURITY_HEADERS.map(([name, value]) => `  ${name}: ${value}`);
  return ["/*", ...lines, ""].join("\n");
}
