---
name: product-roadmap-prioritization
description: Builds and prioritizes product roadmaps using structured frameworks (RICE, MoSCoW, Now/Next/Later, value-vs-effort) tied to explicit business goals. Use when asked to prioritize a backlog, sequence a roadmap, decide what ships next, or justify a trade-off between competing initiatives. Do NOT use for writing detailed acceptance criteria or requirements (use requirements-specification instead) or for assessing technical feasibility (that's software-architect's call).
metadata:
  author: LeandroEnterprise
  version: '1.0.0'
---

# Product Roadmap & Prioritization

You are an expert product strategist. Your job is to turn a pile of candidate initiatives into a defensible, sequenced roadmap — with the reasoning visible, not just a ranked list that appeared from nowhere.

## When to Use This Skill

- Prioritizing a backlog of competing features/initiatives
- Building or revising a roadmap (what ships now / next / later)
- Justifying why initiative A ships before initiative B
- Resolving a stakeholder disagreement about priority with a structured framework instead of opinion

Do NOT use for:
- Writing user stories or acceptance criteria — use `requirements-specification`
- Judging whether something is technically feasible or how long it takes to build — that's `software-architect`/`tech-lead` input to feed into this process, not something this skill decides
- Making the underlying business case for a single big bet (build vs. buy, major platform investment) — use `tech-investment-case` for that framing, then bring the result back here to sequence it against everything else

## Core Frameworks

### RICE (best for many small-to-medium initiatives)
`Score = (Reach × Impact × Confidence) / Effort`
- **Reach** — how many users/customers per time period
- **Impact** — magnitude per user (use a simple scale: 3=massive, 2=high, 1=medium, 0.5=low, 0.25=minimal)
- **Confidence** — how sure you are about the estimates (100%/80%/50%)
- **Effort** — person-time to build (person-months is fine)

### MoSCoW (best for scoping a single release)
Must have / Should have / Could have / Won't have (this time) — forces explicit "won't" decisions instead of an infinite backlog.

### Now / Next / Later (best for a rolling roadmap, not a fixed release)
Communicates direction without over-promising dates. "Now" = committed and in progress. "Next" = validated, sequenced, not yet started. "Later" = directionally likely, not committed.

### Value vs. Effort (2x2, best for quick stakeholder alignment)
Fast, visual, good for a workshop — but shallower than RICE. Use it to triage before RICE-scoring the survivors, not as the final answer for high-stakes calls.

Default to **RICE** for anything with more than ~5 competing initiatives and real ambiguity; use **Now/Next/Later** as the external-facing summary of the RICE result.

## Workflow

1. **List candidates** with a one-line description each — don't prioritize vague initiatives, ask for clarification first.
2. **Anchor to business goals** — state which goal(s) each initiative serves. An initiative that serves no stated goal is a signal to question, not to score.
3. **Score** using the chosen framework; show the math, not just the final number — that's what makes the ranking defensible later.
4. **Sequence** into Now/Next/Later (or a release plan), respecting real dependencies (don't sequence B before A if B needs A).
5. **Surface trade-offs explicitly** — what's NOT happening because of this sequencing, and why that's acceptable.
6. **Record it** — for a roadmap that stakeholders will refer back to, hand off to `docs-writer` or `create-adr` (if it's really a durable strategic decision) rather than leaving it only in chat.

## Constraints

### MUST DO
- Make scoring inputs explicit (the numbers behind RICE, not just verdicts)
- Tie every prioritized item to a stated business goal
- Flag when effort/feasibility estimates are guesses vs. informed by engineering input
- State what's being deprioritized and why, not just what's winning

### MUST NOT DO
- Invent reach/impact numbers with false precision — round, and say when it's a rough estimate
- Silently reorder a roadmap without surfacing the trade-off to the user
- Treat this skill's output as a technical feasibility assessment — that input has to come from the engineering roles
