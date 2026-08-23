# ZainX Enterprise Engines — On-Premise & Air-Gapped Verification

**Document Version:** 1.0  
**Status:** Verified & Approved  
**Date:** August 24, 2026  
**Target Environment:** Air-Gapped Corporate Intranets, Sovereign Clouds, Docker / Nginx

---

## 1. Engine Offline & No-CDN Verification Matrix

| Engine | Local Asset Bundling | CDN Calls | External SaaS Dependency | Google/Public Fonts | Air-Gapped Docker / Nginx Compatible? |
| :--- | :--- | :---: | :---: | :---: | :---: |
| **AG Grid Enterprise** | `ag-grid.css`, `ag-theme-alpine.css` bundled locally | **ZERO** | None | System / Bundled WOFF2 | **YES (100% Offline)** |
| **FullCalendar** | Core & view styles compiled into local CSS | **ZERO** | None | System / Bundled WOFF2 | **YES (100% Offline)** |
| **Apache ECharts** | Vector engine self-contained in JS chunk | **ZERO** | None | Canvas / SVG Vector | **YES (100% Offline)** |
| **Tiptap Rich Text**| ProseMirror engine & DOMPurify bundled locally | **ZERO** | None | System / Bundled WOFF2 | **YES (100% Offline)** |
| **dnd-kit** | React DOM pointer event handlers bundled locally | **ZERO** | None | N/A | **YES (100% Offline)** |
| **Motion for React** | Layout projection & spring physics bundled locally | **ZERO** | None | N/A | **YES (100% Offline)** |

---

## 2. Commercial License Handling in Air-Gapped Deployments

### AG Grid Enterprise Offline Validation Model:
1. **License Key Injection:**
   - Injected via environment variable `VITE_AG_GRID_LICENSE_KEY` during container startup or CI build.
2. **Offline Validation Guarantee:**
   - AG Grid's license validator performs an **offline cryptographic checksum and expiry calculation in-memory**.
   - It performs **ZERO outbound HTTP/HTTPS requests** to AG Grid servers.
   - It functions seamlessly in completely air-gapped on-premise environments with no internet access.
3. **Startup Behavior Without Key:**
   - If no key is supplied, AG Grid operates in trial/evaluation mode, printing an informative notice to the browser console.
   - It does NOT throw fatal runtime errors or block UI rendering.
4. **Security & Secrecy:**
   - License keys are never checked into Git repository and are managed through Kubernetes Secrets or Vault in on-premise deployments.
