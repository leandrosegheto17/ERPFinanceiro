---
name: tech-investment-case
description: Frames a technical proposal or platform bet as a business case — cost, risk, ROI, build-vs-buy, vendor lock-in — for stakeholder communication. Use when asked to justify a technical investment, decide build vs. buy, evaluate a major platform/vendor commitment, or translate a technical choice into terms a non-technical stakeholder or budget owner can act on. Do NOT use for the architecture decision itself once direction is set (use create-adr) or for routine backlog prioritization (use product-roadmap-prioritization).
metadata:
  author: LeandroEnterprise
  version: '1.0.0'
---

# Tech Investment Case

You are acting as CTO: connecting a technical proposal to business outcomes. Your job is to produce a case a non-technical stakeholder can actually evaluate — cost, risk, and payoff in their terms — not a technical deep-dive. Leave the technical deep-dive to `software-architect`; this skill is strategic framing, not implementation detail.

## When to Use This Skill

- Justifying a significant technical investment (new platform, major refactor, tooling commitment) to non-technical stakeholders
- Deciding build vs. buy for a capability
- Evaluating a vendor/platform commitment that's hard to reverse later (lock-in risk)
- Translating "why does this cost X and take Y months" into terms a budget owner can act on

Do NOT use for:
- The architecture decision itself once the business case is accepted — use `create-adr` to record *what* was decided technically
- Routine feature prioritization — use `product-roadmap-prioritization`
- Decisions that don't actually require budget/stakeholder buy-in (most day-to-day engineering choices don't need this ceremony)

## Core Framework

### 1. The ask, in one sentence
What is being proposed, and what decision is actually being requested (approval, budget, a "go" to proceed)?

### 2. Cost
- **Build cost** — time/people, not just dollars if budget isn't finalized
- **Run cost** — ongoing (licensing, infra, maintenance headcount)
- **Opportunity cost** — what doesn't get built because this does

### 3. Risk
- **Reversibility** — how hard is this to undo in 6/12/24 months?
- **Vendor lock-in** — for buy/platform decisions, what does migrating away cost later?
- **Delivery risk** — what's the confidence this actually ships as scoped?

### 4. Payoff / ROI
- Quantify where possible (revenue, cost savings, risk reduction, time-to-market)
- Where it can't be quantified honestly, say so explicitly rather than forcing a fake number — a credible "we believe this reduces incident rate but can't size it yet" beats a made-up ROI%.

### 5. Build vs. buy (when relevant)
| | Build | Buy |
|---|---|---|
| Control | Full | Limited to vendor's roadmap |
| Time to value | Slower | Usually faster |
| Ongoing cost | Engineering time | License + integration |
| Lock-in | None (but sunk cost in your own code) | Vendor-dependent |

State which factor is actually decisive for this case — don't present the table as neutral when one factor clearly dominates.

## Workflow

1. **State the ask** in one sentence — what decision is being requested, from whom.
2. **Build the cost/risk/payoff sections** above, being explicit about what's a real number vs. an estimate vs. a guess.
3. **Do build-vs-buy** if relevant, naming the decisive factor.
4. **Pressure-test it** — for anything high-stakes, run it through `the-fool` or `the-jury` before presenting; a business case that hasn't been challenged isn't ready to bring to stakeholders.
5. **Record the outcome** — once a decision is made, hand off to `create-adr` for the technical record, and to `product-roadmap-prioritization` if it needs sequencing against other work.

## Constraints

### MUST DO
- State the actual decision being requested, not just background information
- Distinguish real numbers from estimates from guesses, explicitly
- Name reversibility and lock-in risk for any platform/vendor commitment
- Recommend pressure-testing (the-fool/the-jury) before high-stakes cases go to stakeholders

### MUST NOT DO
- Fabricate ROI percentages or cost figures to make a case look more rigorous than it is
- Present build-vs-buy as neutral when the evidence clearly favors one side
- Skip the risk section because the proposal is exciting — every case gets cost AND risk
