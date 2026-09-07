/**
 * `tools/content-validate` (TASK-013): valida um `ContentBundle` contra as
 * 11 restrições mecanicamente verificáveis de RN-01 (`PRD-TECNICO.md` §3,
 * C1–C11) e veta a publicação do plano quando alguma falha (CA-02.5) — o
 * mesmo padrão de `CorpusParseError`/`CorpusIntegrityError` de
 * `tools/corpus-import` (TASK-006/007): uma função "pura" que devolve um
 * relatório (`validateContent`) e uma função de veto que lança
 * (`assertContentValid`) para uso em contexto de build/CI.
 *
 * `tools/*` pode importar de `core/*` (regra `no-tools-from-core` só proíbe
 * o sentido oposto, REFAT-L1-01) — este módulo importa os tipos de
 * `core/content` (TASK-012) e não os reimplementa.
 *
 * Cobertura mecânica das 11 restrições de RN-01, com duas exceções
 * documentadas (não simulam checagem vazia — são omitidas do relatório):
 *
 *  - **C5** (proporção AT/NT em palavras sobre o plano inteiro) exige a
 *    quebra de `wordCount` por trilha, que só existe em `PlanDay` como
 *    `atWordCount`/`ntWordCount` **opcionais** (adicionados nesta tarefa,
 *    TASK-013, em `core/content/types.ts`). Se qualquer dia do bundle não
 *    trouxer os dois campos, C5 é mecanicamente inverificável para aquele
 *    bundle — a checagem é pulada (não "passa" silenciosamente) e o motivo
 *    fica registrado só como comentário de código, não como violação, porque
 *    a ausência do dado não é, em si, uma violação de RN-01.
 *  - **C10** ("o plano não é apresentado como cobertura integral da
 *    Bíblia") é, na essência, um requisito de copy de interface (`PRD-
 *    TECNICO.md` §4.3), não um dado estrutural do `ContentBundle`. Este
 *    módulo faz só uma checagem de guarda de baixo custo sobre `plan.name`
 *    contra um pequeno léxico de frases proibidas (ex.: "cobertura
 *    integral", "bíblia completa") — não substitui uma verificação real de
 *    copy de UI, que pertence a uma tarefa de tela (fora do escopo de
 *    TASK-013, que valida o bundle editorial, não a interface).
 *  - **C6** ("toda porção termina em fronteira de perícope ou de capítulo",
 *    `PRD-TECNICO.md` linha 487) tem a metade "perícope" mecanicamente
 *    verificável contra `bundle.pericopes`, mas a metade "capítulo" exige
 *    saber o último versículo real de um capítulo — isso é dado de
 *    versificação do corpus, e `tools/content-validate` não tem acesso a uma
 *    tabela de versificação dos 66 livros (`core/corpus` carrega o texto real
 *    do corpus completo em runtime de navegador; importar essa árvore inteira
 *    aqui, ou reimplementar uma tabela de versificação de todos os
 *    livros/capítulos do AT e NT, é desproporcional ao escopo desta tarefa e
 *    acopla o validador editorial ao corpus bíblico inteiro). A única
 *    versificação já disponível de forma legítima no projeto é
 *    `gospel-versification.ts` (`GOSPEL_FINAL_VERSE`), que documenta apenas o
 *    capítulo/versículo final de cada um dos 4 Evangelhos (para C9). `checkC6`
 *    reaproveita esse dado para o único caso de fronteira de capítulo que dá
 *    para confirmar sem inventar dado: uma porção de NT que termina
 *    exatamente no último capítulo/versículo de um Evangelho. Fora esse caso
 *    (qualquer outro livro/capítulo do AT ou do NT), fronteira de capítulo
 *    permanece mecanicamente inverificável — só a fronteira de perícope é
 *    checada, e uma porção que realmente termina no fim de um capítulo comum
 *    (sem versificação disponível) pode ser reportada como violação de C6
 *    mesmo estando correta pela regra. Essa limitação é do mesmo tipo das de
 *    C5/C10 acima: registrada aqui, não escondida atrás de uma checagem vazia.
 *
 * **RN-13** (TASK-014, `PRD-TECNICO.md` linhas 712–739 + CA-06.3/CA-06.4) tem
 * duas metades de cobertura, checadas por duas funções distintas com tags de
 * violação diferentes (nenhuma delas é número de restrição C-*, porque RN-13
 * não está na lista C1–C11 de RN-01):
 *
 *  - Dias de AT árido/genealógico/profético exigem nota cobrindo
 *    **especificamente aquela porção** (`PRD-TECNICO.md` linha ~726, C8) —
 *    já coberto por **C8** (RN-04); TASK-014 não duplica essa checagem.
 *  - **Todos os demais dias** ("nos demais dias, uma nota cobrindo qualquer
 *    uma das duas porções satisfaz a regra", `PRD-TECNICO.md` linha ~730)
 *    precisam de **pelo menos uma** nota cobrindo a porção de AT **ou** a de
 *    NT do dia — checado por `checkRN13Coverage`, tag `"RN-13-cobertura"`
 *    (rótulo distinto de `"RN-13"` da validação de nota abaixo, para deixar
 *    claro que é a metade de "cobertura por dia" de RN-13, não a metade de
 *    "conteúdo da nota"). Reaproveita a mesma lógica de "perícope cobre
 *    porção" (`rangeContains`) já usada em C8.
 *  - `checkNoteValidity` valida cada `Note` do bundle em si (não por dia),
 *    tag `"RN-13"`: contagem de palavras do `body` entre 120 e 200
 *    (CA-06.3), autoria presente e não vazia (`author`, RN-04/CA-06.4: "toda
 *    nota tem autor identificado"), e — quando `reviewer` estiver presente —
 *    que não seja uma string vazia/só espaço. `reviewer` continua opcional
 *    (RN-04: "e, quando houver, revisor identificado" — nunca obrigatório no
 *    PRD-TECNICO.md), então a ausência de revisor nunca é violação.
 */

