// TASK-034 (Lote 5 — Backend: Schema e Segurança): confirma que os headers
// de segurança são de fato aplicados no artefato de deploy, não apenas em
// memória. Roda um build real via API programática do Vite (mesmo
// `vite.config.ts` de `npm run build`, sem duplicar config — mesmo padrão de
// `src/tools/pwa/pwa-manifest.test.ts`, TASK-003) e confirma que o plugin
// `securityHeadersPlugin` escreve `_headers` (formato Cloudflare Pages, ver
// ADR-008/E-06) no diretório de saída, com o conteúdo exato esperado.
import { describe, expect, it } from "vitest";
import { build } from "vite";
import { existsSync, mkdtempSync, readFileSync, rmSync } from "node:fs";
import { tmpdir } from "node:os";
import { join, resolve } from "node:path";
import { buildHeadersFileContent } from "./securityHeaders";

const PROJECT_ROOT = resolve(__dirname, "../../../");
const VITE_CONFIG = join(PROJECT_ROOT, "vite.config.ts");

describe("TASK-034: build de produção emite `_headers` para o CDN", () => {
  it(
    "dist/_headers existe e contém todos os headers de segurança exigidos",
    async () => {
      const outDir = mkdtempSync(join(tmpdir(), "security-headers-build-test-"));
      try {
        await build({
          root: PROJECT_ROOT,
          configFile: VITE_CONFIG,
          logLevel: "silent",
          build: {
            outDir,
            emptyOutDir: true,
          },
        });

        const headersPath = join(outDir, "_headers");
        expect(existsSync(headersPath)).toBe(true);

        const content = readFileSync(headersPath, "utf-8");
        expect(content).toBe(buildHeadersFileContent());

        // Confirma as 5 chaves literalmente presentes no arquivo publicado
        // (não só que a função geradora concorda consigo mesma).
        for (const header of [
          "Content-Security-Policy",
          "Strict-Transport-Security",
          "Referrer-Policy",
          "Permissions-Policy",
          "X-Content-Type-Options",
        ]) {
          expect(content).toContain(header);
        }
      } finally {
        rmSync(outDir, { recursive: true, force: true });
      }
    },
    30_000,
  );
});
