/**
 * TASK-026 — `FolhaInferior` (bottom sheet): primitivo de sobreposição
 * modal ancorada na base da tela, base `@radix-ui/react-dialog` (DI-15) —
 * Radix não tem um primitivo de "bottom sheet" próprio; a prática
 * documentada é construir sobre `Dialog`, só variando o posicionamento
 * visual do `Content` (mesma armadilha/restauração de foco do `Dialogo`).
 *
 * UX-SPEC §5.2 (T-07): "Folha inferior sobre a leitura — `aria-modal`,
 * armadilha de foco, Esc fecha, foco retorna ao botão de origem." Vêm do
 * Radix Dialog (ver comentário equivalente em `Dialogo.tsx`, incl. a nota
 * de que `aria-modal="true"` precisa ser aplicado explicitamente nesta
 * versão do pacote); esta variante só muda `className` (CSS ancora embaixo,
 * largura cheia, cantos superiores arredondados) e adiciona a alça visual
 * decorativa (`aria-hidden`, não é um controle).
 *
 * Mesma ressalva de `Dialogo.tsx`: restauração automática de foco depende
 * de abrir via a prop `trigger` (`Dialog.Trigger`), porque o
 * `onCloseAutoFocus` padrão do Radix foca especificamente `triggerRef`.
 */
import type { ReactNode } from "react";
import * as RadixDialog from "@radix-ui/react-dialog";
import "./overlays.css";

export interface FolhaInferiorProps {
  open?: boolean;
  defaultOpen?: boolean;
  onOpenChange?: (open: boolean) => void;
  trigger?: ReactNode;
  titulo: string;
  descricao?: string;
  children?: ReactNode;
  /** Rótulo acessível do botão fechar (DI-08: alvo de toque mínimo 44×44px). */
  fecharRotulo?: string;
}

export function FolhaInferior({
  open,
  defaultOpen,
  onOpenChange,
  trigger,
  titulo,
  descricao,
  children,
  fecharRotulo = "Fechar",
}: FolhaInferiorProps) {
  return (
    <RadixDialog.Root open={open} defaultOpen={defaultOpen} onOpenChange={onOpenChange}>
      {trigger ? <RadixDialog.Trigger asChild>{trigger}</RadixDialog.Trigger> : null}
      <RadixDialog.Portal>
        <RadixDialog.Overlay className="eb-overlay-backdrop" />
        <RadixDialog.Content className="eb-folha-content" aria-modal="true">
          <div className="eb-folha-alca" aria-hidden="true" />
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