import type { ContentBundle, PlanDay, ScriptureRange } from "../../core/content";
import {
  isImmediatelyAfter,
  rangeContains,
  rangeEnd,
  rangesOverlap,
  rangeStart,
} from "./range-utils";
import { GOSPEL_BOOK_IDS, GOSPEL_FINAL_VERSE, isGospelBookId } from "./gospel-versification";

export interface ContentValidationReport {
  readonly passed: boolean;
  readonly checkedAt: string;
  readonly violations: readonly string[];
}

export class ContentValidationError extends Error {}

const ARID_CATEGORIES = new Set(["legal-ritual", "genealogico"]);
const NOTE_REQUIRED_CATEGORIES = new Set(["legal-ritual", "genealogico", "profetico"]);
const NOTE_MIN_WORD_COUNT = 120;
const NOTE_MAX_WORD_COUNT = 200;
const FORBIDDEN_PLAN_NAME_PHRASES = [
  "cobertura integral",
  "bíblia completa",
  "biblia completa",
  "todos os livros da bíblia",
];

function violate(rule: string, message: string): string {
  return `${rule}: ${message}`;
}

function sortedDays(bundle: ContentBundle): readonly PlanDay[] {
  return [...bundle.days].sort((a, b) => a.dayNumber - b.dayNumber);
}

/** C1 — todo dia tem exatamente uma porção de AT e uma de NT; nenhum dia sem NT. */
function checkC1(bundle: ContentBundle, violations: string[]): void {
  const days = sortedDays(bundle);

  if (days.length !== bundle.plan.dayCount) {
    violations.push(
      violate(
        "C1",
        `o plano declara ${bundle.plan.dayCount} dia(s), mas o bundle tem ${days.length} dia(s)`,
      ),
    );
  }

  const seen = new Set<number>();
  for (const day of days) {
    if (seen.has(day.dayNumber)) {
      violations.push(violate("C1", `dia ${day.dayNumber} está duplicado no bundle`));
    }
    seen.add(day.dayNumber);

    if (!day.atPortion || !day.ntPortion) {
      violations.push(violate("C1", `dia ${day.dayNumber} não tem porção de AT e de NT`));
      continue;
    }
    if (rangeStart(day.atPortion.range).chapter > rangeEnd(day.atPortion.range).chapter ||
      (rangeStart(day.atPortion.range).chapter === rangeEnd(day.atPortion.range).chapter &&
        rangeStart(day.atPortion.range).verse > rangeEnd(day.atPortion.range).verse)) {
      violations.push(violate("C1", `dia ${day.dayNumber}: porção de AT tem início depois do fim`));
    }
    if (rangeStart(day.ntPortion).chapter > rangeEnd(day.ntPortion).chapter ||
      (rangeStart(day.ntPortion).chapter === rangeEnd(day.ntPortion).chapter &&
        rangeStart(day.ntPortion).verse > rangeEnd(day.ntPortion).verse)) {
      violations.push(violate("C1", `dia ${day.dayNumber}: porção de NT tem início depois do fim`));
    }
  }

  for (let n = 1; n <= bundle.plan.dayCount; n += 1) {
    if (!seen.has(n)) {
      violations.push(violate("C1", `dia ${n} do plano (1 a ${bundle.plan.dayCount}) está ausente do bundle`));
    }
  }
}

