/**
 * TASK-036 — Tela T-08 (Criar conta + consentimento), UX-SPEC T-08/§4/§5.1.
 *
 * Consome `signUpWithConsent` (TASK-035 + TASK-036), que garante a ordem
 * "`consent` antes de qualquer dado pessoal" — ver `sign-up-with-consent.ts`
 * para a decisão completa sobre essa ordem. Esta tela não grava nada
 * diretamente: só chama a orquestração e reage ao resultado.
 *
 * Quatro estados (DI-06, UX-SPEC §4 "T-08 Criar conta"):
 * - **vazio**: formulário pronto, nada preenchido/enviado ainda;
 * - **carregando**: botão em processamento, campos bloqueados;
 * - **erro**: e-mail já usado, senha fraca, sem rede, consentimento não
 *   marcado — cada um com mensagem própria e foco movido ao campo relevante
 *   (WCAG 3.3.2/3.3.1);
 * - **sucesso**: conta criada + estado de migração de progresso (RN-07) —
 *   a migração em si (TASK-055) fica fora do escopo desta tarefa; esta tela
 *   só exibe a contagem de dias quando o chamador a informa via
 *   `diasDeProgressoParaMigrar`.
 */
import { useEffect, useRef, useState } from "react";
import type { FormEvent } from "react";

import { Botao, CampoDeSenha, CampoDeTexto } from "../../design-system/primitives";
import type { AuthGateway } from "./auth-gateway";
import { BlocoConsentimento } from "./BlocoConsentimento";
import { CONSENT_TEXT_SHA256, CONSENT_TEXT_VERSION } from "./consent-text";
import { signUpWithConsent } from "./sign-up-with-consent";
import type { SignUpWithConsentResult } from "./types";
import styles from "./TelaCriarConta.module.css";

type CampoComErro = "email" | "senha" | "consentimento" | "geral";

type Estado =
  | { tipo: "vazio" }
  | { tipo: "carregando" }
  | { tipo: "erro"; campo: CampoComErro; mensagem: string }
  | { tipo: "sucesso"; requerConfirmacaoDeEmail: boolean };

export interface TelaCriarContaProps {
  authGateway: AuthGateway;
  /**
   * Dias de leitura anônima a migrar (RN-07), quando existir progresso
   * local anterior ao cadastro. A migração em si é TASK-055 (integração
   * T-07→T-08→T-03); esta tela só exibe o número quando informado.
   */
  diasDeProgressoParaMigrar?: number;
  onCadastroConcluido?: (result: {
    userId: string;
    requerConfirmacaoDeEmail: boolean;
  }) => void;
}

function mensagemDeErroDeSignUp(
  result: Extract<SignUpWithConsentResult, { status: "error" }>,
): { campo: CampoComErro; mensagem: string } {
  switch (result.reason) {
    case "email-already-registered":
      return { campo: "email", mensagem: "Este e-mail já tem uma conta cadastrada." };
    case "invalid-email":
      return { campo: "email", mensagem: "Digite um e-mail em formato válido." };
    case "weak-password":
      return { campo: "senha", mensagem: "Escolha uma senha mais forte (mínimo 6 caracteres)." };
    case "rate-limited":
      return {
        campo: "geral",
        mensagem: "Muitas tentativas em pouco tempo. Aguarde um instante e tente de novo.",
      };
    case "unknown":
    default:
      return {
        campo: "geral",
        mensagem: "Não foi possível concluir o cadastro. Verifique sua conexão e tente novamente.",
      };
  }
}

