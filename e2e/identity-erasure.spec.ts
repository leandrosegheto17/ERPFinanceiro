import { createClient } from "@supabase/supabase-js";
import { expect, test } from "@playwright/test";

import type { AuthGateway } from "../src/features/identity/auth-gateway";
import { confirmSignUp } from "../src/features/identity/confirm-signup";
import { PALAVRA_DE_CONFIRMACAO_EXCLUSAO } from "../src/features/identity/erasure-confirmation-word";
import type { ErasureRequestGateway } from "../src/features/identity/erasure-request-gateway";
import { getErasureRequestStatus } from "../src/features/identity/get-erasure-request-status";
import { requestAccountErasure } from "../src/features/identity/request-account-erasure";
import { signUpWithEmailPassword } from "../src/features/identity/sign-up";

/**
 * TASK-040 (Lote 6 — Identidade): e2e do fluxo de exclusão de conta (tela
 * T-13, `public.erasure_request`, migration de TASK-032, RN-11) contra o
 * Supabase local real.
 *
 * Mesmo padrão já estabelecido por `identity-signup.spec.ts`/
 * `identity-signin.spec.ts` (TASK-035/037): exercita as funções
 * implementadas diretamente (`requestAccountErasure`/
 * `getErasureRequestStatus`, as mesmas que `BlocoExcluirConta` chama), não
 * uma rota montada no navegador — `src/features/shell/App.tsx` ainda é um
 * placeholder sem roteador (fora do escopo desta tarefa), então não existe
 * ainda uma URL de T-13 para o Playwright navegar. A fricção de digitar
 * "EXCLUIR" (guardrail client-side de `BlocoExcluirConta`, já coberto em
 * unidade por `BlocoExcluirConta.test.tsx`) é simulada aqui checando a
 * mesma constante exportada (`PALAVRA_DE_CONFIRMACAO_EXCLUSAO`) antes de
 * chamar `requestAccountErasure` — exatamente a guarda que o componente usa
 * para habilitar o botão — para o teste também documentar esse contrato.
 *
 * Cobre o critério de aceite central: "e2e cria `erasure_request` com
 * `due_at = +15 dias`" — a partir de `requested_at`, calculado pelo
 * trigger do servidor (`set_erasure_request_due_at`, nunca pelo cliente).
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
  return `erasure-${Date.now()}-${Math.random().toString(36).slice(2)}@example.test`;
}

interface MailpitMessageSummary {
  readonly ID: string;
}
interface MailpitSearchResponse {
  readonly messages: readonly MailpitMessageSummary[];
}
interface MailpitMessage {
  readonly Text: string;
}

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

/** Cria e confirma uma conta real (mesmo caminho de TASK-035), retornando o `access_token` da sessão logada. */
async function createSignedInAccount(
  gateway: AuthGateway,
): Promise<{ userId: string; accessToken: string }> {
  const email = uniqueEmail();
  const password = "senha-teste-e2e-9f8e7d6c!";

  const signUpResult = await signUpWithEmailPassword(gateway, { email, password });
  if (signUpResult.status !== "confirmation-required") {
    throw new Error(`signUp inesperado: ${JSON.stringify(signUpResult)}`);
  }

  const message = await waitForConfirmationEmail(email);
  const tokenHash = extractTokenHash(message.Text);
  const confirmResult = await confirmSignUp(gateway, { tokenHash });
  if (confirmResult.status !== "confirmed") {
    throw new Error(`confirmSignUp inesperado: ${JSON.stringify(confirmResult)}`);
  }

  return { userId: confirmResult.userId, accessToken: confirmResult.accessToken };
}

async function fetchOwnErasureRequest(accessToken: string, userId: string) {
  const res = await fetch(`${REST_URL}/erasure_request?user_id=eq.${userId}`, {
    headers: { apikey: ANON_KEY, Authorization: `Bearer ${accessToken}` },
  });
  return { status: res.status, ok: res.ok, body: (await res.json()) as unknown[] };
}

