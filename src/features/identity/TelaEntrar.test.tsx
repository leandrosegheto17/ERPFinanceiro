// @vitest-environment jsdom
/**
 * TASK-037 — Componente `TelaEntrar` (T-09): cobre os estados de
 * carregando/erro/sucesso de UX-SPEC §4 (DI-06; "vazio" é N/A para esta
 * tela, mesma linha do UX-SPEC.md) e o critério de aceite central —
 * mensagem de erro idêntica para senha errada e para e-mail inexistente.
 */
import "@testing-library/jest-dom/vitest";
import { afterEach, describe, expect, it, vi } from "vitest";
import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";

import type { SignInGateway } from "./sign-in-gateway";
import { TelaEntrar } from "./TelaEntrar";

afterEach(cleanup);

function criarGatewayControlavel() {
  let resolverSignIn: ((value: unknown) => void) | undefined;
  const signInWithPassword = vi.fn(
    () =>
      new Promise((resolve) => {
        resolverSignIn = resolve;
      }),
  );
  const gateway = {
    signInWithPassword,
    signInWithOtp: vi.fn(async () => ({ data: {}, error: null })),
    resetPasswordForEmail: vi.fn(async () => ({ data: {}, error: null })),
  } as unknown as SignInGateway;
  return { gateway, signInWithPassword, resolver: () => resolverSignIn };
}

async function preencherLogin(user: ReturnType<typeof userEvent.setup>, email: string, senha: string) {
  await user.type(screen.getByLabelText("E-mail"), email);
  await user.type(screen.getByLabelText("Senha"), senha);
}

describe("TelaEntrar (TASK-037, T-09)", () => {
  it("estado carregando: botão em processamento e campos desabilitados durante o envio", async () => {
    const user = userEvent.setup();
    const { gateway, resolver } = criarGatewayControlavel();
    render(<TelaEntrar gateway={gateway} />);

    await preencherLogin(user, "leitor@example.test", "senha-qualquer");
    await user.click(screen.getByRole("button", { name: "Entrar" }));

    expect(screen.getByRole("button", { name: "Entrando…" })).toBeDisabled();
    expect(screen.getByLabelText("E-mail")).toBeDisabled();

    resolver()?.({
      data: { user: null, session: null },
      error: { message: "Invalid login credentials", status: 400 },
    });
    await waitFor(() =>
      expect(screen.getByRole("button", { name: "Entrar" })).not.toBeDisabled(),
    );
  });

  it("estado de erro: credencial inválida não revela se o e-mail existe — mesma mensagem para senha errada e e-mail inexistente", async () => {
    const user = userEvent.setup();

    function gatewayComErro(): SignInGateway {
      return {
        signInWithPassword: vi.fn(async () => ({
          data: { user: null, session: null },
          error: { message: "Invalid login credentials", status: 400 },
        })),
        signInWithOtp: vi.fn(),
        resetPasswordForEmail: vi.fn(),
      } as unknown as SignInGateway;
    }

    const { unmount } = render(<TelaEntrar gateway={gatewayComErro()} />);
    await preencherLogin(user, "conta-existente@example.test", "senha-errada");
    await user.click(screen.getByRole("button", { name: "Entrar" }));
    const mensagemSenhaErrada = await screen.findByRole("alert");
    const textoSenhaErrada = mensagemSenhaErrada.textContent;
    expect(textoSenhaErrada).toContain("E-mail ou senha incorretos.");
    expect(textoSenhaErrada?.toLowerCase()).not.toContain("não encontrado");
    expect(textoSenhaErrada?.toLowerCase()).not.toContain("não existe");
    unmount();

    render(<TelaEntrar gateway={gatewayComErro()} />);
    await preencherLogin(user, "nao-cadastrado@example.test", "qualquer-coisa");
    await user.click(screen.getByRole("button", { name: "Entrar" }));
    const mensagemEmailInexistente = await screen.findByRole("alert");

    expect(mensagemEmailInexistente.textContent).toBe(textoSenhaErrada);
  });

  it("estado de sucesso (login): mostra sessão ativa e chama onSignedIn", async () => {
    const user = userEvent.setup();
    const onSignedIn = vi.fn();
    const gateway: SignInGateway = {
      signInWithPassword: vi.fn(async () => ({
        data: {
          user: { id: "user-1" },
          session: { access_token: "at-1", refresh_token: "rt-1" },
        },
        error: null,
      })),
      signInWithOtp: vi.fn(),
      resetPasswordForEmail: vi.fn(),
    } as unknown as SignInGateway;

    render(<TelaEntrar gateway={gateway} onSignedIn={onSignedIn} />);
    await preencherLogin(user, "leitor@example.test", "senha-correta");
    await user.click(screen.getByRole("button", { name: "Entrar" }));

    expect(await screen.findByRole("status")).toHaveTextContent("Sessão ativa");
    expect(onSignedIn).toHaveBeenCalledWith({
      userId: "user-1",
      accessToken: "at-1",
      refreshToken: "rt-1",
    });
  });

  it("estado de sucesso (recuperação): mensagem genérica idêntica para e-mail existente e inexistente", async () => {
    const user = userEvent.setup();

    async function solicitarRecuperacao(erroDoGateway: { message: string; status: number } | null) {
      const gateway: SignInGateway = {
        signInWithPassword: vi.fn(),
        signInWithOtp: vi.fn(),
        resetPasswordForEmail: vi.fn(async () => ({ data: {}, error: erroDoGateway })),
      } as unknown as SignInGateway;
      const { unmount } = render(<TelaEntrar gateway={gateway} />);
      await user.click(screen.getByRole("button", { name: "Esqueci minha senha" }));
      await user.type(screen.getByLabelText("E-mail"), "qualquer@example.test");
      await user.click(screen.getByRole("button", { name: "Enviar instruções" }));
      const mensagem = await screen.findByRole("status");
      const texto = mensagem.textContent;
      unmount();
      return texto;
    }

    const textoComConta = await solicitarRecuperacao(null);
    const textoSemConta = await solicitarRecuperacao({ message: "Unable to find user", status: 400 });

    expect(textoComConta).toContain("Se esse e-mail existir");
    expect(textoComConta).toBe(textoSemConta);
  });

  it("link mágico: alterna para o modo, envia, e mostra confirmação genérica", async () => {
    const user = userEvent.setup();
    const gateway: SignInGateway = {
      signInWithPassword: vi.fn(),
      signInWithOtp: vi.fn(async () => ({ data: {}, error: null })),
      resetPasswordForEmail: vi.fn(),
    } as unknown as SignInGateway;

    render(<TelaEntrar gateway={gateway} />);
    await user.click(screen.getByRole("button", { name: "Entrar com link por e-mail" }));
    await user.type(screen.getByLabelText("E-mail"), "leitor@example.test");
    await user.click(screen.getByRole("button", { name: "Enviar link mágico" }));

    expect(await screen.findByRole("status")).toHaveTextContent("Se esse e-mail existir");
    expect(gateway.signInWithOtp).toHaveBeenCalledWith({
      email: "leitor@example.test",
      options: { shouldCreateUser: false },
    });
  });

  it("não renderiza nenhum CAPTCHA/iframe de verificação anti-robô (UX-SPEC 3.3.8)", () => {
    const gateway: SignInGateway = {
      signInWithPassword: vi.fn(),
      signInWithOtp: vi.fn(),
      resetPasswordForEmail: vi.fn(),
    } as unknown as SignInGateway;
    render(<TelaEntrar gateway={gateway} />);
    expect(document.querySelector("iframe")).toBeNull();
    expect(screen.queryByText(/captcha/i)).toBeNull();
  });
});
