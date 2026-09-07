import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import { VitePWA } from "vite-plugin-pwa";

export default defineConfig({
  plugins: [
    react(),
    // TASK-003 (Lote 0): shell básico instalável. Estratégias de cache por
    // artefato (shell/corpus/conteúdo) ficam para TASK-066 (Lote 13) — aqui só
    // o essencial para instalação local e manifest válido (DI-15).
    VitePWA({
      registerType: "autoUpdate",
      manifestFilename: "manifest.json",
      includeAssets: ["icons/icon-192.png", "icons/icon-512.png"],
      manifest: {
        id: "/",
        name: "Estudo Bíblico",
        short_name: "Estudo Bíblico",
        description:
          "App de leitura bíblica guiada e preparo de estudo, instalável e local-first.",
        lang: "pt-BR",
        start_url: "/",
        scope: "/",
        display: "standalone",
        theme_color: "#1e293b",
        background_color: "#ffffff",
        icons: [
          {
            src: "icons/icon-192.png",
            sizes: "192x192",
            type: "image/png",
          },
          {
            src: "icons/icon-512.png",
            sizes: "512x512",
            type: "image/png",
          },
          {
            src: "icons/icon-512-maskable.png",
            sizes: "512x512",
            type: "image/png",
            purpose: "maskable",
          },
        ],
      },
      // Placeholder mínimo: só o app shell (JS/CSS/HTML) do build entra no
      // precache aqui. Cache de corpus/conteúdo editorial (TASK-066) e
      // storage.persist() (TASK-067) são de outras tarefas do Lote 13.
      workbox: {
        globPatterns: ["**/*.{js,css,html}"],
      },
      devOptions: {
        enabled: false,
      },
    }),
  ],
});
