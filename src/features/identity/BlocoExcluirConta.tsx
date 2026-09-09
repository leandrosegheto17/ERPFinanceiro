/**
 * TASK-040 — Tela T-13 (Conta e dados), UX-SPEC T-13/§4/§5.1: bloco de
 * exclusão de conta.
 *
 * Este componente é deliberadamente um **subcomponente independente**, não
 * a tela T-13 inteira — a outra metade de T-13 (TASK-039, "Baixar meus
 * dados" em JSON) é implementada por outra instância do Executor em
 * paralelo, em componente próprio (ex.: `BlocoExportarDados.tsx`). Uma
 * futura `TelaContaEDados.tsx` (fora do escopo de ambas as tarefas) compõe
 * os dois blocos lado a lado, na mesma página, resolvendo `userId`/
 * gateways a partir da sessão (TASK-038) e passando para cada bloco — isso
 * evita as duas tarefas em paralelo editarem o mesmo arquivo de tela.
 *
 * Fricção deliberada (UX-SPEC T-13: "confirmação digitando a palavra
 * 'EXCLUIR'"): o botão de confirmação só habilita quando o texto digitado
 * é exatamente igual a `PALAVRA_DE_CONFIRMACAO` — nunca uma exclusão de um
 * clique.
 *
 * `due_at` exibido nunca é calculado no cliente — vem sempre da resposta do
 * servidor (`requestAccountErasure`/`getErasureRequestStatus`, trigger de
 * TASK-032). O UX-SPEC de T-13 não prevê opção de cancelamento do pedido de
 * exclusão ("Após confirmar, a conta entra em estado 'exclusão solicitada'
 * com a data-limite visível" — sem menção a cancelar), então este
 * componente não inventa uma.
 *
 * Quatro estados (DI-06, UX-SPEC §4 "T-13 Conta e dados"):
 * - **vazio**: N/A no UX-SPEC (a tela sempre mostra o formulário/estado
 *   atual) — mesmo padrão já adotado por `TelaEntrar` (TASK-037);
 * - **carregando**: verificando se já existe um pedido pendente (ao montar)
 *   ou enviando o pedido de exclusão;
 * - **erro**: falha ao verificar o estado ou ao enviar o pedido — "fica
 *   pendente e tenta de novo" (UX-SPEC), nunca perde o texto já digitado;
 * - **sucesso**: exclusão registrada, com a data-limite (`due_at`) visível.
 */
import { useEffect, useRef, useState } from "react";
import type { FormEvent } from "react";

import { Botao, CampoDeTexto } from "../../design-system/primitives";
import { PALAVRA_DE_CONFIRMACAO_EXCLUSAO } from "./erasure-confirmation-word";
import type { ErasureRequestGateway } from "./erasure-request-gateway";
import { getErasureRequestStatus } from "./get-erasure-request-status";
import { requestAccountErasure } from "./request-account-erasure";
import styles from "./BlocoExcluirConta.module.css";

export { PALAVRA_DE_CONFIRMACAO_EXCLUSAO };

type Estado =
  | { tipo: "carregando" }
  | { tipo: "formulario" }
  | { tipo: "erro"; mensagem: string }
  | { tipo: "sucesso"; requestedAt: string; dueAt: string };

export interface BlocoExcluirContaProps {
  erasureGateway: ErasureRequestGateway;
  userId: string;
}

function formatarData(iso: string): string {
  return new Date(iso).toLocaleDateString("pt-BR", {
    day: "2-digit",
    month: "long",
    year: "numeric",
  });
}

