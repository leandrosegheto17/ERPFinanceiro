/**
 * TASK-036 — `BlocoConsentimento` (UX-SPEC T-08, §5.1 achado "T-08
 * Consentimento como caixa única junto de termos").
 *
 * Componente **separado** do formulário de cadastro — usável/testável de
 * forma isolada (recebe `aceito`/`onChange` de fora, nenhum acoplamento a
 * `TelaCriarConta` nem a `signUpWithConsent`). Desmarcado por padrão: quem
 * decide o valor inicial de `aceito` é sempre o pai (nunca este componente
 * assume `true`); `TelaCriarConta` inicializa com `useState(false)`.
 *
 * Acessibilidade (UX-SPEC §5.1, WCAG 1.3.1/3.3.2):
 * - `fieldset`/`legend` expressam a relação estrutural entre o controle e o
 *   texto de consentimento — não é só posicionamento visual (1.3.1);
 * - o rótulo do `Alternador` é o texto de consentimento completo do resumo
 *   (sempre visível, nunca só placeholder/tooltip — 3.3.2), com a versão do
 *   texto associada via `aria-describedby`;
 * - "ler o texto completo" é um `<button aria-expanded>` nativo que revela
 *   o texto integral no próprio fluxo do DOM (sem overlay/modal — o texto
 *   completo precisa ficar "alcançável", conforme o achado do UX-SPEC), e
 *   Tab/Enter/Espaço funcionam sem handler de teclado escrito à mão;
 * - `erro` (quando o formulário tentar avançar sem consentimento) segue o
 *   mesmo padrão de `CampoDeTexto`/`CampoDeSenha`: ícone **e** texto (DI-07),
 *   nunca só cor, com `role="alert"`.
 */
import { useId, useState } from "react";

import { Alternador } from "../../design-system/primitives";
import { CONSENT_TEXT_FULL, CONSENT_TEXT_SUMMARY, CONSENT_TEXT_VERSION } from "./consent-text";
import styles from "./BlocoConsentimento.module.css";

export interface BlocoConsentimentoProps {
  aceito: boolean;
  onChange: (aceito: boolean) => void;
  /** Versão exibida — sempre a mesma gravada em `consent.version` ao aceitar. */
  versao?: string;
  /** Mensagem de erro (ex.: "marque para continuar"), sem nunca ser só cor. */
  erro?: string;
  id?: string;
}

export function BlocoConsentimento({
  aceito,
  onChange,
  versao = CONSENT_TEXT_VERSION,
  erro,
  id,
}: BlocoConsentimentoProps) {
  const generatedId = useId();
  const baseId = id ?? generatedId;
  const versaoId = `${baseId}-versao`;
  const textoCompletoId = `${baseId}-texto-completo`;
  const erroId = `${baseId}-erro`;
  const [textoCompletoVisivel, setTextoCompletoVisivel] = useState(false);

  return (
    <fieldset className={styles.bloco}>
      <legend className={styles.legenda}>Consentimento para dado pessoal sensível</legend>
      <Alternador
        rotulo={CONSENT_TEXT_SUMMARY}
        ligado={aceito}
        onChange={onChange}
        aria-describedby={
          [versaoId, erro ? erroId : undefined].filter(Boolean).join(" ") || undefined
        }
      />
      <p id={versaoId} className={styles.versao}>
        Versão do texto: {versao}
      </p>
      <button
        type="button"
        className={styles.linkTextoCompleto}
        aria-expanded={textoCompletoVisivel}
        aria-controls={textoCompletoId}
        onClick={() => setTextoCompletoVisivel((atual) => !atual)}
      >
        {textoCompletoVisivel ? "ocultar texto completo" : "ler o texto completo"}
      </button>
      {textoCompletoVisivel && (
        <p id={textoCompletoId} className={styles.textoCompleto}>
          {CONSENT_TEXT_FULL}
        </p>
      )}
      {erro && (
        <p id={erroId} className={styles.erro} role="alert">
          <span aria-hidden="true" className={styles.erroIcone}>
            ⚠
          </span>
          <span>{erro}</span>
        </p>
      )}
    </fieldset>
  );
}
