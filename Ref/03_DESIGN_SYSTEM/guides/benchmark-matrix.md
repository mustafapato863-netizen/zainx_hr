# Final Visual Benchmark Matrix

This matrix is filtered against ZainX's product requirements: operational work, dense enterprise data, payroll/compliance trust, strong permissions, RTL/LTR, contextual AI, and controlled visual personality.

## Tier A — Primary pattern references

| Source | Best use in ZainX | Extract | Do not copy |
|---|---|---|---|
| **Linear** | App shell, hierarchy, density, keyboard-first feel | Calm navigation, consistent headers, dimmer chrome, workspace prominence | Product-management vocabulary or exact monochrome styling |
| **Attio** | Data grid, saved views, filters, record workspace, Kanban density | Table editing model, saved filters/sorts, visible attributes, compact pipeline | CRM-specific object model or visual identity |
| **Rippling** | Workforce platform behavior, permissions, cross-module context, AI actions | People-centered system model, dynamic permission framing, AI with live data and approval | Broad suite complexity or its exact brand look |
| **Stripe Dashboard** | Payroll/financial clarity, search, reports, filters | Accounting-grade information hierarchy, global search, filters, precise amounts | Payments-specific navigation or color system |
| **Ashby** | Recruitment / ATS, pipeline, recruiting analytics | Clear next steps, compact pipeline, drill-down analytics, structured process | Recruiting-only assumptions outside Talent |
| **Intercom** | Contextual AI side panel, inbox/task context | AI inside current work context, source visibility, resizable side panel | Support-inbox information architecture |
| **Carbon Design System** | Enterprise table behavior, accessibility, state discipline | Keyboard semantics, batch actions, table toolbar, density variants, AI presence rules | IBM visual identity |

## Tier B — Domain / secondary references

| Source | Use | Reason |
|---|---|---|
| **ZenHR** | Egypt payroll/attendance/ESS domain coverage | Local HR expectations, bilingual behavior, payroll/attendance coupling |
| **Deel** | Payroll, payslip transparency, employee self-service | Global payroll operations and employee-facing payroll clarity |
| **HiBob** | People experience, time off, HR workflows | Softer HR experience useful for employee/manager surfaces |
| **shadcn/ui** | Implementation patterns for shell and primitives | Composable React patterns, Tailwind-friendly, RTL support |
| **Base UI** | Headless interaction primitives | Accessible, unstyled, composable, good fit for a custom visual identity |

## Tier C — Inspiration libraries, not product architecture

| Source | Role | Filter rule |
|---|---|---|
| **Nicelydone** | Primary SaaS screenshot/flow research library | Use to discover patterns by screen type, not brand |
| **PageFlows** | Flow and sequence research | Use for login, onboarding, permissions, approvals, payroll-like step flows |
| **Mobbin** | Broad visual exploration | Strong recall, but aggressively filter consumer/mobile patterns |
| **SaaSFrame** | Quick SaaS screen reference and Figma availability | Secondary source when stronger product references are missing |
| **21st.dev** | Component style and motion inspiration | Use for spotlight, glow, motion, cards and micro-interactions only after enterprise filtering |

## 21st.dev filter

### Adopt / remix

- Spotlight Card / border-only spotlight.
- Statistics / KPI cards.
- Progress metric cards.
- Controlled glowing effect on major status cards.
- Login/auth composition ideas.
- Empty-state and AI-response patterns.
- Motion primitives that can be slowed and restrained.

### Reject as default system language

- 3D tilt cards on operational screens.
- Continuous neon borders on common cards.
- Heavy glassmorphism over dense tables/forms.
- Decorative shaders behind payroll data.
- Large animated bento layouts for ordinary admin work.
- Motion that triggers on every scroll or hover without business meaning.

## Final design-source assignment by product area

| ZainX area | Primary references | Secondary references |
|---|---|---|
| App Shell | Linear, Rippling | shadcn, Intercom |
| Home / Operational Dashboard | Rippling, Stripe | Ashby, HiBob |
| Data Grid | Attio, Carbon, Stripe | Linear |
| Employee Profile | Rippling | HiBob, Attio |
| Attendance | ZenHR, Rippling | HiBob |
| Leave / ESS | ZenHR, HiBob | Rippling |
| Payroll | Stripe, Rippling | Deel, ZenHR, Carbon |
| Recruitment | Ashby, Attio | Rippling |
| Approvals / My Work | Intercom, Rippling | Carbon |
| Reports | Stripe, Ashby | Attio, Rippling |
| AI Copilot | Intercom, Rippling | Linear AI interaction ideas |
| Administration / Permissions | Rippling, Carbon | Stripe |
| Mobile-responsive ESS | ZenHR, HiBob | Mobbin as research library |
| Signature motion | 21st.dev, PageFlows research | custom ZainX motion rules |

## Research conclusion

The product must not become a collage. The reference stack is intentionally split by responsibility:

- **Behavior:** Linear / Attio / Rippling / Stripe / Ashby / Intercom / Carbon.
- **Egypt domain validation:** ZenHR.
- **Visual energy and motion:** 21st.dev, selectively.
- **Implementation primitives:** shadcn/ui + Base UI patterns.

This gives ZainX a distinctive system while preserving enterprise trust.
