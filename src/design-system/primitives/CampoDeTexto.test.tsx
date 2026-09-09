// @vitest-environment jsdom
/**
 * TASK-025 — teste de teclado e alvo de toque do primitivo `CampoDeTexto`.
 */
import "@testing-library/jest-dom/vitest";
import { afterEach, describe, expect, it } from "vitest";
import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { CampoDeTexto } from "./CampoDeTexto";
import { TOUCH_TARGET_MIN_STYLE } from "./touch-target";

afterEach(cleanup);

describe("CampoDeTexto", () => {
  it("recebe foco via Tab e aceita digitação via teclado", async () => {
    const user = userEvent.setup();
    render(<CampoDeTexto rotulo="Nome" />);

    await user.tab();
    const campo = screen.getByLabelText("Nome");
    expect(campo).toHaveFocus();

    await user.keyboard("Ana");
    expect(campo).toHaveValue("Ana");
  });

  it("tem rótulo sempre visível, associado ao input (não só placeholder)", () => {
    render(<CampoDeTexto rotulo="Nome" placeholder="Digite seu nome" />);
    expect(screen.getByText("Nome").tagName).toBe("LABEL");
    expect(screen.getByLabelText("Nome")).toBeInTheDocument();
  });

  it("mostra erro com ícone e texto (DI-07: nunca só por cor)", () => {
    render(<CampoDeTexto rotulo="E-mail" erro="Formato de e-mail inválido" />);
    const alerta = screen.getByRole("alert");
    expect(alerta).toHaveTextContent("Formato de e-mail inválido");
    // ícone (elemento decorativo aria-hidden) presente além do texto
    expect(alerta.querySelector("[aria-hidden='true']")).not.toBeNull();

    const campo = screen.getByLabelText("E-mail");
    expect(campo).toHaveAttribute("aria-invalid", "true");
    expect(campo).toHaveAttribute("aria-describedby", alerta.id);
  });

  it("tem alvo de toque mínimo 44x44 (--touch-target-min)", () => {
    render(<CampoDeTexto rotulo="Nome" />);
    const campo = screen.getByLabelText("Nome");
    expect(campo.style.minWidth).toBe(TOUCH_TARGET_MIN_STYLE.minWidth);
    expect(campo.style.minHeight).toBe(TOUCH_TARGET_MIN_STYLE.minHeight);
  });
});
