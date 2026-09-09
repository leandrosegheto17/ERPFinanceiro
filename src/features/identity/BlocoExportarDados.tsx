/**
 * TASK-039 (Lote 6 — Identidade) — `BlocoExportarDados`, a parte de
 * **exportação** da Tela T-13 "Conta e dados" (UX-SPEC T-13, CA-10.4).
 *
 * Nota de composição (concorrência com TASK-040, mesma tela T-13): T-13 tem
 * duas seções — "Baixar meus dados" (esta, TASK-039) e "Excluir conta e
 * dados" (TASK-040, `erasure_request`/"EXCLUIR", implementada por outra
 * instância do Executor em paralelo). Para evitar as duas tarefas colidindo
 * no mesmo arquivo, cada uma vive em seu próprio subcomponente
 * (`BlocoExportarDados` aqui; o bloco de exclusão fica a cargo de TASK-040,
 * presumivelmente `BlocoExcluirConta`). Uma tarefa de composição posterior —
 * ou a instância que terminar por último entre TASK-039/TASK-040 — monta
 * `TelaContaDados.tsx`/`TelaT13.tsx` renderizando os dois blocos em
 * sequência (exportar acima, excluir abaixo, na ordem do wireframe do
 * UX-SPEC T-13). Este arquivo **não** cria essa tela composta.
 *
 * Estados (DI-06, UX-SPEC §4 "T-13 Conta e dados"): a própria tabela de
 * estados do UX-SPEC.md marca **Vazio: N/A** para T-13 (sem motivo escrito
 * na tabela) — motivo aplicado aqui, por escrito: a exportação sempre produz
 * um JSON estruturalmente válido, mesmo quando `progresso`/`preferencias`
 * estão vazios (o titular acabou de criar a conta) e `esbocos` é sempre `[]`
 * nesta fase (ver `export-data.ts`) — não existe estado distinto de "nada
 * para exportar", porque o arquivo sempre existe e sempre é válido. Os
 * outros três estados são implementados literalmente como o UX-SPEC.md
 * define: **carregando** ("Preparando exportação"), **erro** ("Exportação
 * falhou") e **sucesso** ("Arquivo baixado").
 */
import { useState } from "react";

import type { AppDatabase } from "../../core/storage";
import { Botao } from "../../design-system/primitives";
import { buildExportFilename, downloadJsonFile } from "./download-json";
import { collectDataExportPayload } from "./export-data";
import styles from "./BlocoExportarDados.module.css";

type Estado =
  | { tipo: "pronto" }
  | { tipo: "carregando" }
  | { tipo: "erro"; mensagem: string }
  | { tipo: "sucesso"; nomeArquivo: string };

export interface BlocoExportarDadosProps {
  /** Banco local (`core/storage`) do titular logado neste aparelho. */
  db: AppDatabase;
  /**
   * Ponto de injeção para teste (evita depender do `<a download>` real);
   * padrão: `downloadJsonFile` real (Blob + clique sintético). Pode devolver
   * uma `Promise` — o estado "carregando" permanece até ela resolver.
   */
  onDownload?: (data: unknown, filename: string) => void | Promise<void>;
}

export function BlocoExportarDados({ db, onDownload = downloadJsonFile }: BlocoExportarDadosProps) {
  const [estado, setEstado] = useState<Estado>({ tipo: "pronto" });
  const carregando = estado.tipo === "carregando";

  async function handleExportar() {
    setEstado({ tipo: "carregando" });
    try {
      const payload = await collectDataExportPayload(db);
      const nomeArquivo = buildExportFilename();
      await onDownload(payload, nomeArquivo);
      setEstado({ tipo: "sucesso", nomeArquivo });
    } catch {
      setEstado({
        tipo: "erro",
        mensagem: "Não conseguimos preparar seus dados agora. Tente novamente em instantes.",
      });
    }
  }

  return (
    <section className={styles.bloco} aria-labelledby="bloco-exportar-dados-titulo">
      <h2 id="bloco-exportar-dados-titulo" className={styles.titulo}>
        Baixar meus dados
      </h2>
      <p className={styles.descricao}>
        Baixa um arquivo com o seu progresso de leitura, suas preferências e seus esboços, em
        formato JSON.
      </p>

      <Botao type="button" onClick={handleExportar} disabled={carregando}>
        {carregando ? "Preparando exportação…" : "Exportar meus dados"}
      </Botao>

      {estado.tipo === "erro" && (
        <p className={styles.erro} role="alert">
          <span aria-hidden="true" className={styles.erroIcone}>
            ⚠
          </span>
          <span>{estado.mensagem}</span>
        </p>
      )}

      {estado.tipo === "sucesso" && (
        <p className={styles.sucesso} role="status">
          <span aria-hidden="true" className={styles.sucessoIcone}>
            ✓
          </span>
          <span>Arquivo baixado ({estado.nomeArquivo}).</span>
        </p>
      )}
    </section>
  );
}