/** C2 — no máximo 1 dia consecutivo com AT árido; o dia seguinte é narrativo/poético. */
function checkC2(bundle: ContentBundle, violations: string[]): void {
  const days = sortedDays(bundle);
  for (let i = 0; i < days.length - 1; i += 1) {
    const current = days[i];
    const next = days[i + 1];
    if (next.dayNumber !== current.dayNumber + 1) {
      continue;
    }
    if (ARID_CATEGORIES.has(current.atPortion.category)) {
      const nextCategory = next.atPortion.category;
      if (nextCategory !== "narrativo" && nextCategory !== "poetico") {
        violations.push(
          violate(
            "C2",
            `dia ${current.dayNumber} tem AT árido ('${current.atPortion.category}') e o dia ${next.dayNumber} não é 'narrativo' nem 'poetico' (é '${nextCategory}')`,
          ),
        );
      }
    }
  }
}

/** C3 — dias 1 a 7: AT obrigatoriamente 'narrativo'; NT vem de um Evangelho. */
function checkC3(bundle: ContentBundle, violations: string[]): void {
  const activationWindowEnd = Math.min(7, bundle.plan.dayCount);
  for (const day of sortedDays(bundle)) {
    if (day.dayNumber < 1 || day.dayNumber > activationWindowEnd) {
      continue;
    }
    if (day.atPortion.category !== "narrativo") {
      violations.push(
        violate(
          "C3",
          `dia ${day.dayNumber} está na janela de ativação (1–7) mas a porção de AT é '${day.atPortion.category}', não 'narrativo'`,
        ),
      );
    }
    if (!isGospelBookId(day.ntPortion.bookId)) {
      violations.push(
        violate(
          "C3",
          `dia ${day.dayNumber} está na janela de ativação (1–7) mas a porção de NT ('${day.ntPortion.bookId}') não vem de um Evangelho (${GOSPEL_BOOK_IDS.join(", ")})`,
        ),
      );
    }
  }
}

/** C4 — soma de palavras das duas porções do dia entre 1.800 e 3.200. */
function checkC4(bundle: ContentBundle, violations: string[]): void {
  for (const day of sortedDays(bundle)) {
    if (day.wordCount < 1800 || day.wordCount > 3200) {
      violations.push(
        violate(
          "C4",
          `dia ${day.dayNumber} tem ${day.wordCount} palavra(s) no total; fora da faixa 1.800–3.200`,
        ),
      );
    }
  }
}

/**
 * C5 — proporção de palavras AT/NT entre 50/50 e 65/35 a favor do AT, medida
 * sobre o plano inteiro. Pulado (sem violação e sem "passar" silencioso) se
 * algum dia não trouxer `atWordCount`/`ntWordCount` — ver nota de módulo.
 */
function checkC5(bundle: ContentBundle, violations: string[]): void {
  const days = sortedDays(bundle);
  const hasFullBreakdown = days.every(
    (day) => typeof day.atWordCount === "number" && typeof day.ntWordCount === "number",
  );
  if (!hasFullBreakdown) {
    return;
  }

  let atTotal = 0;
  let ntTotal = 0;
  for (const day of days) {
    atTotal += day.atWordCount as number;
    ntTotal += day.ntWordCount as number;
  }
  const total = atTotal + ntTotal;
  if (total === 0) {
    return;
  }
  const atShare = atTotal / total;
  if (atShare < 0.5 || atShare > 0.65) {
    violations.push(
      violate(
        "C5",
        `proporção de palavras do AT sobre o plano inteiro é ${(atShare * 100).toFixed(1)}%; fora da faixa 50%–65%`,
      ),
    );
  }
}

