/**
 * TASK-039 (Lote 6 — Identidade), Tela T-13 "Baixar meus dados" (CA-10.4).
 *
 * Aciona o download de um arquivo JSON no navegador a partir de dado
 * arbitrário — `Blob` + `URL.createObjectURL` + um `<a download>` sintético,
 * sem nenhum SDK de terceiro (DI-15). Função de efeito colateral puro sobre o
 * DOM: recebe o objeto já pronto (ver `collectDataExportPayload`,
 * `export-data.ts`) e não conhece nada sobre progresso/preferências/esboços.
 */
export function downloadJsonFile(data: unknown, filename: string): void {
  const json = JSON.stringify(data, null, 2);
  const blob = new Blob([json], { type: "application/json" });
  const url = URL.createObjectURL(blob);

  const anchor = document.createElement("a");
  anchor.href = url;
  anchor.download = filename;
  anchor.rel = "noopener";
  document.body.appendChild(anchor);
  anchor.click();
  document.body.removeChild(anchor);

  URL.revokeObjectURL(url);
}

/** Nome de arquivo padrão da exportação de dados de T-13, com data (AAAA-MM-DD) embutida. */
export function buildExportFilename(now: Date = new Date()): string {
  const isoDate = now.toISOString().slice(0, 10);
  return `meus-dados-estudobiblico-${isoDate}.json`;
}
