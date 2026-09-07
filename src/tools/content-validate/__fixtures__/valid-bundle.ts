/**
 * Fixture de um `ContentBundle` de 90 dias que satisfaz C1–C11 (RN-01),
 * construído programaticamente (TASK-013) — não é conteúdo editorial real
 * (a curadoria de verdade é decisão do stakeholder, fora de escopo técnico,
 * `PRD-TECNICO.md` RN-01). Estrutura:
 *
 *  - AT: um capítulo de Salmos (livro `PSA`, arbitrário — só precisa ser um
 *    livro com capítulos suficientes e distintos por dia, C7) por dia,
 *    1..90, com categoria seguindo um ciclo de 3 a partir do dia 8 (árido a
 *    cada 3 dias, nunca 2 dias consecutivos, C2) e 'narrativo' fixo nos dias
 *    1–7 (C3).
 *  - NT: dias 1–28 cobrem Mateus 1–28 (capítulo por dia, terminando em
 *    Mateus 28:20 — versificação padrão, ver `gospel-versification.ts`),
 *    completando o Evangelho dentro do plano (C9); dias 29–89 preenchem
 *    Marcos 1–16, Lucas 1–24 e João 1–21 (capítulo por dia, sem repetir
 *    trecho, C7); dia 90 usa Atos 1 como preenchimento.
 *  - Cada porção (AT e NT) tem uma perícope correspondente terminando
 *    exatamente onde a porção termina (C6).
 *  - Todo dia árido/genealógico/profético (aqui, só árido) tem uma nota
 *    referenciando a perícope daquela porção de AT (C8).
 *  - Todo dia não-árido também tem uma nota, referenciando a perícope da
 *    porção de NT do dia (RN-13, metade de cobertura para os "demais dias" —
 *    `checkRN13Coverage`: qualquer uma das duas porções satisfaz).
 *  - Todo dia 1–89 tem gancho de continuidade; o dia 90 não tem (C11).
 *  - `atWordCount`/`ntWordCount` fixos por dia (1500/1000, soma 2500 —
 *    dentro de C4) resultam em 60% de participação do AT sobre o plano
 *    inteiro, dentro da faixa 50–65% de C5.
 */

import type {
  AtCategory,
  ContentBundle,
  Note,
  Pericope,
  PlanDay,
  ScriptureRange,
} from "../../../core/content";

const DAY_COUNT = 90;

function aridCycleCategory(dayNumber: number): AtCategory {
  if (dayNumber <= 7) {
    return "narrativo";
  }
  const offset = (dayNumber - 8) % 3;
  if (offset === 0) {
    return "legal-ritual";
  }
  return offset === 1 ? "narrativo" : "poetico";
}

function ntPortionForDay(dayNumber: number): ScriptureRange {
  if (dayNumber <= 28) {
    const chapter = dayNumber;
    return {
      bookId: "MAT",
      startChapter: chapter,
      startVerse: 1,
      endChapter: chapter,
      endVerse: chapter === 28 ? 20 : 15,
    };
  }
  if (dayNumber <= 44) {
    const chapter = dayNumber - 28;
    return { bookId: "MRK", startChapter: chapter, startVerse: 1, endChapter: chapter, endVerse: 15 };
  }
  if (dayNumber <= 68) {
    const chapter = dayNumber - 44;
    return { bookId: "LUK", startChapter: chapter, startVerse: 1, endChapter: chapter, endVerse: 15 };
  }
  if (dayNumber <= 89) {
    const chapter = dayNumber - 68;
    return { bookId: "JHN", startChapter: chapter, startVerse: 1, endChapter: chapter, endVerse: 15 };
  }
  return { bookId: "ACT", startChapter: 1, startVerse: 1, endChapter: 1, endVerse: 15 };
}

const NOTE_REQUIRED: readonly AtCategory[] = ["legal-ritual", "genealogico", "profetico"];

