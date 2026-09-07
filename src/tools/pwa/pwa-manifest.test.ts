// TASK-003 (Lote 0 — Fundação e CI): `vite-plugin-pwa` + Workbox, shell básico
// instalável. Critério de aceite: "App instala localmente; manifest.json
// válido." Este teste roda um build real via API programática do Vite
// (mesmo `vite.config.ts` usado por `npm run build`, sem duplicar
// configuração) e valida:
//
//   1. `manifest.json` gerado no output é JSON válido e cumpre os campos
//      mínimos de instalabilidade (name, short_name, start_url, display,
//      ícones 192x192 e 512x512 — critério de instalabilidade de um PWA,
//      independente de plataforma).
//   2. O plugin efetivamente gera um Service Worker (`sw.js`) no build —
//      não apenas registra a intenção; sem SW não há PWA instalável.
//
// Escopo explicitamente fora desta tarefa (ver TASK-066/067, Lote 13):
// estratégias de cache por artefato (corpus/conteúdo editorial) e
// `storage.persist()`. Este teste não cobre isso de propósito.
import { describe, expect, it } from "vitest";
import { build } from "vite";
import { existsSync, mkdtempSync, readFileSync, rmSync } from "node:fs";
import { tmpdir } from "node:os";
import { join, resolve } from "node:path";

const PROJECT_ROOT = resolve(__dirname, "../../../");
const VITE_CONFIG = join(PROJECT_ROOT, "vite.config.ts");

describe("TASK-003: vite-plugin-pwa gera shell instalável", () => {
  it(
    "produz manifest.json válido e um service worker no build",
    async () => {
      const outDir = mkdtempSync(join(tmpdir(), "pwa-build-test-"));
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

        // --- 1. manifest.json existe e é JSON válido ---
        const manifestPath = join(outDir, "manifest.json");
        expect(existsSync(manifestPath)).toBe(true);

        const raw = readFileSync(manifestPath, "utf-8");
        let manifest: Record<string, unknown>;
        expect(() => {
          manifest = JSON.parse(raw);
        }).not.toThrow();
        manifest = JSON.parse(raw);

        // Campos mínimos de instalabilidade de um PWA (independente de
        // navegador/plataforma): nome, ponto de entrada, modo de exibição.
        expect(manifest.name).toBe("Estudo Bíblico");
        expect(manifest.short_name).toBeTruthy();
        expect(manifest.start_url).toBe("/");
        expect(["standalone", "fullscreen", "minimal-ui"]).toContain(
          manifest.display,
        );

        const icons = manifest.icons as Array<{
          src: string;
          sizes: string;
          purpose?: string;
        }>;
        expect(Array.isArray(icons)).toBe(true);

        const icon192 = icons.find((icon) => icon.sizes === "192x192");
        const icon512Any = icons.find((icon) => icon.sizes === "512x512");
        expect(icon192, "ícone 192x192 é obrigatório para instalabilidade").toBeTruthy();
        expect(icon512Any, "ícone 512x512 é obrigatório para instalabilidade").toBeTruthy();

        // Cada ícone referenciado no manifest precisa existir de fato no
        // output do build (senão o manifest "válido" é só sintaticamente
        // válido, não funcionalmente instalável).
        for (const icon of icons) {
          expect(existsSync(join(outDir, icon.src))).toBe(true);
        }

        // --- 2. Service Worker foi de fato gerado (não só registrado) ---
        const swPath = join(outDir, "sw.js");
        expect(existsSync(swPath)).toBe(true);
        const swContent = readFileSync(swPath, "utf-8");
        expect(swContent.length).toBeGreaterThan(0);
        // Confirma que o SW gerado é o Workbox (DI-15: Workbox via
        // vite-plugin-pwa), não um arquivo vazio/placeholder.
        expect(swContent).toMatch(/workbox/i);

        // --- link do manifest no shell (pré-requisito de instalabilidade) ---
        const indexHtml = readFileSync(join(outDir, "index.html"), "utf-8");
        expect(indexHtml).toMatch(/<link[^>]+rel="manifest"/);
      } finally {
        rmSync(outDir, { recursive: true, force: true });
      }
    },
    30_000,
  );
});
