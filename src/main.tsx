import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { App } from "./features/shell/App";
import { initTheme } from "./design-tokens/theme";
import "./design-tokens/tokens.css";
// TASK-024: tema exclusivo do Modo Apresentação (AAA). Só produz efeito
// dentro de um elemento com `data-theme="apresentacao"` — nenhuma tela hoje
// seta esse atributo; será aplicado pela rota do Modo Apresentação quando
// ela existir (Lote 15, Fase 2).
import "./design-tokens/presentation-theme.css";

// TASK-023: aplica o tema (claro/escuro) o mais cedo possível no boot, antes
// da primeira renderização, para evitar flash de tema errado.
initTheme({
  target: document.documentElement,
  storage: window.localStorage,
  systemPrefersDark: window.matchMedia("(prefers-color-scheme: dark)").matches,
});

const rootElement = document.getElementById("root");
if (!rootElement) {
  throw new Error("Elemento #root não encontrado no index.html");
}

createRoot(rootElement).render(
  <StrictMode>
    <App />
  </StrictMode>,
);