export function BlocoExcluirConta({ erasureGateway, userId }: BlocoExcluirContaProps) {
  const [estado, setEstado] = useState<Estado>({ tipo: "carregando" });
  const [confirmacao, setConfirmacao] = useState("");
  const erroRef = useRef<HTMLParagraphElement>(null);

  useEffect(() => {
    let cancelado = false;
    void (async () => {
      const result = await getErasureRequestStatus(erasureGateway, userId);
      if (cancelado) {
        return;
      }
      if (result.status === "requested") {
        setEstado({ tipo: "sucesso", requestedAt: result.requestedAt, dueAt: result.dueAt });
      } else if (result.status === "error") {
        setEstado({
          tipo: "erro",
          mensagem: "Não foi possível verificar o estado da sua conta agora. Tente novamente.",
        });
      } else {
        setEstado({ tipo: "formulario" });
      }
    })();
    return () => {
      cancelado = true;
    };
  }, [erasureGateway, userId]);

  useEffect(() => {
    if (estado.tipo === "erro") {
      erroRef.current?.focus();
    }
  }, [estado]);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (confirmacao !== PALAVRA_DE_CONFIRMACAO_EXCLUSAO) {
      return;
    }

    setEstado({ tipo: "carregando" });

    try {
      const result = await requestAccountErasure(erasureGateway, { userId });
      if (result.status === "error") {
        setEstado({
          tipo: "erro",
          mensagem:
            "Não foi possível enviar o pedido de exclusão agora. Seu pedido continua pendente — tente novamente.",
        });
        return;
      }
      setEstado({ tipo: "sucesso", requestedAt: result.requestedAt, dueAt: result.dueAt });
    } catch {
      setEstado({
        tipo: "erro",
        mensagem:
          "Sem conexão com a internet. Seu pedido de exclusão continua pendente — verifique sua rede e tente novamente.",
      });
    }
  }

  if (estado.tipo === "carregando") {
    return (
      <div className={styles.bloco} role="status">
        <p>Carregando…</p>
      </div>
    );
  }

  if (estado.tipo === "sucesso") {
    return (
      <div className={styles.bloco} role="status">
        <h2 className={styles.titulo}>Exclusão solicitada</h2>
        <p>
          Sua conta e seus dados serão apagados até{" "}
          <strong data-testid="erasure-due-at">{formatarData(estado.dueAt)}</strong>.
        </p>
        <p className={styles.apoio}>
          Pedido registrado em {formatarData(estado.requestedAt)}. Um backup de segurança pode ser
          retido por período adicional, sem uso ativo dos seus dados, apenas para recuperação de
          desastre.
        </p>
      </div>
    );
  }

  return (
    <form className={styles.bloco} onSubmit={handleSubmit} noValidate>
      <h2 className={styles.titulo}>Excluir conta e dados</h2>

      {estado.tipo === "erro" && (
        <p ref={erroRef} tabIndex={-1} role="alert" className={styles.erro}>
          <span aria-hidden="true" className={styles.erroIcone}>
            ⚠
          </span>
          <span>{estado.mensagem}</span>
        </p>
      )}

      <p>Esta ação apaga permanentemente, em até 15 dias, o que a sua conta guarda:</p>
      <ul className={styles.lista}>
        <li>Seu perfil e credenciais de acesso</li>
        <li>Seu progresso de leitura e preferências</li>
        <li>Seus esboços e anotações do púlpito</li>
      </ul>
      <p className={styles.apoio}>
        Um backup de segurança pode ser retido por período adicional, sem uso ativo dos seus
        dados, apenas para recuperação de desastre.
      </p>
      <p className={styles.aviso}>
        <span aria-hidden="true">⚠</span> Esta ação é irreversível depois de concluída.
      </p>

      <CampoDeTexto
        rotulo={`Digite "${PALAVRA_DE_CONFIRMACAO_EXCLUSAO}" para confirmar`}
        value={confirmacao}
        onChange={(event) => setConfirmacao(event.target.value)}
        autoComplete="off"
        autoCapitalize="characters"
      />

      <Botao
        type="submit"
        variante="perigo"
        disabled={confirmacao !== PALAVRA_DE_CONFIRMACAO_EXCLUSAO}
      >
        Excluir conta e dados
      </Botao>
      {confirmacao !== PALAVRA_DE_CONFIRMACAO_EXCLUSAO && (
        <p className={styles.motivoDesabilitado}>
          Digite exatamente &quot;{PALAVRA_DE_CONFIRMACAO_EXCLUSAO}&quot; acima para habilitar o
          botão.
        </p>
      )}
    </form>
  );
}
