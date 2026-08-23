# AI Design Generation Guardrails

Use this file whenever an AI model generates or edits ZainX product UI.

## Required instruction

Design for **ZainX Workforce**, a mature enterprise Workforce / Payroll / Compliance / Talent platform. Do not produce a generic shadcn dashboard, a Linear clone, a purple AI template, or a marketing-style Bento page.

### Product character

- Operational first; dashboards are secondary.
- Dense but easy to scan.
- Quiet neutral foundation.
- Strong information hierarchy.
- Financial-grade clarity in payroll.
- Contextual workspaces over fragmented pages.
- Arabic RTL and English LTR from the same component system.
- Keyboard/accessibility-aware.

### Signature visual rules

- The system is quiet. Important things glow.
- Light is an event, not decoration.
- Use Utility, Emphasis, and Spotlight card levels.
- Spotlight/neon is allowed for the primary payroll state, major AI insight, critical operational outcome, brand/loading/permission/success moments.
- Never make every card glow.
- Avoid default AI clichés: giant purple orb, random sparkles, glossy gradient chat bubble everywhere.
- AI should feel intelligent through context, provenance, tool/source visibility, and controlled actions.

### Motion rules

Use the shared motion tokens. Brand/login motion may be expressive; routine workflow interactions stay fast. Permission states, long-running processing, AI thinking, and major success moments may use short signature motion. Respect reduced motion.

### Pattern rules

Choose one of the eight approved patterns before composing the screen:

1. Operational Dashboard
2. Data Grid / List
3. Detail Workspace
4. Guided Process Workspace
5. Kanban / Pipeline
6. Calendar / Schedule
7. Inbox / Exceptions
8. Configuration / Builder

### Forbidden defaults

- Generic 4-card dashboard as the answer to every screen.
- Excessive rounded cards around every section.
- 3D/tilt cards in payroll, admin or compliance.
- Continuous animated gradients behind dense data.
- Huge whitespace that reduces operational density.
- Hidden critical actions available only on hover.
- Desktop tables compressed unreadably on mobile.
- Styling that assumes LTR only.

### Reference priorities

- Shell/hierarchy: Linear + Rippling.
- Grid/views: Attio + Carbon + Stripe.
- Payroll: Stripe + Rippling + Deel/ZenHR domain checks.
- Recruitment: Ashby + Attio.
- AI: Intercom + Rippling.
- Motion/style details: selectively inspired by 21st.dev, re-authored into ZainX rules.
