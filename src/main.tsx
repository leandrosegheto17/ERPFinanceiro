import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { App } from "./features/shell/App";

const rootElement = document.getElementById("root");
if (!rootElement) {
  throw new Error("Elemento #root não encontrado no index.html");
}

createRoot(rootElement).render(
  <StrictMode>
    <App />
  </StrictMode>,
);
