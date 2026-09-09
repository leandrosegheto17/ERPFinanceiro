// @vitest-environment jsdom
/**
 * TASK-036 — teste de `TelaCriarConta` cobrindo os 4 estados de UX-SPEC §4
 * (DI-06: vazio/carregando/erro/sucesso) e a ordem "consent antes de
 * qualquer dado pessoal" (via `signUpWithConsent`, já coberta em unidade
 * por `sign-up-with-consent.test.ts` — aqui confirmamos que a tela usa essa
 * orquestração e reage corretamente a cada desfecho dela).
 */
import "@testing-library/jest-dom/vitest";
import { afterEach, describe, expect, it, vi } from "vitest";
import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";

import type { AuthGateway } from "./auth-gateway";
import { TelaCriarConta } from "./TelaCriarConta";

afterEach(cleanup);

type SignUpResponse = Awaited<ReturnType<AuthGateway["signUp"]>>;

function authGatewayReturning(response: SignUpResponse | (() => Promise<SignUpResponse>)): AuthGateway {
  return {
    signUp: async () => (typeof response === "function" ? response() : response),
    verifyOtp: async () => {
      throw new Error("não deveria ser chamado por TelaCriarConta");
    },
  };
}

async function preencherFormulario(user: ReturnType<typeof userEvent.setup>) {
  await user.type(screen.getByLabelText("E-mail"), "leitor@example.test");
  await user.type(screen.getByLabelText("Senha"), "senha-forte-9f8e7d6c!");
  await user.click(screen.getByRole("switch"));
}

describe("TelaCriarConta (TASK-036) — estado vazio", () => {
  it("renderiza os campos e o botão desabilitado com motivo em texto (consentimento não marcado)", () => {
    render(
      <TelaCriarConta
        authGateway={authGatewayReturning({} as SignUpResponse)}
      />,
    );
    expect(screen.getByLabelText("E-mail")).toBeInTheDocument();
    expect(screen.getByLabelText("Senha")).toBeInTheDocument();
    const botao = screen.getByRole("button", { name: "Criar conta" });
    expect(botao).toBeDisabled();
    expect(screen.getByText(/marque o consentimento acima/i)).toBeInTheDocument();
  });

  it("habilita o botão assim que o consentimento é marcado", async () => {
    const user = userEvent.setup();
    render(<TelaCriarConta authGateway={authGatewayReturning({} as SignUpResponse)} />);
    await user.click(screen.getByRole("switch"));
    expect(screen.getByRole("button", { name: "Criar conta" })).toBeEnabled();
  });
});

describe("TelaCriarConta (TASK-036) — estado carregando", () => {
  it("bloqueia os campos e mostra o botão em processamento enquanto aguarda o resultado", async () => {
    const user = userEvent.setup();
    let resolveSignUp: (value: SignUpResponse) => void = () => {};
    const pendente = new Promise<SignUpResponse>((resolve) => {
      resolveSignUp = resolve;
    });
    const authGateway = authGatewayReturning(() => pendente);

    render(<TelaCriarConta authGateway={authGateway} />);
    await preencherFormulario(user);
    await user.click(screen.getByRole("button", { name: "Criar conta" }));

    expect(screen.getByRole("button", { name: /criando conta/i })).toBeDisabled();
    expect(screen.getByLabelText("E-mail")).toBeDisabled();
    expect(screen.getByLabelText("Senha")).toBeDisabled();

    resolveSignUp({
      data: { user: { id: "user-1" }, session: null },
      error: null,
    } as SignUpResponse);
    await waitFor(() => expect(screen.getByText("Conta criada")).toBeInTheDocument());
  });
});

