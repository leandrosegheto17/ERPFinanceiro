/**
 * TASK-026 — `Toast`: primitivo de notificação transitória não-modal, base
 * `@radix-ui/react-toast` (DI-15).
 *
 * Decisão documentada (critério de aceite da tarefa pede "armadilha de foco
 * e restauração, conforme o padrão real do componente Radix"):
 *
 * 1. **Sem armadilha de foco** — Toast não é modal (ao contrário de
 *    `Dialogo`/`FolhaInferior`). Confirmado no código-fonte do Radix
 *    (`node_modules/@radix-ui/react-toast/dist/index.mjs`, `ToastViewport`):
 *    a região do toast usa dois sentinelas (`FocusProxy`, "head"/"tail")
 *    que devolvem o foco à ordem de tabulação normal da página assim que o
 *    usuário tabula para além do último elemento focável do toast — não há
 *    loop interno. Abrir um toast também não move o foco para dentro dele
 *    (não é modal): o elemento que disparou a ação continua focado até o
 *    usuário decidir entrar na região do toast (tecla de atalho padrão do
 *    Radix, F8, ou tabulação natural). `Toast.test.tsx` prova as duas
 *    pontas: nenhum roubo de foco ao abrir, e nenhuma prisão de foco ao
 *    tabular a partir de dentro.
 * 2. **Restauração de foco não é automática no Radix Toast** — diferente do
 *    Dialog, o `handleClose` interno do Radix Toast só move o foco para o
 *    `viewport` (não para o elemento que abriu o toast) quando o fecha
 *    enquanto o foco está dentro dele (mesmo arquivo, `ToastImpl`,
 *    `isFocusInToast`). Como o critério de aceite pede restauração "em cada
 *    primitivo que fizer sentido", e aqui *faz* sentido pelo menos para o
 *    caso em que o usuário efetivamente moveu o foco para dentro do toast
 *    (ex.: para fechar via botão), este wrapper adiciona a restauração
 *    manual: guarda o elemento focado no instante em que o toast abre e,
 *    ao fechar, se o foco não estiver mais nesse elemento (sinal de que o
 *    usuário entrou na região do toast), devolve o foco a ele.
 */
import { useEffect, useRef } from "react";
import * as RadixToast from "@radix-ui/react-toast";
import "./overlays.css";

export interface ToastProviderProps {
  children: React.ReactNode;
  /** Duração padrão (ms) antes do fechamento automático de um toast em primeiro plano. */
  duration?: number;
}

/** Envolve a árvore da aplicação (ou de uma feature) que pode disparar `Toast`. */
export function ToastProvider({ children, duration = 5000 }: ToastProviderProps) {
  return (
    <RadixToast.Provider duration={duration} swipeDirection="right">
      {children}
      <RadixToast.Viewport className="eb-toast-viewport" label="Notificações ({hotkey})" />
    </RadixToast.Provider>
  );
}

export type TipoDeToast = "info" | "sucesso" | "erro";

// DI-07: nenhuma informação de estado só por cor — cada tipo tem ícone e
// texto (aria-hidden no ícone; o texto do título/descrição carrega o
// significado para leitor de tela).
const ICONE_POR_TIPO: Record<TipoDeToast, string> = {
  info: "ℹ",
  sucesso: "✓",
  erro: "!",
};

export interface ToastProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  titulo: string;
  descricao?: string;
  tipo?: TipoDeToast;
  acaoRotulo?: string;
  onAcao?: () => void;
  /** Rótulo acessível do botão fechar (DI-08: alvo de toque mínimo 44×44px). */
  fecharRotulo?: string;
}

export function Toast({
  open,
  onOpenChange,
  titulo,
  descricao,
  tipo = "info",
  acaoRotulo,
  onAcao,
  fecharRotulo = "Fechar notificação",
}: ToastProps) {
  const focoAoAbrirRef = useRef<HTMLElement | null>(null);

  useEffect(() => {
    if (open) {
      focoAoAbrirRef.current =
        document.activeElement instanceof HTMLElement ? document.activeElement : null;
    }
  }, [open]);

  function aoMudarAbertura(proximoAberto: boolean) {
    if (!proximoAberto) {
      const elementoParaRestaurar = focoAoAbrirRef.current;
      // Só restaura se o foco de fato saiu do elemento que abriu o toast
      // (sinal de que o usuário entrou na região do toast) — se o foco
      // nunca saiu de lá (toast fechado por timeout sem interação
      // nenhuma), não há nada a restaurar (DI: nunca mover foco que o
      // usuário não pediu para mover).
      if (
        elementoParaRestaurar &&
        document.contains(elementoParaRestaurar) &&
        document.activeElement !== elementoParaRestaurar
      ) {
        elementoParaRestaurar.focus();
      }
      focoAoAbrirRef.current = null;
    }
    onOpenChange(proximoAberto);
  }

  return (
    <RadixToast.Root
      open={open}
      onOpenChange={aoMudarAbertura}
      className="eb-toast-root"
      type={tipo === "erro" ? "foreground" : "background"}
    >
      <span className="eb-toast-icone" aria-hidden="true">
        {ICONE_POR_TIPO[tipo]}
      </span>
      <RadixToast.Title className="eb-toast-titulo">{titulo}</RadixToast.Title>
      {descricao ? (
        <RadixToast.Description className="eb-toast-descricao">{descricao}</RadixToast.Description>
      ) : null}
      {acaoRotulo && onAcao ? (
        <RadixToast.Action altText={acaoRotulo} asChild>
          <button type="button" className="eb-toast-acao" onClick={onAcao}>
            {acaoRotulo}
          </button>
        </RadixToast.Action>
      ) : null}
      <RadixToast.Close aria-label={fecharRotulo} className="eb-toast-fechar">
        ×
      </RadixToast.Close>
    </RadixToast.Root>
  );
}
