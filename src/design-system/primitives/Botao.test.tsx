// @vitest-environment jsdom
/**
 * TASK-025 — teste de teclado e alvo de toque do primitivo `Botao`.
 */
import "@testing-library/jest-dom/vitest";
import { afterEach, describe, expect, it, vi } from "vitest";
import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { Botao } from "./Botao";
import { TOUCH_TARGET_MIN_STYLE } from "./touch-target";

afterEach(cleanup);

describe("Botao", () => {
  it("recebe foco via Tab e ativa via Enter", async () => {
    const user = userEvent.setup();
    const aoClicar = vi.fn();
    render(<Botao onClick={aoClicar}>Continuar</Botao>);

    await user.tab();
    expect(screen.getByRole("button", { name: "Continuar" })).toHaveFocus();

    await user.keyboard("{Enter}");
    expect(aoClicar).toHaveBeenCalledTimes(1);
  });

  it("ativa via Espaço", async () => {
    const user = userEvent.setup();
    const aoClicar = vi.fn();
    render(<Botao onClick={aoClicar}>Continuar</Botao>);

    await user.tab();
    await user.keyboard(" ");
    expect(aoClicar).toHaveBeenCalledTimes(1);
  });

  it("tem alvo de toque mínimo 44x44 (--touch-target-min)", () => {
    render(<Botao>Continuar</Botao>);
    const botao = screen.getByRole("button", { name: "Continuar" });
    expect(botao.style.minWidth).toBe(TOUCH_TARGET_MIN_STYLE.minWidth);
    expect(botao.style.minHeight).toBe(TOUCH_TARGET_MIN_STYLE.minHeight);
  });

  it("não é alcançado por Tab quando desabilitado", async () => {
    const user = userEvent.setup();
    render(<Botao disabled>Continuar</Botao>);

    await user.tab();
    expect(screen.getByRole("button", { name: "Continuar" })).not.toHaveFocus();
  });

  it("aplica a variante correta", () => {
    render(<Botao variante="perigo">Excluir conta</Botao>);
    const botao = screen.getByRole("button", { name: "Excluir conta" });
    expect(botao.className).toMatch(/perigo/);
  });
});
