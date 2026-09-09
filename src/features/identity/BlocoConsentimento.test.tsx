// @vitest-environment jsdom
/**
 * TASK-036 — teste de `BlocoConsentimento` isolado (sem `TelaCriarConta`),
 * cobrindo WCAG 3.3.2 (rótulo/instrução sempre visível) e 1.3.1 (relação
 * estrutural fieldset/legend, não só posicionamento visual).
 */
import { useState } from "react";
import "@testing-library/jest-dom/vitest";
import { afterEach, describe, expect, it } from "vitest";
import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { BlocoConsentimento } from "./BlocoConsentimento";
import { CONSENT_TEXT_FULL, CONSENT_TEXT_SUMMARY, CONSENT_TEXT_VERSION } from "./consent-text";

afterEach(cleanup);

function BlocoConsentimentoControlado({
  inicial = false,
  erro,
}: {
  inicial?: boolean;
  erro?: string;
}) {
  const [aceito, setAceito] = useState(inicial);
  return <BlocoConsentimento aceito={aceito} onChange={setAceito} erro={erro} />;
}

describe("BlocoConsentimento (TASK-036)", () => {
  it("é usável isoladamente, sem TelaCriarConta ao redor", () => {
    render(<BlocoConsentimento aceito={false} onChange={() => {}} />);
    expect(screen.getByRole("switch")).toBeInTheDocument();
  });

  it("começa desmarcado por padrão quando o pai inicializa aceito=false", () => {
    render(<BlocoConsentimentoControlado />);
    expect(screen.getByRole("switch")).toHaveAttribute("aria-checked", "false");
  });

  it("usa fieldset/legend — relação estrutural entre controle e texto, não só posição visual (1.3.1)", () => {
    const { container } = render(<BlocoConsentimento aceito={false} onChange={() => {}} />);
    const fieldset = container.querySelector("fieldset");
    expect(fieldset).not.toBeNull();
    const legend = fieldset?.querySelector("legend");
    expect(legend).not.toBeNull();
    expect(legend?.textContent).toMatch(/consentimento/i);
  });

  it("tem o texto de consentimento sempre visível como rótulo do controle, nunca só placeholder/tooltip (3.3.2)", () => {
    render(<BlocoConsentimento aceito={false} onChange={() => {}} />);
    const alternador = screen.getByRole("switch");
    expect(alternador).toHaveTextContent(CONSENT_TEXT_SUMMARY);
  });

  it("exibe a versão do texto exibida — a mesma constante gravada em consent.version ao aceitar", () => {
    render(<BlocoConsentimento aceito={false} onChange={() => {}} />);
    expect(screen.getByText(new RegExp(CONSENT_TEXT_VERSION))).toBeInTheDocument();
  });

  it("aceita versao explícita distinta do default, e exibe exatamente essa", () => {
    render(<BlocoConsentimento aceito={false} onChange={() => {}} versao="outra-versao" />);
    expect(screen.getByText(/outra-versao/)).toBeInTheDocument();
    expect(screen.queryByText(new RegExp(CONSENT_TEXT_VERSION))).not.toBeInTheDocument();
  });

  it("revela o texto completo do consentimento via botão 'ler o texto completo', com aria-expanded", async () => {
    const user = userEvent.setup();
    render(<BlocoConsentimento aceito={false} onChange={() => {}} />);

    expect(screen.queryByText(CONSENT_TEXT_FULL)).not.toBeInTheDocument();
    const botao = screen.getByRole("button", { name: /ler o texto completo/i });
    expect(botao).toHaveAttribute("aria-expanded", "false");

    await user.click(botao);

    expect(botao).toHaveAttribute("aria-expanded", "true");
    expect(screen.getByText(CONSENT_TEXT_FULL)).toBeInTheDocument();
  });

  it("alterna via teclado (Tab + Espaço), sem handler de teclado escrito à mão", async () => {
    const user = userEvent.setup();
    render(<BlocoConsentimentoControlado />);

    await user.tab();
    const alternador = screen.getByRole("switch");
    expect(alternador).toHaveFocus();

    await user.keyboard(" ");
    expect(alternador).toHaveAttribute("aria-checked", "true");
  });

  it("mostra erro de validação com ícone e texto, nunca só cor (DI-07), quando o pai passa `erro`", () => {
    render(<BlocoConsentimentoControlado erro="Marque para continuar o cadastro" />);
    const alerta = screen.getByRole("alert");
    expect(alerta).toHaveTextContent("Marque para continuar o cadastro");
    expect(alerta.querySelector("[aria-hidden='true']")).not.toBeNull();

    const alternador = screen.getByRole("switch");
    expect(alternador.getAttribute("aria-describedby")).toContain(alerta.id);
  });
});
