import { createClient } from "@supabase/supabase-js";
import { expect, test } from "@playwright/test";

import type { AuthGateway } from "../src/features/identity/auth-gateway";
import { confirmSignUp } from "../src/features/identity/confirm-signup";
import { signUpWithEmailPassword } from "../src/features/identity/sign-up";

/**
 * TASK-035 (Lote 6 — Identidade): e2e do mecanismo de cadastro
 * e-mail+senha com verificação (SDD.md §7.1). Testa a função implementada
 * diretamente (não a tela — T-08 com o `BlocoConsentimento` é TASK-036,
 * ainda não implementada), contra o Supabase local real: cria uma conta,
 * confirma que o e-mail de verificação foi "enviado" (capturado no
 * Mailpit/Inbucket local, nunca envio real — "verificação mock" do critério
 * de aceite), completa o fluxo de verificação e confirma que uma linha em
 * `profile` foi criada para o novo titular (TASK-035, gatilho de banco
 * `on_auth_user_created`, `supabase/migrations/20260908190000_profile_on_signup_trigger.sql`).
 *
 * Credenciais: mesmas chaves de demonstração fixas e públicas do Supabase
 * CLI para ambiente local (default do próprio `supabase start`, documentadas
 * em `docs/ci-secrets.md` §3 como "não são segredo") — mesmo padrão já
 * usado por `tests/db/*.test.mjs`. Nunca a `anon key` de produção.
 *
 * Nota sobre o nome do serviço: `supabase/config.toml` e `docs/ci-secrets.md`
 * chamam a porta 54424 de "Inbucket" (nome histórico do produto), mas a
 * versão atual da CLI do Supabase serve **Mailpit** nessa porta — mesmo
 * papel (captura local de e-mail de teste, com API HTTP própria), API
 * diferente da do Inbucket clássico. Este teste usa a API do Mailpit
 * (`/api/v1/...`), confirmada contra o Supabase local real desta máquina.
 */

const SUPABASE_URL = process.env.SUPABASE_URL ?? "http://127.0.0.1:54421";
const ANON_KEY =
  process.env.SUPABASE_ANON_KEY ??
  "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZS1kZW1vIiwicm9sZSI6ImFub24iLCJleHAiOjE5ODM4MTI5OTZ9.CRXP1A7WOeoJeXxjNni43kdQwgnWNReilDMblYTn_I0";
const SERVICE_ROLE_KEY =
  process.env.SUPABASE_SERVICE_ROLE_KEY ??
  "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZS1kZW1vIiwicm9sZSI6InNlcnZpY2Vfcm9sZSIsImV4cCI6MTk4MzgxMjk5Nn0.EGIM96RAZx35lJzdJsyH-qQwv8Hdp7fsn3W0YpN81IU";
const MAILPIT_URL = process.env.SUPABASE_MAILPIT_URL ?? "http://127.0.0.1:54424";

const AUTH_URL = `${SUPABASE_URL}/auth/v1`;
const REST_URL = `${SUPABASE_URL}/rest/v1`;

async function isLocalSupabaseUp(): Promise<boolean> {
  try {
    const res = await fetch(`${AUTH_URL}/health`, { headers: { apikey: ANON_KEY } });
    return res.ok;
  } catch {
    return false;
  }
}

function uniqueEmail(): string {
  return `signup-${Date.now()}-${Math.random().toString(36).slice(2)}@example.test`;
}

interface MailpitMessageSummary {
  readonly ID: string;
}

interface MailpitSearchResponse {
  readonly messages: readonly MailpitMessageSummary[];
}

interface MailpitMessage {
  readonly Subject: string;
  readonly Text: string;
}

