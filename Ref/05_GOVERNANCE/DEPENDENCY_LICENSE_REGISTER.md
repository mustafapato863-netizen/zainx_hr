# Dependency, License & Procurement Register

**Register Status:** Re-baselined against current web lockfile; Phase 1C decisions preserved
**Date:** August 25, 2026

---

## 1. Evaluated Dependencies & Licensing Status

| Dependency | Installed Version | Role | License | Commercial Status & Procurement Action | Phase 1C Decision |
| :--- | :--- | :--- | :--- | :--- | :---: |
| **AG Grid Enterprise** | `33.3.2` | Heavy operational grid | Commercial / Proprietary | Evaluation/trial status remains visible in development. Developer seat license required before production deployment. Air-gapped / on-premise deployment supported without cloud telemetry. | **ACCEPT WITH RESTRICTIONS** |
| **FullCalendar Core/Views** | `6.1.15` | Day/Week/Month/List scheduling | MIT (Open-Source) | 100% Free & Open-Source for standard calendar views. No commercial license required for core views. | **ACCEPT** |
| **FullCalendar Premium/Scheduler** | `N/A` (Deferred) | Resource Timeline grid | Commercial / Premium | Timeline view requires commercial license. Deferred until resource scheduling module start. | **DEFER PREMIUM** |
| **Apache ECharts** | `6.1.0` | Analytics & trend charts | Apache 2.0 (Open-Source) | 100% Open-Source. Zero runtime cloud dependencies. Accessible data table alternative required. | **ACCEPT** |
| **Tiptap Core / StarterKit** | `2.27.2` | Controlled rich text editor | MIT (Open-Source) | 100% Open-Source. Server-side schema validation remains the security boundary; client sanitization is defense-in-depth. Zero paid cloud extensions utilized. | **ACCEPT** |
| **dnd-kit** | `6.3.1` | Kanban & ordering drag interactions | MIT (Open-Source) | 100% Open-Source. Backend command authority owns domain state truth with client rollback. | **ACCEPT** |
| **Motion for React** | `12.43.0` | UI micro-interactions & layout | MIT (Open-Source) | 100% Open-Source. Complete `prefers-reduced-motion` compliance. | **ACCEPT** |
| **GSAP** | `N/A` (Rejected) | Brand SVG animation | Standard GreenSock License | **REJECTED**: Motion for React satisfies all approved UI motion and BrandAssembly SVG requirements without introducing a second animation runtime or bundle weight. Review trigger: only if official brand SVG requirements exceed Motion capabilities. | **REJECT** |
| **React Aria Components** | `1.6.0` | Accessible UI foundation | Adobe BSD-3-Clause | 100% Open-Source. Clean accessibility guarantees. | **APPROVED (Phase 1B)** |
| **Lucide React** | `0.475.0` | System iconography | ISC (Open-Source) | 100% Open-Source. Clean tree-shakeable icons. | **APPROVED (Phase 1B)** |

---

## 2. Procurement & Deployment Parameters Recorded

1. **Number of Developer Seats Requiring Commercial Licensing:**
   - AG Grid Enterprise: ~5 Frontend Core Engineers configuring heavy operational grids.
2. **CI / CD Build Usage:**
   - Evaluation packages compile cleanly on CI without breaking builds.
3. **On-Premise / Customer Air-Gapped Deployment:**
   - 100% bundled locally in Docker/Nginx containers.
   - Zero external CDN or cloud license phone-home calls.
4. **License Key Handling Strategy:**
   - AG Grid license key injected via secure environment variable (`VITE_AG_GRID_LICENSE_KEY`) during container startup.