describe("TelaCriarConta (TASK-036) — estado erro", () => {
  it("e-mail já cadastrado: mensagem própria e foco movido ao campo de e-mail", async () => {
    const user = userEvent.setup();
    const authGateway = authGatewayReturning({
      data: { user: null, session: null },
      error: { message: "User already registered", status: 422 },
    } as SignUpResponse);

    render(<TelaCriarConta authGateway={authGateway} />);
    await preencherFormulario(user);
    await user.click(screen.getByRole("button", { name: "Criar conta" }));

    const alerta = await screen.findByText(/já tem uma conta cadastrada/i);
    expect(alerta).toBeInTheDocument();
    await waitFor(() => expect(screen.getByLabelText("E-mail")).toHaveFocus());
  });

  it("senha fraca: mensagem própria e foco movido ao campo de senha", async () => {
    const user = userEvent.setup();
    const authGateway = authGatewayReturning({
      data: { user: null, session: null },
      error: { message: "Password should be at least 6 characters", status: 422 },
    } as SignUpResponse);

    render(<TelaCriarConta authGateway={authGateway} />);
    await preencherFormulario(user);
    await user.click(screen.getByRole("button", { name: "Criar conta" }));

    await screen.findByText(/senha mais forte/i);
    await waitFor(() => expect(screen.getByLabelText("Senha")).toHaveFocus());
  });

  it("sem rede (exceção lançada pelo gateway): mensagem própria, sem quebrar a tela", async () => {
    const user = userEvent.setup();
    const authGateway: AuthGateway = {
      signUp: async () => {
        throw new TypeError("Failed to fetch");
      },
      verifyOtp: async () => {
        throw new Error("não deveria ser chamado");
      },
    };

    render(<TelaCriarConta authGateway={authGateway} />);
    await preencherFormulario(user);
    await user.click(screen.getByRole("button", { name: "Criar conta" }));

    const alerta = await screen.findByRole("alert");
    expect(alerta).toHaveTextContent(/sem conexão/i);
  });

  it("consentimento não marcado (submissão direta do form, ignorando o disabled do botão): mensagem e foco no controle de consentimento", async () => {
    const authGateway = authGatewayReturning({} as SignUpResponse);
    const { container } = render(
      <TelaCriarConta authGateway={authGateway} />,
    );

    const form = container.querySelector("form");
    expect(form).not.toBeNull();
    fireEvent.submit(form as HTMLFormElement);

    await screen.findByText(/marque a caixa de consentimento/i);
    await waitFor(() => expect(screen.getByRole("switch")).toHaveFocus());
  });
});

describe("TelaCriarConta (TASK-036) — estado sucesso", () => {
  it("mostra confirmação de e-mail pendente e, quando informado, o estado de migração de progresso (RN-07)", async () => {
    const user = userEvent.setup();
    const authGateway = authGatewayReturning({
      data: { user: { id: "user-1" }, session: null },
      error: null,
    } as SignUpResponse);
    const aoConcluir = vi.fn();

    render(
      <TelaCriarConta
        authGateway={authGateway}
        diasDeProgressoParaMigrar={3}
        onCadastroConcluido={aoConcluir}
      />,
    );
    await preencherFormulario(user);
    await user.click(screen.getByRole("button", { name: "Criar conta" }));

    await screen.findByText("Conta criada");
    expect(screen.getByText(/link de confirmação/i)).toBeInTheDocument();
    expect(screen.getByText(/seus 3 dias foram guardados na sua conta/i)).toBeInTheDocument();
    expect(aoConcluir).toHaveBeenCalledWith({
      userId: "user-1",
      requerConfirmacaoDeEmail: true,
    });
  });

  it("não mostra estado de migração quando diasDeProgressoParaMigrar não é informado", async () => {
    const user = userEvent.setup();
    const authGateway = authGatewayReturning({
      data: {
        user: { id: "user-2" },
        session: { access_token: "at-2", refresh_token: "rt-2" },
      },
      error: null,
    } as SignUpResponse);

    render(<TelaCriarConta authGateway={authGateway} />);
    await preencherFormulario(user);
    await user.click(screen.getByRole("button", { name: "Criar conta" }));

    await screen.findByText("Conta criada");
    expect(screen.queryByText(/dias foram guardados/i)).not.toBeInTheDocument();
  });
});