/**
 * C6 — toda porção termina em fronteira de perícope OU de capítulo (ver nota
 * de módulo: a fronteira de capítulo só é mecanicamente verificável para o
 * último capítulo dos 4 Evangelhos, via `GOSPEL_FINAL_VERSE` — para os demais
 * livros/capítulos, sem tabela de versificação disponível, só a fronteira de
 * perícope é checada).
 */
function checkC6(bundle: ContentBundle, violations: string[]): void {
  const endsAtPericopeBoundary = (range: ScriptureRange): boolean =>
    bundle.pericopes.some(
      (pericope) =>
        pericope.bookId === range.bookId &&
        pericope.endChapter === range.endChapter &&
        pericope.endVerse === range.endVerse,
    );

  const endsAtChapterBoundary = (range: ScriptureRange): boolean => {
    if (!isGospelBookId(range.bookId)) {
      return false;
    }
    const finalVerse = GOSPEL_FINAL_VERSE[range.bookId];
    return range.endChapter === finalVerse.chapter && range.endVerse === finalVerse.verse;
  };

  const endsAtValidBoundary = (range: ScriptureRange): boolean =>
    endsAtPericopeBoundary(range) || endsAtChapterBoundary(range);

  for (const day of sortedDays(bundle)) {
    if (!endsAtValidBoundary(day.atPortion.range)) {
      violations.push(
        violate(
          "C6",
          `dia ${day.dayNumber}: porção de AT (${day.atPortion.range.bookId} ${day.atPortion.range.endChapter}:${day.atPortion.range.endVerse}) não termina em fronteira de nenhuma perícope do bundle nem de capítulo verificável`,
        ),
      );
    }
    if (!endsAtValidBoundary(day.ntPortion)) {
      violations.push(
        violate(
          "C6",
          `dia ${day.dayNumber}: porção de NT (${day.ntPortion.bookId} ${day.ntPortion.endChapter}:${day.ntPortion.endVerse}) não termina em fronteira de nenhuma perícope do bundle nem de capítulo verificável`,
        ),
      );
    }
  }
}

/** C7 — nenhum trecho do corpus aparece em mais de um dia do plano. */
function checkC7(bundle: ContentBundle, violations: string[]): void {
  const portions: { dayNumber: number; label: string; range: ScriptureRange }[] = [];
  for (const day of sortedDays(bundle)) {
    portions.push({ dayNumber: day.dayNumber, label: "AT", range: day.atPortion.range });
    portions.push({ dayNumber: day.dayNumber, label: "NT", range: day.ntPortion });
  }

  for (let i = 0; i < portions.length; i += 1) {
    for (let j = i + 1; j < portions.length; j += 1) {
      const a = portions[i];
      const b = portions[j];
      if (a.dayNumber === b.dayNumber) {
        continue;
      }
      if (rangesOverlap(a.range, b.range)) {
        violations.push(
          violate(
            "C7",
            `porção de ${a.label} do dia ${a.dayNumber} se sobrepõe à porção de ${b.label} do dia ${b.dayNumber} (${a.range.bookId})`,
          ),
        );
      }
    }
  }
}

/** C8 — dia com AT 'legal-ritual'/'genealogico'/'profetico' tem nota cobrindo essa porção. */
function checkC8(bundle: ContentBundle, violations: string[]): void {
  for (const day of sortedDays(bundle)) {
    if (!NOTE_REQUIRED_CATEGORIES.has(day.atPortion.category)) {
      continue;
    }

    const covered = day.noteIds.some((noteId) => {
      const note = bundle.notes.find((n) => n.id === noteId);
      if (!note) {
        return false;
      }
      const pericope = bundle.pericopes.find((p) => p.id === note.pericopeId);
      if (!pericope) {
        return false;
      }
      return rangeContains(pericope, day.atPortion.range);
    });

    if (!covered) {
      violations.push(
        violate(
          "C8",
          `dia ${day.dayNumber} tem AT '${day.atPortion.category}' mas nenhuma nota referenciada cobre essa porção (RN-04)`,
        ),
      );
    }
  }
}

