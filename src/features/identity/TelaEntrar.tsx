/**
 * TASK-037 (Lote 6 — Identidade) — Tela T-09 (Entrar/recuperar acesso,
 * UX-SPEC.md).
 *
 * Três modos dentro da mesma tela (nunca uma rota separada por modo, para
 * não perder o e-mail já digitado ao trocar de método): login por senha
 * (padrão), link mágico, e recuperação de senha. Sem CAPTCHA de
 * quebra-cabeça em nenhum modo (UX-SPEC §5.1, 3.3.8) — nenhum mecanismo
 * anti-robô foi adicionado porque nada no SDD.md/GUARDRAILS.md exige um, e
 * o único mecanismo anti-abuso é o limite de tentativas do próprio
 * servidor (Supabase Auth), refletido aqui só como o estado de erro
 * `"rate-limited"`.
 *
 * Guardrail central (critério de aceite de TASK-037): a mensagem de erro
 * de login e de recuperação nunca diferencia "e-mail não existe" de
 * qualquer outro motivo — ver `mapearMensagemDeErro` abaixo, que só
 * conhece os baldes genéricos de `sign-in-types.ts`, nunca um motivo
 * específico de conta.
 *
 * Os 4 estados de UX-SPEC §4 (DI-06), por modo:
 * - Vazio: N/A — a tela sempre mostra o formulário (mesmo motivo do T-09
 *   na tabela de estados do UX-SPEC.md).
 * - Carregando: botão em processamento, campos e alternativas desabilitados.
 * - Erro: credencial inválida (genérico), sem rede, limite de tentativas,
 *   ou link expirado (ao chegar via redirect de e-mail) — cada um com
 *   texto próprio, nenhum revelando existência de e-mail.
 * - Sucesso: sessão ativa (login) ou confirmação genérica de envio (link
 *   mágico/recuperação).
 */
import { useEffect, useId, useState } from "react";
import type { FormEvent } from "react";

import { Botao, CampoDeSenha, CampoDeTexto } from "../../design-system/primitives";
import { parseAuthCallbackError } from "./parse-auth-callback-error";
import { requestPasswordReset } from "./request-password-reset";
import type { SignInGateway } from "./sign-in-gateway";
import { signInWithMagicLink } from "./sign-in-with-magic-link";
import { signInWithPassword } from "./sign-in-with-password";
import type { AuthCallbackErrorReason, SignInErrorReason } from "./sign-in-types";
import styles from "./TelaEntrar.module.css";

export type ModoDeAcesso = "senha" | "link-magico" | "recuperar-senha";

export interface SessaoIniciada {
  readonly userId: string;
  readonly accessToken: string;
  readonly refreshToken: string;
}

export interface TelaEntrarProps {
  readonly gateway: SignInGateway;
  /** Chamado quando o login por senha é concluído com sucesso. */
  readonly onSignedIn?: (sessao: SessaoIniciada) => void;
  /** URL de retorno após o clique no link de recuperação de senha. */
  readonly redirectToAposRecuperacao?: string;
  /**
   * Fragmento/query da URL atual — usado para detectar retorno de um link
   * mágico/recuperação expirado (UX-SPEC "link expirado"). Por padrão lê
   * `window.location.hash`; parametrizável para teste sem `jsdom`/DOM real.
   */
  readonly localizacaoInicial?: string;
}

const MENSAGENS_DE_ERRO_DE_ACESSO: Record<SignInErrorReason, string> = {
  // Deliberadamente o mesmo texto para qualquer motivo de credencial —
  // nunca "e-mail não encontrado" nem "senha incorreta" separados.
  "invalid-credentials": "E-mail ou senha incorretos.",
  "network-error": "Sem conexão. Verifique sua internet e tente novamente.",
  "rate-limited": "Muitas tentativas. Aguarde alguns minutos antes de tentar de novo.",
};

const MENSAGENS_DE_ERRO_DE_CALLBACK: Record<AuthCallbackErrorReason, string> = {
  "link-expirado": "Esse link expirou ou já foi usado. Solicite um novo.",
  desconhecido: "Não foi possível concluir a autenticação. Tente novamente.",
};

const MENSAGEM_LINK_MAGICO_ENVIADO =
  "Se esse e-mail existir na nossa base, você vai receber um link de acesso em instantes.";
const MENSAGEM_RECUPERACAO_ENVIADA =
  "Se esse e-mail existir na nossa base, você vai receber instruções para redefinir sua senha.";

