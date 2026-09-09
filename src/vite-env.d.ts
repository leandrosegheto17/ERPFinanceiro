/// <reference types="vite/client" />

// TASK-035 (Lote 6 — Identidade): tipagem das variáveis públicas do cliente
// Supabase Auth injetadas pelo Vite (ver .env.example/docs/ci-secrets.md).
// Nunca declarar aqui nenhuma variável de segredo de serviço (DI-09) — só o
// que é seguro aparecer no bundle publicado.
interface ImportMetaEnv {
  readonly VITE_SUPABASE_URL?: string;
  readonly VITE_SUPABASE_ANON_KEY?: string;
}