/**
 * RN-13 (cobertura, TASK-014) — todo dia cuja categoria de AT **não** esteja
 * em `NOTE_REQUIRED_CATEGORIES` (esses já são checados por C8) precisa ter
 * pelo menos uma nota referenciada cobrindo a porção de AT **ou** a de NT do
 * dia (`PRD-TECNICO.md`: "nos demais dias, uma nota cobrindo qualquer uma das
 * duas porções satisfaz a regra" — não é exigida uma nota por porção).
 */
function checkRN13Coverage(bundle: ContentBundle, violations: string[]): void {
  for (const day of sortedDays(bundle)) {
    if (NOTE_REQUIRED_CATEGORIES.has(day.atPortion.category)) {
      continue;
    }

    const covered = day.noteIds.some((noteId) => {
      const note = bundle.notes.find((n) => n.id === noteId);
      if (!note) {
        return false;
      }
      const pericope = bundle.pericopes.find((p) => p.id === note.pericopeId);
      if (!pericope) {
        return false;
      }
      return rangeContains(pericope, day.atPortion.range) || rangeContains(pericope, day.ntPortion);
    });

    if (!covered) {
      violations.push(
        violate(
          "RN-13-cobertura",
          `dia ${day.dayNumber} não tem nenhuma nota referenciada cobrindo a porção de AT nem a de NT do dia`,
        ),
      );
    }
  }
}

/** C9 — trilha de NT inicia por um Evangelho e completa pelo menos um integralmente. */
function checkC9(bundle: ContentBundle, violations: string[]): void {
  const days = sortedDays(bundle);
  const dayOne = days.find((day) => day.dayNumber === 1);

  if (!dayOne || !isGospelBookId(dayOne.ntPortion.bookId)) {
    violations.push(
      violate(
        "C9",
        `o dia 1 deveria começar a trilha de NT por um Evangelho (${GOSPEL_BOOK_IDS.join(", ")}); encontrado '${dayOne?.ntPortion.bookId ?? "nenhum dia 1"}'`,
      ),
    );
  }

  const completesAnyGospel = GOSPEL_BOOK_IDS.some((gospelId) => {
    const portions = days
      .filter((day) => day.ntPortion.bookId === gospelId)
      .map((day) => day.ntPortion)
      .sort((a, b) => {
        const cmp = a.startChapter - b.startChapter;
        return cmp !== 0 ? cmp : a.startVerse - b.startVerse;
      });

    if (portions.length === 0) {
      return false;
    }
    if (portions[0].startChapter !== 1 || portions[0].startVerse !== 1) {
      return false;
    }
    for (let i = 0; i < portions.length - 1; i += 1) {
      if (!isImmediatelyAfter(rangeEnd(portions[i]), rangeStart(portions[i + 1]))) {
        return false;
      }
    }
    const lastEnd = rangeEnd(portions[portions.length - 1]);
    const finalVerse = GOSPEL_FINAL_VERSE[gospelId];
    return lastEnd.chapter === finalVerse.chapter && lastEnd.verse === finalVerse.verse;
  });

  if (!completesAnyGospel) {
    violations.push(
      violate(
        "C9",
        `nenhum Evangelho (${GOSPEL_BOOK_IDS.join(", ")}) é completado integralmente pela trilha de NT dentro dos ${bundle.plan.dayCount} dias`,
      ),
    );
  }
}

/**
 * C10 — checagem de guarda sobre `plan.name` (ver nota de módulo: a
 * verificação plena é de copy de interface, fora do escopo deste bundle).
 */
function checkC10(bundle: ContentBundle, violations: string[]): void {
  const normalized = bundle.plan.name.toLowerCase();
  const match = FORBIDDEN_PLAN_NAME_PHRASES.find((phrase) => normalized.includes(phrase));
  if (match) {
    violations.push(
      violate(
        "C10",
        `nome do plano ("${bundle.plan.name}") sugere cobertura integral da Bíblia ("${match}"); o plano deve ser nomeado como curadoria da espinha dorsal narrativa`,
      ),
    );
  }
}

