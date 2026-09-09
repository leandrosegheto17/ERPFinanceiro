import type { AuthGateway } from "./auth-gateway";
import { signUpWithEmailPassword } from "./sign-up";
import type { SignUpWithConsentInput, SignUpWithConsentResult } from "./types";

/**
 * TASK-036 — orquestra cadastro (TASK-035) + aceite de consentimento
 * (TASK-027, `public.consent`) na ordem exigida pelo critério de aceite:
 * "grava `consent` antes de qualquer dado pessoal".
 *
 * ## Correção da reprovação crítica do Validador (2026-09-08)
 *
 * A versão anterior desta função chamava `signUpWithEmailPassword` e, **em
 * seguida, como uma segunda chamada HTTP separada**, `recordConsent` —
 * garantindo a ordem só por sequência de chamadas no cliente. Isso
 * contraria `SDD.md` §7.6 de forma literal: "Nenhuma escrita de dado
 * pessoal no servidor ocorre antes de existir a linha de consentimento —
 * garantido por política no banco, não por ordem de chamadas no cliente."
 * Se `signUp` tivesse sucesso e o segundo INSERT falhasse (rede/JS/crash
 * entre as duas chamadas), ficava uma conta persistida sem `consent`, sem
 * nenhum mecanismo de banco para impedir ou reparar isso. Ver
 * `.md/QA-REPORT.md` "Lote 6" (Seções 2-3), `.md/SECURITY-REVIEW.md`
 * "Lote 6" (DEVSEC-L6-01) e `.md/BLOCKERS.md`, Entrada 2.
 *
 * Correção: `version`/`text_sha256` viajam como metadata do próprio
 * `signUp` (`options.data`, suportado nativamente pelo Supabase Auth —
 * `SignUpInput.metadata`), persistida pelo GoTrue em
 * `auth.users.raw_user_meta_data` antes do gatilho de banco
 * `handle_new_auth_user`
 * (`supabase/migrations/20260908190000_profile_on_signup_trigger.sql`)
 * disparar. Esse gatilho insere `profile` **e** `consent` na mesma
 * transação que cria `auth.users` — se a gravação de `consent` falhar
 * (dentro do gatilho), a transação inteira é desfeita pelo Postgres,
 * inclusive a criação do usuário. Não existe mais estado intermediário
 * "usuário existe, consentimento não" — é impossível por construção, não
 * apenas evitado por ordem de chamadas.
 *
 * Por isso esta função virou uma chamada única a `signUpWithEmailPassword`
 * com a metadata de consentimento anexada — nenhuma segunda chamada de rede,
 * nenhum `ConsentGateway`/`recordConsent` (removidos: ver histórico de
 * `.md/TASK.md` TASK-036 para a versão anterior). `SignUpWithConsentResult`
 * é o mesmo tipo de `SignUpResult` (`types.ts`): se `signUp` falhar — por
 * qualquer motivo, incluindo uma falha do gatilho ao gravar `consent` — o
 * resultado é `"error"`, nunca sucesso parcial.
 */
export async function signUpWithConsent(
  authGateway: AuthGateway,
  input: SignUpWithConsentInput,
): Promise<SignUpWithConsentResult> {
  return signUpWithEmailPassword(authGateway, {
    email: input.email,
    password: input.password,
    metadata: {
      consent_version: input.consentVersion,
      consent_text_sha256: input.consentTextSha256,
    },
  });
}