test.describe("Fluxo de exclusão de conta (TASK-040, T-13)", () => {
  test("login → aciona exclusão digitando EXCLUIR → cria erasure_request com due_at = +15 dias", async () => {
    test.skip(
      !(await isLocalSupabaseUp()),
      "Supabase local (supabase start) não está respondendo — teste pulado; ver docs/ci-secrets.md §3",
    );

    // Gateway só com a anon key pública (DI-09) — mesma configuração real
    // usada pela UI (`createIdentityAuthGateway`, minus o storage Dexie,
    // irrelevante para este teste de servidor).
    const authGateway: AuthGateway = createClient(SUPABASE_URL, ANON_KEY).auth;

    const { userId, accessToken } = await createSignedInAccount(authGateway);

    // Gateway autenticado como o próprio titular logado (`login` acima) —
    // mesma sessão que `BlocoExcluirConta` usaria depois de T-09/TASK-037.
    const erasureGateway: ErasureRequestGateway = createClient(SUPABASE_URL, ANON_KEY, {
      global: { headers: { Authorization: `Bearer ${accessToken}` } },
    });

    try {
      // Estado inicial: nenhum pedido de exclusão ainda (o que
      // `BlocoExcluirConta` consulta ao montar).
      const statusInicial = await getErasureRequestStatus(erasureGateway, userId);
      expect(statusInicial).toEqual({ status: "none" });

      // "Digita EXCLUIR, confirma": a mesma guarda que habilita o botão em
      // `BlocoExcluirConta` — só chama `requestAccountErasure` quando o
      // texto confirmado bate exatamente com a palavra exigida.
      const textoDigitado = PALAVRA_DE_CONFIRMACAO_EXCLUSAO;
      expect(textoDigitado).toBe("EXCLUIR");

      const antesDoEnvio = Date.now();
      const result = await requestAccountErasure(erasureGateway, { userId });
      expect(result.status).toBe("requested");
      if (result.status !== "requested") {
        throw new Error(`requestAccountErasure inesperado: ${JSON.stringify(result)}`);
      }

      // Critério de aceite central: due_at = requested_at + 15 dias.
      const requestedAtMs = new Date(result.requestedAt).getTime();
      const dueAtMs = new Date(result.dueAt).getTime();
      expect(requestedAtMs).toBeGreaterThanOrEqual(antesDoEnvio - 5_000);
      const quinzeDiasMs = 15 * 24 * 60 * 60 * 1000;
      expect(dueAtMs - requestedAtMs).toBe(quinzeDiasMs);

      // Estado visível após confirmar: consulta direta (REST, mesma RLS de
      // TASK-032) confirma a linha gravada e a mesma data-limite.
      const linha = await fetchOwnErasureRequest(accessToken, userId);
      expect(linha.ok).toBe(true);
      expect(linha.body).toHaveLength(1);
      const registro = linha.body[0] as { due_at: string; requested_at: string; user_id: string };
      expect(registro.user_id).toBe(userId);
      expect(new Date(registro.due_at).getTime() - new Date(registro.requested_at).getTime()).toBe(
        quinzeDiasMs,
      );

      // Estado visível também via a mesma função que `BlocoExcluirConta`
      // consulta ao montar/recarregar a página.
      const statusApos = await getErasureRequestStatus(erasureGateway, userId);
      expect(statusApos).toEqual({ status: "requested", requestedAt: result.requestedAt, dueAt: result.dueAt });
    } finally {
      await deleteUser(userId);
    }
  });

  test("guardrail: o cliente não consegue gravar um due_at arbitrário — o servidor sempre recalcula", async () => {
    test.skip(
      !(await isLocalSupabaseUp()),
      "Supabase local (supabase start) não está respondendo — teste pulado; ver docs/ci-secrets.md §3",
    );

    const authGateway: AuthGateway = createClient(SUPABASE_URL, ANON_KEY).auth;
    const { userId, accessToken } = await createSignedInAccount(authGateway);

    try {
      // Tentativa deliberada de enviar um due_at manipulado (100 anos no
      // futuro) diretamente via REST, simulando um cliente malicioso que
      // ignore `requestAccountErasure` e monte o INSERT à mão.
      const dueAtManipulado = new Date(Date.now() + 100 * 365 * 24 * 60 * 60 * 1000).toISOString();
      const res = await fetch(`${REST_URL}/erasure_request`, {
        method: "POST",
        headers: {
          apikey: ANON_KEY,
          Authorization: `Bearer ${accessToken}`,
          "Content-Type": "application/json",
          Prefer: "return=representation",
        },
        body: JSON.stringify({ user_id: userId, due_at: dueAtManipulado }),
      });
      expect(res.ok).toBe(true);
      const body = (await res.json()) as Array<{ due_at: string; requested_at: string }>;
      expect(body).toHaveLength(1);

      // O trigger `set_erasure_request_due_at` sobrescreveu o valor enviado:
      // due_at continua sendo requested_at + 15 dias, nunca o valor manipulado.
      const registro = body[0];
      expect(registro.due_at).not.toBe(dueAtManipulado);
      const quinzeDiasMs = 15 * 24 * 60 * 60 * 1000;
      expect(new Date(registro.due_at).getTime() - new Date(registro.requested_at).getTime()).toBe(
        quinzeDiasMs,
      );
    } finally {
      await deleteUser(userId);
    }
  });
});
