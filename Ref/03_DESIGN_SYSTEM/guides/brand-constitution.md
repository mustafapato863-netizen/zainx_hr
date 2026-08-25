# ZainX Workforce Brand Constitution

**Status:** Approved-artwork implementation constitution; SVG master still pending  
**Scope:** Workforce web application UI  
**Authority:** Design source of truth for the current frontend recovery initiative

## Product promise

ZainX Workforce makes workforce operations easier to understand and safer to act on. The interface should communicate calm control, human context, and trustworthy data.

## Approved brand source

The application brand source of truth is the approved artwork kit at:

`D:\Projects\ZainX_HR\Ref\03_DESIGN_SYSTEM\ZainX_HR_Brand_Kit_v1.1_APPROVED`

Use the exact approved mark, wordmark, lockup, light/dark variants, PWA icons, and motion snippets from this kit. Do not replace them with regenerated, reconstructed, or alternate Gemini artwork. Runtime copies must remain traceable to this directory.

## Design direction

**Editorial Enterprise + Operational Clarity + Human Workforce**

The product is a serious operational tool. It should feel premium through typography, composition, surface hierarchy, and intentional detail—not through decoration or novelty.

## Visual system

- **Foundation:** ink navy, mineral canvas, quiet surfaces, hairline dividers.
- **Primary accent:** cyan derived from the current ZainX working identity.
- **Support accent:** sand for selected brand moments and human warmth.
- **Semantic colors:** green, amber, red, and blue are reserved for status meaning.
- **Depth:** flat by default; raised for grouped work; overlay for dialogs and command surfaces.
- **Radius:** compact controls use small/medium radii; panels use one consistent large radius; pills are reserved for statuses.
- **Backgrounds:** operational pages stay quiet; Home and selected brand moments may use the ZainX grid treatment.
- **Prohibited:** default purple AI gradients, full-screen glow, repeated card grids, decorative fake metrics, and unlicensed imagery.

## Typography

The current implementation uses a local/system-first stack (`Segoe UI Variable`, `Segoe UI`, and `Tahoma` for Arabic) so offline and on-premise builds do not depend on a remote font. A self-hosted Arabic-compatible variable family can replace this stack after licensing and asset approval. Maximum budget: two families and four weights.

## Motion

Motion is used for state change, hierarchy, and feedback. Cold-start BrandAssembly is limited to 800–1200ms, route changes use layout-matched skeletons, and `prefers-reduced-motion` receives a static experience. GSAP remains rejected; the approved Motion decision remains unchanged.

## Arabic and RTL

- Direction comes from the active locale, never from a page-specific override.
- Logical properties (`start`, `end`, `ms`, `me`) are preferred.
- Directional icons mirror through the design-system icon contract.
- Numbers, dates, currency, and status labels remain legible in both directions.
- Arabic typography is not an afterthought: line-height, truncation, and mobile wrapping require verification.

## Component token contract

```text
Primitive tokens → Semantic tokens → Component tokens
```

Feature components consume semantic or component tokens. New raw colors, arbitrary shadows, and ad-hoc radii require a documented exception.

## Voice

Use concise, direct, respectful language. Explain what is happening, what is unavailable, and what the user can do next. Never imply that unavailable data is zero and never hide authorization or service errors behind empty states.

## Approved asset rule

`BrandMark` and `BrandAssembly` use the approved v1.1 raster artwork and its controlled Motion choreography. An official SVG master may replace the raster source only after formal approval and visual equivalence review; it must not change the silhouette, human figure, wordmark, or Blue → Cyan treatment. No alternate image or font may enter the production bundle without license and offline-build verification.
