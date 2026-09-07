/**
 * Tipos compartilhados do conteúdo editorial estático (TASK-012, SDD.md §5.1/§5.3,
 * PRD-TECNICO.md RN-01/RN-04/RN-13): `Pericope`, `Note`, `PlanDay`, `Plan`.
 *
 * Consumidores previstos (DI-02, ADR-001/ADR-012 — direção de dependência
 * unidirecional verificada por `no-tools-from-core`/`no-features-from-core`):
 *  - `tools/content-validate` (TASK-013/014, roda em Node/build): importa **daqui**
 *    para validar C1–C11 (RN-01) e a cobertura/formato de nota (RN-13). `tools/*`
 *    pode importar de `core/*` — só a direção oposta é proibida.
 *  - `core/content` (TASK-015, roda no bundle do navegador): módulo irmão deste
 *    arquivo, mesma pasta.
 *
 * `CorpusManifest` (SDD.md §5.3) é um tipo **distinto** e já existe em
 * `src/tools/corpus-import/manifest-types.ts` (TASK-008) — mesma entidade que o
 * diagrama ER do SDD.md §5.1 (proveniência/licença do corpus bíblico bruto), não
 * duplicada nem renomeada aqui. Ele não é reexportado deste módulo porque:
 *  - nenhum critério de aceite de TASK-013/014/015 depende de `CorpusManifest`
 *    (C1–C11 e RN-13 não referenciam proveniência/licença do corpus);
 *  - `core/content` (runtime do navegador) não pode importar de
 *    `tools/corpus-import` (regra `no-tools-from-core`, DI-02); quem eventualmente
 *    precisar de `CorpusManifest` em runtime é `core/corpus` (atribuição derivada
 *    do manifesto, SDD.md §2.2, TASK-045), não `core/content`;
 *  - se `tools/content-validate` precisar de `CorpusManifest` no futuro, importa
 *    diretamente de `src/tools/corpus-import/manifest-types.ts` (tools → tools,
 *    direção permitida) — sem passar por `core/*`.
 *
 * As perícopes/notas em si (RN-13) são reutilizáveis em três contextos (dia do
 * plano, leitura livre, auto-embed do Módulo 2) — por isso `Pericope`/`Note` não
 * carregam nenhuma referência ao plano; a relação é o inverso (`PlanDay.noteIds`).
 */

/** Intervalo contínuo de texto bíblico (livro + capítulo:versículo inicial → final). */
export interface ScriptureRange {
  readonly bookId: string;
  readonly startChapter: number;
  readonly startVerse: number;
  readonly endChapter: number;
  readonly endVerse: number;
}

/**
 * Unidade de sentido (RN-13): intervalo contínuo, com título curto. Nunca
 * aparece nomeada como "perícope" na interface (SDD.md §5.4) — isso é
 * responsabilidade de camada de apresentação, não deste tipo.
 */
export interface Pericope extends ScriptureRange {
  readonly id: string;
  readonly title: string;
}

/**
 * Nota de Perícope (RF-06, RN-04, RN-13). `body` deve ter entre 120 e 200
 * palavras (CA-06.3) — a contagem/validação em si é responsabilidade de
 * `tools/content-validate` (TASK-014), não deste tipo. `reviewer`/`reviewedAt`
 * são opcionais porque nem toda nota tem revisor ainda (RN-04: "quando houver").
 */
export interface Note {
  readonly id: string;
  readonly pericopeId: string;
  readonly body: string;
  readonly author: string;
  readonly reviewer?: string;
  readonly reviewedAt?: string;
}

/** Categoria da porção de AT do dia (RN-01), fechada em 5 valores. */
export type AtCategory =
  | "narrativo"
  | "poetico"
  | "profetico"
  | "legal-ritual"
  | "genealogico";

/** Porção de AT do dia: intervalo + categoria (RN-01, C2/C3/C8). */
export interface AtPortion {
  readonly range: ScriptureRange;
  readonly category: AtCategory;
}

/**
 * Dia do Plano Entrelaçado (RN-01, RN-04, RN-13). `hook` é opcional apenas
 * porque o dia 90 não tem gancho — tem a tela de conclusão (C11, RF-20).
 * `wordCount` é a soma de palavras das duas porções (C4: 1.800–3.200), já
 * calculada em build — este tipo não recalcula a contagem.
 *
 * `atWordCount`/`ntWordCount` (adicionados na TASK-013, opcionais) são a
 * quebra por trilha de `wordCount`, necessária para C5 (RN-01 — proporção de
 * palavras AT/NT medida sobre o plano inteiro, 50/50 a 65/35 a favor do AT).
 * São opcionais porque nem todo produtor de `ContentBundle` (ex.: fixture
 * parcial de teste) precisa fornecer a quebra por trilha — `tools/content-
 * validate` (TASK-013) trata a ausência em qualquer dia do plano como "C5 não
 * verificável mecanicamente para este bundle" em vez de simular uma checagem
 * vazia (ver `src/tools/content-validate/validate-content.ts`).
 */
export interface PlanDay {
  readonly dayNumber: number;
  readonly atPortion: AtPortion;
  readonly ntPortion: ScriptureRange;
  readonly noteIds: readonly string[];
  readonly hook?: string;
  readonly wordCount: number;
  readonly atWordCount?: number;
  readonly ntWordCount?: number;
}

/**
 * Relatório de validação de conteúdo (CA-02.5, C10). Shape mínimo e estável;
 * `tools/content-validate` (TASK-013/014) é quem produz o valor real e pode
 * precisar de mais detalhe por violação — este tipo não impede extensão futura
 * (ex.: violações tipadas por regra), só define o contrato mínimo que `Plan`
 * já expõe hoje.
 */
export interface ValidationReport {
  readonly passed: boolean;
  readonly checkedAt: string;
  readonly violations: readonly string[];
}

/**
 * Plano em si (RN-01). Deliberadamente **sem** array de dias embutido — SDD.md
 * §5.3 não lista `days` nos "campos essenciais" de `Plan`, e o relacionamento
 * `PLAN ||--o{ PLAN_DAY` do diagrama ER (§5.1) é modelado aqui como uma
 * coleção externa (`ContentBundle.days`), não como propriedade aninhada.
 */
export interface Plan {
  readonly id: string;
  readonly name: string;
  readonly dayCount: number;
  readonly validationReport: ValidationReport;
}

/**
 * Agregado de conveniência para quem opera sobre o conteúdo editorial inteiro
 * (`tools/content-validate` valida o conjunto; `core/content`/TASK-015 carrega
 * o bundle e resolve `PlanDay` por número). Não é uma entidade do SDD.md §5.3 —
 * é a composição das quatro entidades que o diagrama ER do §5.1 já relaciona
 * (`PLAN ||--o{ PLAN_DAY }o--o{ PERICOPE`, `NOTE ||--|| PERICOPE`), reunida aqui
 * para os dois consumidores não precisarem inventar a mesma forma duas vezes.
 */
export interface ContentBundle {
  readonly plan: Plan;
  readonly days: readonly PlanDay[];
  readonly pericopes: readonly Pericope[];
  readonly notes: readonly Note[];
}
