import { createClient } from "@supabase/supabase-js";
import { expect, test } from "@playwright/test";

import type { SignInGateway } from "../src/features/identity/sign-in-gateway";
import { requestPasswordReset } from "../src/features/identity/request-password-reset";
import { signInWithPassword } from "../src/features/identity/sign-in-with-password";

/**
 * TASK-037 (Lote 6 — Identidade): e2e da tela T-09 (Entrar/recuperar
 * acesso) contra o Supabase local real. Mesmo padrão de
 * `identity-signup.spec.ts` (TASK-035): exercita as funções implementadas
 * diretamente, não uma rota montada — `src/features/shell/App.tsx` ainda é
 * um placeholder sem roteador (fora do escopo desta tarefa), então o
 * ponto de integração testável de ponta a ponta é a função que fala com o
 * Supabase local de verdade, igual ao precedente já estabelecido.
 *
 * Cobre os 3 pontos exigidos pelo critério de aceite:
 * 1. Login bem-sucedido (senha correta de conta real, criada e confirmada
 *    neste próprio teste via signUp+confirmSignUp de TASK-035).
 * 2. Recuperação de senha (`resetPasswordForEmail`, e-mail chega ao
 *    Mailpit local).
 * 3. Mensagem de erro idêntica para "e-mail inexistente" e "senha errada
 *    de conta existente" — critério central desta tarefa.
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

async function isLocalSupabaseUp(): Promise<boolean> {
  try {
    const res = await fetch(`${AUTH_URL}/health`, { headers: { apikey: ANON_KEY } });
    return res.ok;
  } catch {
    return false;
  }
}

function uniqueEmail(prefix: string): string {
  return `${prefix}-${Date.now()}-${Math.random().toString(36).slice(2)}@example.test`;
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

async function waitForEmail(email: string, subjectContains: string): Promise<MailpitMessage> {
  const query = encodeURIComponent(`to:${email}`);
  for (let attempt = 0; attempt < 20; attempt += 1) {
    const searchRes = await fetch(`${MAILPIT_URL}/api/v1/search?query=${query}`);
    if (searchRes.ok) {
      const search = (await searchRes.json()) as MailpitSearchResponse;
      const match = search.messages.length > 0 ? search.messages[0] : undefined;
      if (match) {
        const messageRes = await fetch(`${MAILPIT_URL}/api/v1/message/${match.ID}`);
        const message = (await messageRes.json()) as MailpitMessage;
        if (message.Subject.toLowerCase().includes(subjectContains)) {
          return message;
        }
      }
    }
    await new Promise((resolve) => setTimeout(resolve, 250));
  }
  throw new Error(`Nenhum e-mail com assunto contendo "${subjectContains}" capturado para ${email}`);
}

function extractTokenHash(emailBody: string, type: string): string {
  const regex = new RegExp(`[?&]token=([a-f0-9]+)&type=${type}`);
  const match = regex.exec(emailBody);
  if (!match) {
    throw new Error(`Link (${type}) não encontrado no corpo do e-mail: ${emailBody.slice(0, 300)}`);
  }
  return match[1];
}

async function createConfirmedAccount(
  gateway: SignInGateway & { signUp: (input: unknown) => Promise<unknown>; verifyOtp: (input: unknown) => Promise<unknown> },
  email: string,
  password: string,
): Promise<string> {
  const signUpRes = (await gateway.signUp({ email, password })) as {
    data: { user: { id: string } | null };
    error: unknown;
  };
  if (!signUpRes.data.user) {
    throw new Error("signUp não retornou usuário ao preparar a conta de teste");
  }
  const userId = signUpRes.data.user.id;
  const message = await waitForEmail(email, "confirm");
  const tokenHash = extractTokenHash(message.Text, "signup");
  await gateway.verifyOtp({ type: "signup", token_hash: tokenHash });
  return userId;
}

async function deleteUser(userId: string): Promise<void> {
  await fetch(`${AUTH_URL}/admin/users/${userId}`, {
    method: "DELETE",
    headers: { apikey: SERVICE_ROLE_KEY, Authorization: `Bearer ${SERVICE_ROLE_KEY}` },
  });
}

test.describe("Entrar / recuperar acesso (TASK-037, T-09)", () => {
  test("login com senha correta é bem-sucedido", async () => {
    test.skip(
      !(await isLocalSupabaseUp()),
      "Supabase local (supabase start) não está respondendo — teste pulado; ver docs/ci-secrets.md §3",
    );

    const gateway = createClient(SUPABASE_URL, ANON_KEY).auth as unknown as SignInGateway & {
      signUp: (input: unknown) => Promise<unknown>;
      verifyOtp: (input: unknown) => Promise<unknown>;
    };
    const email = uniqueEmail("signin-ok");
    const password = "senha-teste-e2e-9f8e7d6c!";
    const userId = await createConfirmedAccount(gateway, email, password);

    try {
      const result = await signInWithPassword(gateway, { email, password });
      expect(result.status).toBe("signed-in");
      if (result.status === "signed-in") {
        expect(result.userId).toBe(userId);
        expect(result.accessToken.length).toBeGreaterThan(0);
      }
    } finally {
      await deleteUser(userId);
    }
  });

  test("recuperação de senha envia e-mail real via Mailpit local", async () => {
    test.skip(
      !(await isLocalSupabaseUp()),
      "Supabase local (supabase start) não está respondendo — teste pulado; ver docs/ci-secrets.md §3",
    );

    const gateway = createClient(SUPABASE_URL, ANON_KEY).auth as unknown as SignInGateway & {
      signUp: (input: unknown) => Promise<unknown>;
      verifyOtp: (input: unknown) => Promise<unknown>;
    };
    const email = uniqueEmail("signin-recuperar");
    const password = "senha-teste-e2e-9f8e7d6c!";
    const userId = await createConfirmedAccount(gateway, email, password);

    try {
      const result = await requestPasswordReset(gateway, email);
      expect(result.status).toBe("request-sent");

      const message = await waitForEmail(email, "reset your password");
      expect(message.Text.toLowerCase()).toContain("recovery");
    } finally {
      await deleteUser(userId);
    }
  });

  test("critério de aceite: erro de login idêntico para e-mail inexistente e para senha errada de conta real", async () => {
    test.skip(
      !(await isLocalSupabaseUp()),
      "Supabase local (supabase start) não está respondendo — teste pulado; ver docs/ci-secrets.md §3",
    );

    const gateway = createClient(SUPABASE_URL, ANON_KEY).auth as unknown as SignInGateway & {
      signUp: (input: unknown) => Promise<unknown>;
      verifyOtp: (input: unknown) => Promise<unknown>;
    };
    const email = uniqueEmail("signin-existe");
    const password = "senha-correta-9f8e7d6c!";
    const userId = await createConfirmedAccount(gateway, email, password);

    try {
      const resultadoSenhaErrada = await signInWithPassword(gateway, {
        email,
        password: "senha-errada-completamente-diferente",
      });
      const resultadoEmailInexistente = await signInWithPassword(gateway, {
        email: uniqueEmail("signin-nao-existe"),
        password: "qualquer-senha-aqui",
      });

      expect(resultadoSenhaErrada.status).toBe("error");
      expect(resultadoEmailInexistente.status).toBe("error");
      if (resultadoSenhaErrada.status === "error" && resultadoEmailInexistente.status === "error") {
        // O ponto central do critério de aceite: o motivo classificado (e,
        // portanto, a mensagem que a UI mostra a partir dele,
        // `TelaEntrar.tsx`) é exatamente o mesmo balde genérico nos dois
        // casos — nunca um "email-not-found" separado de "wrong-password".
        expect(resultadoSenhaErrada.reason).toBe("invalid-credentials");
        expect(resultadoEmailInexistente.reason).toBe("invalid-credentials");
        expect(resultadoSenhaErrada.reason).toBe(resultadoEmailInexistente.reason);
      }
    } finally {
      await deleteUser(userId);
    }
  });
});
