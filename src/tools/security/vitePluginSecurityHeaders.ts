// TASK-034 (Lote 5 — Backend: Schema e Segurança): plugin Vite que aplica os
// headers de segurança (CSP, HSTS, Permissions-Policy, Referrer-Policy — ver
// `securityHeaders.ts`) em três momentos:
//
//   1. `vite dev`: middleware que seta todos os headers, EXCETO CSP (ver
//      `applyDevSecurityHeaders` em `securityHeaders.ts` — CSP restrita
//      quebra o preamble inline do React Fast Refresh injetado pelo HMR;
//      correção pós-fechamento de TASK-034, achada durante TASK-035/Lote 6).
//   2. `vite preview`: middleware que seta o conjunto completo (com CSP) —
//      serve o build real, sem HMR, então não tem o problema acima. Permite
//      testar a CSP de produção via HTTP real (fetch contra `localhost`)
//      sem depender do provedor de hospedagem final.
//   3. `vite build`: escreve `_headers` (formato Cloudflare Pages, ver
//      ADR-008/E-06) no diretório de saída, com o conjunto completo (com
//      CSP), para o CDN aplicar os mesmos headers em produção.
//
// Preview e build usam a mesma fonte (`SECURITY_HEADERS`/`applySecurityHeaders`),
// então não há risco de o que é testado localmente divergir do que é
// publicado. Só o dev server difere, deliberadamente (ver acima).
import { mkdirSync, writeFileSync } from "node:fs";
import { join } from "node:path";
import type { Plugin } from "vite";
import {
  applyDevSecurityHeaders,
  applySecurityHeaders,
  buildHeadersFileContent,
} from "./securityHeaders.ts";

export function securityHeadersPlugin(): Plugin {
  return {
    name: "task-034-security-headers",
    // Correção pós-fechamento de TASK-034 (regressão encontrada durante
    // TASK-035/Lote 6): `vite dev` NÃO aplica a CSP restritiva de produção —
    // só `configurePreviewServer`/`writeBundle` aplicam. Ver
    // `applyDevSecurityHeaders` em `securityHeaders.ts` para a causa raiz e a
    // justificativa completa da decisão.
    configureServer(server) {
      server.middlewares.use((_req, res, next) => {
        applyDevSecurityHeaders(res);
        next();
      });
    },
    configurePreviewServer(server) {
      server.middlewares.use((_req, res, next) => {
        applySecurityHeaders(res);
        next();
      });
    },
    writeBundle(options) {
      const outDir = options.dir ?? "dist";
      mkdirSync(outDir, { recursive: true });
      writeFileSync(join(outDir, "_headers"), buildHeadersFileContent(), "utf-8");
    },
  };
}
