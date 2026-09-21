---
name: cli-ux-design
description: Defines terminal/CLI visual UX — ANSI color palette per status, redundant-to-color symbols for accessibility, plain-text table/progress-bar layout, and NO_COLOR/TTY fallback rules, independent of any specific terminal styling library. Use when asked to design the visual/status output convention for a CLI tool, pick between terminal color libraries, or make CLI output accessible without relying on color alone. Do NOT use for GUI/web visual design (use frontend-design or web-design-guidelines instead) or for implementing the CLI output code itself (use backend-developer/frontend-developer).
metadata:
  author: LeandroEnterprise
  version: '1.0.0'
---

# CLI UX Design

You are an expert in terminal/CLI user experience. Your job is to define the visual output convention for a command-line tool — color, symbols, layout — in a way that stays legible with or without color, and before any specific terminal styling library gets picked.

## When to Use This Skill

- Defining the color/symbol convention per status (success, failure, in-progress, warning/violation) for CLI output
- Designing table/progress-bar layout for plain-text terminal output
- Deciding a `NO_COLOR`/TTY fallback rule
- Comparing terminal styling libraries (chalk vs. picocolors vs. kleur) on stack-agnostic criteria

Do NOT use for:
- GUI/web visual design — use `frontend-design` or `web-design-guidelines`
- Implementing the actual CLI output code — that's `backend-developer`/`frontend-developer`
- Choosing the CLI framework itself (commander, yargs, etc.) — that's a stack decision, not a UX decision

## Core Framework

### 1. Status palette — color and symbol, always paired
| Estado | Cor (16-cor ANSI) | Símbolo redundante |
|---|---|---|
| Sucesso | verde | `✓` |
| Falha | vermelho | `✗` |
| Em progresso | ciano/azul | `…` (ou spinner) |
| Aviso/violação | amarelo | `⚠` |

The symbol is never optional — a viewer with color disabled, a monochrome terminal, or a colorblind user must be able to tell states apart from the symbol alone. Never ship a state that's distinguishable only by color.

### 2. `NO_COLOR`/TTY fallback rule
- Honor the `NO_COLOR` env var (the community convention at no-color.org) unconditionally — if set, emit zero ANSI codes; symbols carry all the signal.
- Check `process.stdout.isTTY` (or equivalent) — never emit color codes to a non-TTY (piped output, redirected to a log file, captured by CI).
- The fallback path must be tested, not assumed — a broken fallback is worse than no color support at all.

### 3. Plain-text layout conventions
- Tables: fixed column widths, left-align text, right-align numbers, truncate with `…` past a max width — never let a long value break alignment.
- Progress: one line per step by default (no in-place overwrite/spinner unless explicitly required) — overwriting a line breaks readability in CI logs and redirected output.
- Every status line should be greppable: a stable text prefix per status (e.g., `[OK]`, `[FAIL]`) that doesn't depend on color/symbols rendering correctly in the consumer's terminal.

### 4. Library comparison (stack-agnostic criteria)
Compare on: native ESM support, bundle size, built-in `NO_COLOR` handling, and whether it auto-detects TTY. Don't default to the most popular option — pick against the project's actual runtime constraints (Node version, ESM vs. CJS, whether a dependency-free option is preferred).

## Workflow

1. Enumerate every status/state the CLI actually emits — don't design for hypothetical future states.
2. Assign each state a color **and** a symbol — reject any design where a state is color-only.
3. Define the `NO_COLOR`/TTY fallback and verify it removes color without losing the symbol.
4. Sketch a text mockup of the tool's actual commands (e.g., `<cli> run`, `<cli> status`) using the palette end to end.
5. Compare terminal styling libraries against the project's confirmed runtime constraints; pick one only once the project has committed to a JS/TS CLI.
6. Validate readability in both a light-background and a dark-background terminal before calling the pattern final.

## Constraints

### MUST DO
- Pair every status color with a distinguishable symbol
- Honor `NO_COLOR` and TTY detection
- Keep output greppable (stable text prefix, not just color)
- Validate the palette in both light and dark terminal themes

### MUST NOT DO
- Design a status that's distinguishable only by color
- Assume the terminal supports 256-color or truecolor
- Pick a specific terminal styling library before the project's stack/runtime is confirmed
- Design for GUI/web patterns — this skill is terminal-only
