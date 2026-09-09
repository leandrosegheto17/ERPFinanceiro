// @vitest-environment jsdom
/**
 * TASK-025 — teste de teclado, alvo de toque e senha colável do primitivo
 * `CampoDeSenha`.
 */
import "@testing-library/jest-dom/vitest";
import { afterEach, describe, expect, it } from "vitest";
import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { CampoDeSenha } from "./CampoDeSenha";
import { TOUCH_TARGET_MIN_STYLE } from "./touch-target";

afterEach(cleanup);

describe("CampoDeSenha", () => {
  it("recebe foco via Tab, aceita digitação, e o botão de mostrar/ocultar é alcançado em seguida via Tab e ativado via Enter", async () => {
    const user = userEvent.setup();
    render(<CampoDeSenha rotulo="Senha" />);

    await user.tab();
    const campo = screen.getByLabelText("Senha");
    expect(campo).toHaveFocus();
    expect(campo).toHaveAttribute("type", "password");

    await user.keyboard("segredo123");
    expect(campo).toHaveValue("segredo123");

    await user.tab();
    const botaoMostrar = screen.getByRole("button", { name: "Mostrar Senha" });
    expect(botaoMostrar).toHaveFocus();

    await user.keyboard("{Enter}");
    expect(campo).toHaveAttribute("type", "text");
    expect(screen.getByRole("button", { name: "Ocultar Senha" })).toHaveFocus();
  });

  it("ativa o botão de mostrar/ocultar via Espaço também", async () => {
    const user = userEvent.setup();
    render(<CampoDeSenha rotulo="Senha" />);

    await user.tab();
    await user.tab();
    await user.keyboard(" ");
    expect(screen.getByLabelText("Senha")).toHaveAttribute("type", "text");
  });

  it("nunca bloqueia colar (senha colável)", () => {
    render(<CampoDeSenha rotulo="Senha" />);
    const campo = screen.getByLabelText("Senha");
    expect(campo).not.toHaveAttribute("onpaste");

    const evento = new Event("paste", { bubbles: true, cancelable: true });
    const evitado = !campo.dispatchEvent(evento);
    // dispatchEvent retorna false se algum handler chamou preventDefault();
    // aqui não há handler nenhum, então o evento deve seguir seu curso.
    expect(evitado).toBe(false);
  });

  it("mostra erro com ícone e texto (DI-07: nunca só por cor)", () => {
    render(<CampoDeSenha rotulo="Senha" erro="Senha muito curta" />);
    const alerta = screen.getByRole("alert");
    expect(alerta).toHaveTextContent("Senha muito curta");
    expect(alerta.querySelector("[aria-hidden='true']")).not.toBeNull();
  });

  it("campo de senha e botão de alternar visibilidade têm alvo de toque mínimo 44x44", () => {
    render(<CampoDeSenha rotulo="Senha" />);
    const campo = screen.getByLabelText("Senha");
    const botao = screen.getByRole("button", { name: "Mostrar Senha" });

    for (const elemento of [campo, botao]) {
      expect(elemento.style.minWidth).toBe(TOUCH_TARGET_MIN_STYLE.minWidth);
      expect(elemento.style.minHeight).toBe(TOUCH_TARGET_MIN_STYLE.minHeight);
    }
  });
});