/**
 * Corpo de nota com exatamente 150 palavras (dentro da faixa 120–200 de
 * CA-06.3/RN-13, checada por `checkNoteValidity` — TASK-014): vocabulário
 * fixo repetido até atingir o total, com o número do dia intercalado só para
 * diferenciar levemente o texto entre dias. Não é conteúdo editorial real
 * (mesma ressalva do resto desta fixture) — só precisa satisfazer a contagem
 * mecânica de palavras.
 */
const NOTE_VOCABULARY = [
  "Esta", "nota", "de", "perícope", "explica", "por", "que", "este", "texto",
  "está", "aqui", "no", "dia", "do", "plano", "entrelaçado", "ajudando", "quem",
  "lê", "a", "entender", "o", "contexto", "histórico", "o", "gênero", "literário",
  "e", "o", "propósito", "teológico", "da", "passagem", "sem", "impor", "posição",
  "doutrinária", "nem", "prescrever", "conduta", "ao", "leitor", "apenas",
  "oferecendo", "orientação", "objetiva", "sobre", "o", "que", "observar", "e",
  "o", "que", "não", "esperar", "encontrar", "nesta", "porção", "específica",
  "da", "Escritura", "estudada", "hoje",
];
const NOTE_BODY_WORD_COUNT = 150;

function buildNoteBody(dayNumber: number): string {
  const words: string[] = ["Dia", String(dayNumber)];
  let vocabularyIndex = 0;
  while (words.length < NOTE_BODY_WORD_COUNT) {
    words.push(NOTE_VOCABULARY[vocabularyIndex % NOTE_VOCABULARY.length]);
    vocabularyIndex += 1;
  }
  return `${words.join(" ")}.`;
}

export function buildValidBundle(): ContentBundle {
  const days: PlanDay[] = [];
  const pericopes: Pericope[] = [];
  const notes: Note[] = [];

  for (let dayNumber = 1; dayNumber <= DAY_COUNT; dayNumber += 1) {
    const category = aridCycleCategory(dayNumber);
    const atRange: ScriptureRange = {
      bookId: "PSA",
      startChapter: dayNumber,
      startVerse: 1,
      endChapter: dayNumber,
      endVerse: 10,
    };
    const ntRange = ntPortionForDay(dayNumber);

    const atPericope: Pericope = {
      id: `at-${dayNumber}`,
      title: `Salmo ${dayNumber}`,
      ...atRange,
    };
    const ntPericope: Pericope = {
      id: `nt-${dayNumber}`,
      title: `${ntRange.bookId} ${ntRange.startChapter}`,
      ...ntRange,
    };
    pericopes.push(atPericope, ntPericope);

    const noteIds: string[] = [];
    if (NOTE_REQUIRED.includes(category)) {
      const note: Note = {
        id: `note-${dayNumber}`,
        pericopeId: atPericope.id,
        body: buildNoteBody(dayNumber),
        author: "Autora Fixture",
      };
      notes.push(note);
      noteIds.push(note.id);
    } else {
      // Dia não-árido: RN-13 (checkRN13Coverage) exige pelo menos uma nota
      // cobrindo qualquer uma das duas porções do dia — usa a perícope de NT.
      const note: Note = {
        id: `note-nt-${dayNumber}`,
        pericopeId: ntPericope.id,
        body: buildNoteBody(dayNumber),
        author: "Autora Fixture",
      };
      notes.push(note);
      noteIds.push(note.id);
    }

    days.push({
      dayNumber,
      atPortion: { range: atRange, category },
      ntPortion: ntRange,
      noteIds,
      hook: dayNumber === DAY_COUNT ? undefined : `O que acontece no dia ${dayNumber + 1}?`,
      wordCount: 2500,
      atWordCount: 1500,
      ntWordCount: 1000,
    });
  }

  return {
    plan: {
      id: "plan-90-fixture",
      name: "Plano Entrelaçado — espinha dorsal narrativa (90 dias)",
      dayCount: DAY_COUNT,
      validationReport: { passed: true, checkedAt: new Date(0).toISOString(), violations: [] },
    },
    days,
    pericopes,
    notes,
  };
}