export function TelaCriarConta({
  authGateway,
  diasDeProgressoParaMigrar,
  onCadastroConcluido,
}: TelaCriarContaProps) {
  const [email, setEmail] = useState("");
  const [senha, setSenha] = useState("");
  const [consentimentoAceito, setConsentimentoAceito] = useState(false);
  const [estado, setEstado] = useState<Estado>({ tipo: "vazio" });

  const emailRef = useRef<HTMLInputElement>(null);
  const senhaRef = useRef<HTMLInputElement>(null);
  const consentimentoContainerRef = useRef<HTMLDivElement>(null);
  const erroGeralRef = useRef<HTMLParagraphElement>(null);

  useEffect(() => {
    if (estado.tipo !== "erro") {
      return;
    }
    if (estado.campo === "email") {
      emailRef.current?.focus();
    } else if (estado.campo === "senha") {
      senhaRef.current?.focus();
    } else if (estado.campo === "consentimento") {
      consentimentoContainerRef.current
        ?.querySelector<HTMLButtonElement>('[role="switch"]')
        ?.focus();
    } else {
      erroGeralRef.current?.focus();
    }
  }, [estado]);

  const carregando = estado.tipo === "carregando";

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!consentimentoAceito) {
      setEstado({
        tipo: "erro",
        campo: "consentimento",
        mensagem: "Marque a caixa de consentimento para continuar.",
      });
      return;
    }

    setEstado({ tipo: "carregando" });

    try {
      const result = await signUpWithConsent(authGateway, {
        email,
        password: senha,
        consentVersion: CONSENT_TEXT_VERSION,
        consentTextSha256: CONSENT_TEXT_SHA256,
      });

      if (result.status === "error") {
        setEstado({ tipo: "erro", ...mensagemDeErroDeSignUp(result) });
        return;
      }

      const requerConfirmacaoDeEmail = result.status === "confirmation-required";
      setEstado({ tipo: "sucesso", requerConfirmacaoDeEmail });
      onCadastroConcluido?.({ userId: result.userId, requerConfirmacaoDeEmail });
    } catch {
      setEstado({
        tipo: "erro",
        campo: "geral",
        mensagem: "Sem conexão com a internet. Verifique sua rede e tente novamente.",
      });
    }
  }

  if (estado.tipo === "sucesso") {
    return (
      <div className={styles.tela} role="status">
        <h1 className={styles.titulo}>Conta criada</h1>
        {estado.requerConfirmacaoDeEmail ? (
          <p>Enviamos um link de confirmação para o seu e-mail.</p>
        ) : (
          <p>Sua conta está pronta.</p>
        )}
        {typeof diasDeProgressoParaMigrar === "number" && diasDeProgressoParaMigrar > 0 && (
          <p>
            Seus {diasDeProgressoParaMigrar}{" "}
            {diasDeProgressoParaMigrar === 1 ? "dia foi guardado" : "dias foram guardados"} na
            sua conta.
          </p>
        )}
      </div>
    );
  }

  const erroConsentimento =
    estado.tipo === "erro" && estado.campo === "consentimento" ? estado.mensagem : undefined;
  const erroEmail =
    estado.tipo === "erro" && estado.campo === "email" ? estado.mensagem : undefined;
  const erroSenha =
    estado.tipo === "erro" && estado.campo === "senha" ? estado.mensagem : undefined;
  const erroGeral =
    estado.tipo === "erro" && estado.campo === "geral" ? estado.mensagem : undefined;

  return (
    <form className={styles.tela} onSubmit={handleSubmit} noValidate>
      <h1 className={styles.titulo}>Criar conta</h1>

      {erroGeral && (
        <p
          ref={erroGeralRef}
          tabIndex={-1}
          role="alert"
          className={styles.erroGeral}
        >
          <span aria-hidden="true" className={styles.erroGeralIcone}>
            ⚠
          </span>
          <span>{erroGeral}</span>
        </p>
      )}

      <CampoDeTexto
        ref={emailRef}
        rotulo="E-mail"
        type="email"
        autoComplete="email"
        value={email}
        onChange={(event) => setEmail(event.target.value)}
        erro={erroEmail}
        disabled={carregando}
        required
      />

      <CampoDeSenha
        ref={senhaRef}
        rotulo="Senha"
        autoComplete="new-password"
        value={senha}
        onChange={(event) => setSenha(event.target.value)}
        erro={erroSenha}
        disabled={carregando}
        required
      />

      <div ref={consentimentoContainerRef}>
        <BlocoConsentimento
          aceito={consentimentoAceito}
          onChange={setConsentimentoAceito}
          erro={erroConsentimento}
        />
      </div>

      <Botao type="submit" disabled={carregando || !consentimentoAceito}>
        {carregando ? "Criando conta…" : "Criar conta"}
      </Botao>
      {!consentimentoAceito && (
        <p className={styles.motivoDesabilitado}>
          Marque o consentimento acima para habilitar o botão.
        </p>
      )}
    </form>
  );
}
