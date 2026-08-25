# Phase 1C Enterprise Engine Decision Matrix

**Document Version:** 1.0  
**Phase:** Phase 1C Enterprise Spikes  
**Date:** August 25, 2026
**Final Verdict:** All 10 Enterprise Technology Tracks Evaluated & Documented — **PASS / CLOSED**

---

## 1. Summary Decision Matrix

| Technology | Candidate Package & Version | Role in ZainX Platform | Decision | Core Rationale |
| :--- | :--- | :--- | :---: | :--- |
| **1. AG Grid Enterprise** | `ag-grid-enterprise@33.3.2`<br>`ag-grid-react@33.3.2` | Heavy operational grid (Payroll, Rosters, Logs) | **ACCEPT WITH RESTRICTIONS** | Superior 10k+ row virtualization, column pinning, and aggregation. Encapsulated in `ZainXDataGrid`. Commercial procurement required before production release; Phase 2 must remain Community-safe. |
| **2. FullCalendar Scheduler** | `@fullcalendar/core@6.1.15`<br>`@fullcalendar/react@6.1.15` | Attendance rosters, Leave calendar, Interview schedules | **ACCEPT WITH RESTRICTIONS** | Open-source MIT views (Day, Week, Month, List) approved behind `ZainXScheduler`. Premium Timeline view deferred to procurement review. Non-drag accessible form fallback mandatory. |
| **3. Apache ECharts** | `echarts@6.1.0`<br>`echarts-for-react@3.0.2` | High-fidelity executive analytics and variance trends | **ACCEPT WITH RESTRICTIONS** | Encapsulated in `ZainXChart`. Responsive SVG rendering, dark/light contrast, RTL Arabic tooltips. Mandatory accessible semantic data table alternative provided on all charts. |
| **4. Tiptap Rich Text** | `@tiptap/react@2.27.2`<br>`@tiptap/starter-kit@2.27.2` | Controlled job descriptions and template editing | **ACCEPT WITH RESTRICTIONS** | Scoped to open-source core starter kit. Server-side structured-schema validation plus client sanitization defense-in-depth. Zero paid cloud extensions. |
| **5. dnd-kit** | `@dnd-kit/core@6.3.1` | ATS candidate Kanban stage movement & priority order | **ACCEPT WITH RESTRICTIONS** | Encapsulated in `ZainXKanban`. Backend command authority owns domain state truth with optimistic rollback. Accessible keyboard/button movement controls mandatory. |
| **6. Motion for React** | `motion@12.43.0` | Core micro-interactions, layout transitions, Spotlights | **ACCEPT** | Modern animation engine for React 19. Complete `prefers-reduced-motion` compliance. Clean semantic token integration. |
| **7. GSAP** | `gsap` (Evaluated for BrandAssembly) | Brand SVG choreography | **REJECT (Motion Sufficient)** | Motion for React provides complete SVG path draw, glow, and timeline choreography. Adding GSAP would add a second animation runtime, bundle weight, and maintenance complexity without requirement. |
| **8. Enterprise Auth / SSO** | Standard OIDC / OAuth 2.x / Entra ID | Corporate Identity Provider single sign-on | **ACCEPT APPROACH** | Standards-based OIDC integration. Layering boundary enforced: External IdP → Host Auth Infra → Authenticated Principal → Provider-Neutral `CurrentActor` / `IUserContext` → App Operations → Domain. Domain modules never import auth SDKs. |
| **9. Realtime Push Transport** | SignalR / Server-Sent Events (SSE) | Accelerated notification & job status updates | **DEFER PUSH / ACCEPT POLLING** | Polling via TanStack Query is canonical correctness baseline. Push transport is acceleration-only; deferred until high-frequency live collaboration features in later phases. |
| **10. Architecture Boundaries** | ESLint `no-restricted-imports` + Nx | Wrapper encapsulation & isolation | **ACCEPT** | 100% boundary enforcement. Feature modules must consume `@zainx/design-system` and cannot import raw vendor libraries. |

---

## 2. Wrapper Boundaries & Encapsulation Mapping

```
Feature Modules (@zainx/people, @zainx/payroll, @zainx/attendance, etc.)
  │
  ├── Must Import: @zainx/design-system
  │     ├── ZainXDataGrid         ──> wraps ag-grid-react / ag-grid-enterprise
  │     ├── ZainXScheduler        ──> wraps @fullcalendar/react
  │     ├── ZainXChart            ──> wraps echarts-for-react
  │     ├── ZainXRichTextEditor   ──> wraps @tiptap/react + DOMPurify
  │     ├── ZainXKanban           ──> wraps @dnd-kit/core
  │     └── Motion Primitives     ──> wraps motion
  │
  └── Prohibited: Direct vendor engine imports (Enforced by ESLint CI)
```