export function TelaEntrar({
  gateway,
  onSignedIn,
  redirectToAposRecuperacao,
  localizacaoInicial,
}: TelaEntrarProps) {
  const tituloId = useId();
  const [modo, setModo] = useState<ModoDeAcesso>("senha");
  const [email, setEmail] = useState("");
  const [senha, setSenha] = useState("");
  const [enviando, setEnviando] = useState(false);
  const [erro, setErro] = useState<string | undefined>(undefined);
  const [mensagemDeConfirmacao, setMensagemDeConfirmacao] = useState<string | undefined>(
    undefined,
  );
  const [sessaoAtiva, setSessaoAtiva] = useState(false);

  useEffect(() => {
    const localizacao =
      localizacaoInicial ??
      (typeof window !== "undefined" ? window.location.hash : undefined);
    if (!localizacao) {
      return;
    }
    const motivo = parseAuthCallbackError(localizacao);
    if (motivo) {
      setErro(MENSAGENS_DE_ERRO_DE_CALLBACK[motivo]);
    }
    // Roda só na montagem: o erro de callback é lido uma única vez da URL
    // com a qual esta tela foi aberta.
  }, []);

  function trocarModo(novoModo: ModoDeAcesso) {
    setModo(novoModo);
    setErro(undefined);
    setMensagemDeConfirmacao(undefined);
  }

  async function handleEntrarComSenha(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setErro(undefined);
    setMensagemDeConfirmacao(undefined);
    setEnviando(true);
    const resultado = await signInWithPassword(gateway, { email, password: senha });
    setEnviando(false);
    if (resultado.status === "signed-in") {
      setSessaoAtiva(true);
      onSignedIn?.({
        userId: resultado.userId,
        accessToken: resultado.accessToken,
        refreshToken: resultado.refreshToken,
      });
      return;
    }
    setErro(MENSAGENS_DE_ERRO_DE_ACESSO[resultado.reason]);
  }

  async function handleEnviarLinkMagico(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setErro(undefined);
    setMensagemDeConfirmacao(undefined);
    setEnviando(true);
    const resultado = await signInWithMagicLink(gateway, email);
    setEnviando(false);
    if (resultado.status === "request-sent") {
      setMensagemDeConfirmacao(MENSAGEM_LINK_MAGICO_ENVIADO);
      return;
    }
    setErro(MENSAGENS_DE_ERRO_DE_ACESSO[resultado.reason]);
  }

  async function handleSolicitarRecuperacao(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setErro(undefined);
    setMensagemDeConfirmacao(undefined);
    setEnviando(true);
    const resultado = await requestPasswordReset(gateway, email, {
      redirectTo: redirectToAposRecuperacao,
    });
    setEnviando(false);
    if (resultado.status === "request-sent") {
      setMensagemDeConfirmacao(MENSAGEM_RECUPERACAO_ENVIADA);
      return;
    }
    setErro(MENSAGENS_DE_ERRO_DE_ACESSO[resultado.reason]);
  }

  if (sessaoAtiva) {
    return (
      <main className={styles.tela}>
        <h1 id={tituloId}>Entrar</h1>
        <p role="status">Sessão ativa. Você já pode continuar de onde parou.</p>
      </main>
    );
  }

  return (
    <main className={styles.tela}>
      <h1 id={tituloId}>{tituloPorModo(modo)}</h1>

      {erro && (
        <p role="alert" className={styles.mensagemSucesso}>
          <span aria-hidden="true">⚠</span>
          <span>{erro}</span>
        </p>
      )}
      {mensagemDeConfirmacao && (
        <p role="status" className={styles.mensagemSucesso}>
          <span aria-hidden="true">✔</span>
          <span>{mensagemDeConfirmacao}</span>
        </p>
      )}

      {modo === "senha" && (
        <form className={styles.formulario} onSubmit={handleEntrarComSenha} aria-labelledby={tituloId}>
          <CampoDeTexto
            rotulo="E-mail"
            type="email"
            autoComplete="email"
            required
            value={email}
            onChange={(event) => setEmail(event.target.value)}
            disabled={enviando}
          />
          <CampoDeSenha
            rotulo="Senha"
            autoComplete="current-password"
            required
            value={senha}
            onChange={(event) => setSenha(event.target.value)}
            disabled={enviando}
          />
          <Botao type="submit" variante="primario" disabled={enviando}>
            {enviando ? "Entrando…" : "Entrar"}
          </Botao>
          <div className={styles.alternativas}>
            <button
              type="button"
              className={styles.linkComoBotao}
              onClick={() => trocarModo("recuperar-senha")}
              disabled={enviando}
            >
              Esqueci minha senha
            </button>
            <button
              type="button"
              className={styles.linkComoBotao}
              onClick={() => trocarModo("link-magico")}
              disabled={enviando}
            >
              Entrar com link por e-mail
            </button>
          </div>
        </form>
      )}

      {modo === "link-magico" && (
        <form
          className={styles.formulario}
          onSubmit={handleEnviarLinkMagico}
          aria-labelledby={tituloId}
        >
          <CampoDeTexto
            rotulo="E-mail"
            type="email"
            autoComplete="email"
            required
            value={email}
            onChange={(event) => setEmail(event.target.value)}
            disabled={enviando}
          />
          <Botao type="submit" variante="primario" disabled={enviando}>
            {enviando ? "Enviando…" : "Enviar link mágico"}
          </Botao>
          <div className={styles.alternativas}>
            <button
              type="button"
              className={styles.linkComoBotao}
              onClick={() => trocarModo("senha")}
              disabled={enviando}
            >
              Entrar com senha
            </button>
          </div>
        </form>
      )}

      {modo === "recuperar-senha" && (
        <form
          className={styles.formulario}
          onSubmit={handleSolicitarRecuperacao}
          aria-labelledby={tituloId}
        >
          <CampoDeTexto
            rotulo="E-mail"
            type="email"
            autoComplete="email"
            required
            value={email}
            onChange={(event) => setEmail(event.target.value)}
            disabled={enviando}
          />
          <Botao type="submit" variante="primario" disabled={enviando}>
            {enviando ? "Enviando…" : "Enviar instruções"}
          </Botao>
          <div className={styles.alternativas}>
            <button
              type="button"
              className={styles.linkComoBotao}
              onClick={() => trocarModo("senha")}
              disabled={enviando}
            >
              Voltar para entrar
            </button>
          </div>
        </form>
      )}
    </main>
  );
}

function tituloPorModo(modo: ModoDeAcesso): string {
  if (modo === "link-magico") {
    return "Entrar com link por e-mail";
  }
  if (modo === "recuperar-senha") {
    return "Recuperar acesso";
  }
  return "Entrar";
}
