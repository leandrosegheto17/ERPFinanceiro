export type { AuthGateway } from "./auth-gateway";
export { confirmSignUp } from "./confirm-signup";
export {
  createIdentityAuthGateway,
  resolveSupabaseClientEnvConfig,
} from "./create-identity-supabase-client";
export type {
  CreateIdentityAuthGatewayOptions,
  SupabaseClientEnvConfig,
} from "./create-identity-supabase-client";
export { createDexieAuthStorage } from "./dexie-auth-storage";
export type { DexieAuthStorage } from "./dexie-auth-storage";
export { signUpWithEmailPassword } from "./sign-up";
export type {
  ConfirmSignUpInput,
  ConfirmSignUpResult,
  SignUpErrorReason,
  SignUpInput,
  SignUpResult,
} from "./types";

// TASK-037 (Lote 6 — Identidade): tela T-09 (Entrar/recuperar acesso).
export { classifyAuthError } from "./classify-auth-error";
export { parseAuthCallbackError } from "./parse-auth-callback-error";
export { requestPasswordReset } from "./request-password-reset";
export type { SignInGateway } from "./sign-in-gateway";
export { signInWithMagicLink } from "./sign-in-with-magic-link";
export { signInWithPassword } from "./sign-in-with-password";
export type {
  AuthCallbackErrorReason,
  PasswordlessRequestResult,
  SignInErrorReason,
  SignInWithPasswordInput,
  SignInWithPasswordResult,
} from "./sign-in-types";
export { TelaEntrar } from "./TelaEntrar";
export type { ModoDeAcesso, SessaoIniciada, TelaEntrarProps } from "./TelaEntrar";

// TASK-036 (Lote 6 — Identidade): tela T-08 (Criar conta + consentimento).
export { BlocoConsentimento } from "./BlocoConsentimento";
export type { BlocoConsentimentoProps } from "./BlocoConsentimento";
export {
  CONSENT_TEXT_FULL,
  CONSENT_TEXT_SHA256,
  CONSENT_TEXT_SUMMARY,
  CONSENT_TEXT_VERSION,
} from "./consent-text";
export { signUpWithConsent } from "./sign-up-with-consent";
export { TelaCriarConta } from "./TelaCriarConta";
export type { TelaCriarContaProps } from "./TelaCriarConta";
export type { SignUpWithConsentInput, SignUpWithConsentResult } from "./types";

// TASK-039 (Lote 6 — Identidade): tela T-13, bloco de exportação de dados.
export { BlocoExportarDados } from "./BlocoExportarDados";
export type { BlocoExportarDadosProps } from "./BlocoExportarDados";
export { buildExportFilename, downloadJsonFile } from "./download-json";
export { collectDataExportPayload } from "./export-data";
export type {
  DataExportPayload,
  DataExportPreferenceEntry,
  DataExportProgressEntry,
} from "./export-data";

// TASK-040 (Lote 6 — Identidade): tela T-13, bloco de exclusão de conta.
export { BlocoExcluirConta, PALAVRA_DE_CONFIRMACAO_EXCLUSAO } from "./BlocoExcluirConta";
export type { BlocoExcluirContaProps } from "./BlocoExcluirConta";
export type { ErasureRequestGateway } from "./erasure-request-gateway";
export { getErasureRequestStatus } from "./get-erasure-request-status";
export { requestAccountErasure } from "./request-account-erasure";
export type {
  GetErasureRequestStatusResult,
  RequestAccountErasureInput,
  RequestAccountErasureResult,
} from "./types";
