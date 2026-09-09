/**
 * TASK-026 — `Dialogo`: primitivo de sobreposição modal centralizada, base
 * `@radix-ui/react-dialog` (DI-15 — biblioteca obrigatória para overlays,
 * proibido construir do zero).
 *
 * Armadilha de foco (Tab não escapa enquanto aberto) é comportamento padrão
 * do `Dialog.Content` do Radix (`FocusScope` interno, `modal` por padrão
 * `true`) — não reimplementado aqui.
 *
 * Restauração de foco: o `onCloseAutoFocus` padrão do Radix Dialog foca
 * especificamente `context.triggerRef` (o elemento `Dialog.Trigger`), não
 * "o que estava focado antes" de forma genérica (conferido em
 * `node_modules/@radix-ui/react-dialog/dist/index.mjs`, `DialogContent`) —
 * por isso este componente sempre usa a prop `trigger` (renderizada via
 * `Dialog.Trigger asChild`) como o padrão de uso esperado, para que a
 * restauração automática do Radix funcione sem código adicional. Se o
 * diálogo for aberto por controle externo sem `trigger`, a restauração de
 * foco passa a ser responsabilidade de quem controla `open`/`onOpenChange`
 * (fora do escopo deste primitivo).
 *
 * UX-SPEC §5.2 (T-07): `aria-modal`, armadilha de foco, Esc fecha, foco
 * volta ao botão de origem. Armadilha/Esc/restauração vêm do Radix quando
 * `trigger` é usado; `aria-modal="true"` **não** vem por padrão nesta
 * versão do `@radix-ui/react-dialog` (confirmado em
 * `node_modules/@radix-ui/react-dialog/dist/index.mjs` — só seta
 * `role="dialog"`), por isso é aplicado explicitamente aqui em
 * `Dialog.Content`.
 */
import type { ReactNode } from "react";
import * as RadixDialog from "@radix-ui/react-dialog";
import "./overlays.css";

export interface DialogoProps {
  /** Controla abertura de fora (padrão recomendado — mesmo estilo de `open`/`onOpenChange` do Radix). */
  open?: boolean;
  defaultOpen?: boolean;
  onOpenChange?: (open: boolean) => void;
  /** Elemento que abre o diálogo (opcional — o diálogo também pode ser aberto só por `open`/estado externo). */
  trigger?: ReactNode;
  titulo: string;
  descricao?: string;
  children?: ReactNode;
  /** Rótulo acessível do botão fechar (DI-08: alvo de toque mínimo 44×44px). */
  fecharRotulo?: string;
}

export function Dialogo({
  open,
  defaultOpen,
  onOpenChange,
  trigger,
  titulo,
  descricao,
  children,
  fecharRotulo = "Fechar",
}: DialogoProps) {
  return (
    <RadixDialog.Root open={open} defaultOpen={defaultOpen} onOpenChange={onOpenChange}>
      {trigger ? <RadixDialog.Trigger asChild>{trigger}</RadixDialog.Trigger> : null}
      <RadixDialog.Portal>
        <RadixDialog.Overlay className="eb-overlay-backdrop" />
        <RadixDialog.Content className="eb-dialogo-content" aria-modal="true">
          <RadixDialog.Title className="eb-overlay-title">{titulo}</RadixDialog.Title>
          {descricao ? (
            <RadixDialog.Description className="eb-overlay-description">
              {descricao}
            </RadixDialog.Description>
          ) : null}
          {children}
          <RadixDialog.Close asChild>
            <button type="button" className="eb-overlay-close" aria-label={fecharRotulo}>
              ×
            </button>
          </RadixDialog.Close>
        </RadixDialog.Content>
      </RadixDialog.Portal>
    </RadixDialog.Root>
  );
}