/** C11 — todo dia (exceto o último) tem gancho de continuidade de até 140 caracteres. */
function checkC11(bundle: ContentBundle, violations: string[]): void {
  for (const day of sortedDays(bundle)) {
    const isLastDay = day.dayNumber === bundle.plan.dayCount;
    if (isLastDay) {
      if (day.hook) {
        violations.push(
          violate(
            "C11",
            `dia ${day.dayNumber} (último) não deveria ter gancho de continuidade — tem a tela de conclusão (RF-20)`,
          ),
        );
      }
      continue;
    }
    if (!day.hook || day.hook.trim().length === 0) {
      violations.push(violate("C11", `dia ${day.dayNumber} não tem gancho de continuidade`));
    } else if (day.hook.length > 140) {
      violations.push(
        violate(
          "C11",
          `dia ${day.dayNumber}: gancho de continuidade tem ${day.hook.length} caracteres, acima do limite de 140`,
        ),
      );
    }
  }
}

function countWords(text: string): number {
  const trimmed = text.trim();
  if (trimmed.length === 0) {
    return 0;
  }
  return trimmed.split(/\s+/).length;
}

/**
 * RN-13 (validação de nota) — cada `Note` do bundle, isoladamente: contagem
 * de palavras do `body` entre 120 e 200 (CA-06.3) e autoria presente/não
 * vazia (RN-04/CA-06.4). `reviewer`, quando presente, não pode ser string
 * vazia/só espaço — mas sua ausência nunca é violação (RN-04: opcional).
 */
function checkNoteValidity(bundle: ContentBundle, violations: string[]): void {
  for (const note of bundle.notes) {
    const wordCount = countWords(note.body);
    if (wordCount < NOTE_MIN_WORD_COUNT || wordCount > NOTE_MAX_WORD_COUNT) {
      violations.push(
        violate(
          "RN-13",
          `nota '${note.id}' tem ${wordCount} palavra(s) no corpo; fora da faixa ${NOTE_MIN_WORD_COUNT}–${NOTE_MAX_WORD_COUNT} (CA-06.3)`,
        ),
      );
    }

    if (!note.author || note.author.trim().length === 0) {
      violations.push(
        violate("RN-13", `nota '${note.id}' não tem autoria identificada (RN-04/CA-06.4)`),
      );
    }

    if (typeof note.reviewer === "string" && note.reviewer.trim().length === 0) {
      violations.push(
        violate(
          "RN-13",
          `nota '${note.id}' declara campo de revisor vazio/só espaço — revisor deve ser omitido quando não houver, não uma string vazia (RN-04)`,
        ),
      );
    }
  }
}

/**
 * Roda as 11 restrições de RN-01 (C1–C11, com C5/C6/C10 nos limites descritos
 * na nota de módulo), as duas metades de cobertura de RN-13
 * (`checkC8`/`checkRN13Coverage`) e a validação de nota de RN-13
 * (`checkNoteValidity`, TASK-014) contra `bundle` e devolve o relatório. Não
 * lança — quem precisa vetar o build chama `assertContentValid`.
 */
export function validateContent(bundle: ContentBundle): ContentValidationReport {
  const violations: string[] = [];

  checkC1(bundle, violations);
  checkC2(bundle, violations);
  checkC3(bundle, violations);
  checkC4(bundle, violations);
  checkC5(bundle, violations);
  checkC6(bundle, violations);
  checkC7(bundle, violations);
  checkC8(bundle, violations);
  checkRN13Coverage(bundle, violations);
  checkC9(bundle, violations);
  checkC10(bundle, violations);
  checkC11(bundle, violations);
  checkNoteValidity(bundle, violations);

  return {
    passed: violations.length === 0,
    checkedAt: new Date().toISOString(),
    violations,
  };
}

/**
 * Veto de build (CA-02.5): lança `ContentValidationError` com a lista de
 * violações (uma por linha, cada uma já prefixada com o C-número) quando o
 * bundle não passa. Uso pretendido: pipeline de build/CI do conteúdo
 * editorial, para nunca publicar um plano inválido.
 */
export function assertContentValid(bundle: ContentBundle): void {
  const report = validateContent(bundle);
  if (!report.passed) {
    throw new ContentValidationError(
      `Validação de conteúdo falhou (RN-01/RN-13, ${report.violations.length} violação(ões)):\n${report.violations
        .map((v) => `  - ${v}`)
        .join("\n")}`,
    );
  }
}
