# AI Release Model — 7A / 7B

AI shell and design primitives may exist earlier, but production AI capability ships in two controlled stages.

## Phase 7A — Read / Analyze / Explain

Capabilities:
- Ask
- Analyze
- Explain
- read-only business tools
- RAG over approved company/product knowledge
- payroll trace explanation
- variance explanation
- report interpretation
- source provenance
- citations
- permission-scoped context
- evaluation suite
- feedback collection

No business mutation.

### Required before release
- tool allowlist
- user-scope inheritance
- provenance labels
- source citation model
- prompt-injection controls
- evaluation datasets
- privacy/telemetry rules
- fallback behavior

## Phase 7B — Proposed / Confirmed Actions

Capabilities:
- propose action
- show before/after
- effective date
- impact
- permission/approval context
- explicit confirmation
- execute normal backend command
- audit result
- learning inbox / quality workflow

Flow:

```text
User
→ AI
→ proposed governed tool
→ authorization
→ validation
→ impact preview
→ user confirmation
→ normal application command
→ database
→ audit
```

AI never writes the database directly.

High-risk actions must not be autonomous.

## Activity UI

Allowed:
- Context attached
- Company policy retrieved
- Payroll trace tool running
- Tool completed
- Action requires confirmation

Not allowed:
- hidden chain-of-thought
- fabricated “reasoning steps”
- invented progress percentages
