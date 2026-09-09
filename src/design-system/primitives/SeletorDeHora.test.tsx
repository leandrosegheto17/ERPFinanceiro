// @vitest-environment jsdom
/**
 * TASK-025 — teste de teclado e alvo de toque do primitivo `SeletorDeHora`.
 */
import "@testing-library/jest-dom/vitest";
import { afterEach, describe, expect, it } from "vitest";
import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { SeletorDeHora } from "./SeletorDeHora";
import { TOUCH_TARGET_MIN_STYLE } from "./touch-target";

afterEach(cleanup);

describe("SeletorDeHora", () => {
  it("recebe foco via Tab e aceita digitação via teclado", async () => {
    const user = userEvent.setup();
    render(<SeletorDeHora rotulo="Horário do lembrete" />);

    await user.tab();
    const campo = screen.getByLabelText("Horário do lembrete");
    expect(campo).toHaveFocus();
    expect(campo).toHaveAttribute("type", "time");

    await user.keyboard("0730AM");
    expect(campo).toHaveValue("07:30");
  });

  it("tem rótulo sempre visível", () => {
    render(<SeletorDeHora rotulo="Horário do lembrete" />);
    expect(screen.getByText("Horário do lembrete").tagName).toBe("LABEL");
  });

  it("mostra erro com ícone e texto (DI-07: nunca só por cor)", () => {
    render(<SeletorDeHora rotulo="Horário do lembrete" erro="Escolha um horário válido" />);
    const alerta = screen.getByRole("alert");
    expect(alerta).toHaveTextContent("Escolha um horário válido");
    expect(alerta.querySelector("[aria-hidden='true']")).not.toBeNull();
  });

  it("tem alvo de toque mínimo 44x44 (--touch-target-min)", () => {
    render(<SeletorDeHora rotulo="Horário do lembrete" />);
    const campo = screen.getByLabelText("Horário do lembrete");
    expect(campo.style.minWidth).toBe(TOUCH_TARGET_MIN_STYLE.minWidth);
    expect(campo.style.minHeight).toBe(TOUCH_TARGET_MIN_STYLE.minHeight);
  });
});