/** Espera (com retry curto) a mensagem de verificação chegar ao Mailpit local. */
async function waitForConfirmationEmail(email: string): Promise<MailpitMessage> {
  const query = encodeURIComponent(`to:${email}`);
  for (let attempt = 0; attempt < 20; attempt += 1) {
    const searchRes = await fetch(`${MAILPIT_URL}/api/v1/search?query=${query}`);
    if (searchRes.ok) {
      const search = (await searchRes.json()) as MailpitSearchResponse;
      if (search.messages.length > 0) {
        const messageRes = await fetch(`${MAILPIT_URL}/api/v1/message/${search.messages[0].ID}`);
        return (await messageRes.json()) as MailpitMessage;
      }
    }
    await new Promise((resolve) => setTimeout(resolve, 250));
  }
  throw new Error(`Nenhum e-mail de verificação capturado no Mailpit local para ${email}`);
}

/** Extrai o `token` (tratado como `token_hash` por `auth.verifyOtp`) do link `.../auth/v1/verify?...`. */
function extractTokenHash(emailBody: string): string {
  const match = /[?&]token=([a-f0-9]+)&type=signup/.exec(emailBody);
  if (!match) {
    throw new Error(
      `Link de verificação não encontrado no corpo do e-mail capturado: ${emailBody.slice(0, 300)}`,
    );
  }
  return match[1];
}

async function deleteUser(userId: string): Promise<void> {
  await fetch(`${AUTH_URL}/admin/users/${userId}`, {
    method: "DELETE",
    headers: { apikey: SERVICE_ROLE_KEY, Authorization: `Bearer ${SERVICE_ROLE_KEY}` },
  });
}

async function fetchOwnProfile(accessToken: string, userId: string) {
  const res = await fetch(`${REST_URL}/profile?user_id=eq.${userId}`, {
    headers: { apikey: ANON_KEY, Authorization: `Bearer ${accessToken}` },
  });
  return { status: res.status, ok: res.ok, body: (await res.json()) as unknown[] };
}

test.describe("Cadastro e-mail+senha com verificação (TASK-035)", () => {
  test("cria conta, recebe verificação mock (Mailpit local) e ganha uma linha em profile", async () => {
    test.skip(
      !(await isLocalSupabaseUp()),
      "Supabase local (supabase start) não está respondendo — teste pulado; ver docs/ci-secrets.md §3",
    );

    // O gateway usa só a anon key pública (DI-09/GUARDRAILS G-10) — nunca a
    // service role, que só entra aqui para a limpeza (delete) ao final.
    const gateway: AuthGateway = createClient(SUPABASE_URL, ANON_KEY).auth;

    const email = uniqueEmail();
    const password = "senha-teste-e2e-9f8e7d6c!";

    const signUpResult = await signUpWithEmailPassword(gateway, { email, password });
    expect(signUpResult.status).toBe("confirmation-required");
    if (signUpResult.status !== "confirmation-required") {
      throw new Error(`signUp inesperado: ${JSON.stringify(signUpResult)}`);
    }
    const userId = signUpResult.userId;

    try {
      // Critério de aceite: "recebe verificação mock" — confirma que o
      // Supabase local de fato capturou um e-mail de verificação (nunca
      // enviado de verdade) para o endereço cadastrado.
      const email_ = await waitForConfirmationEmail(email);
      expect(email_.Subject.toLowerCase()).toContain("confirm");

      const tokenHash = extractTokenHash(email_.Text);
      const confirmResult = await confirmSignUp(gateway, { tokenHash });
      expect(confirmResult.status).toBe("confirmed");
      if (confirmResult.status !== "confirmed") {
        throw new Error(`confirmSignUp inesperado: ${JSON.stringify(confirmResult)}`);
      }
      expect(confirmResult.userId).toBe(userId);

      // Prova que o gatilho de banco (TASK-035) criou a linha de `profile`
      // e que a RLS de TASK-027 permite ao próprio titular lê-la.
      const profile = await fetchOwnProfile(confirmResult.accessToken, userId);
      expect(profile.ok).toBe(true);
      expect(profile.body).toHaveLength(1);
    } finally {
      await deleteUser(userId);
    }
  });
});
