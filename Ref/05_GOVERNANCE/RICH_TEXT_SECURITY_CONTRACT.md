# ZainX Rich Text Security & Storage Architecture Contract

**Document Version:** 1.0  
**Status:** Approved Architectural Contract  
**Date:** August 24, 2026  
**Owners:** Architecture / Security / Frontend Engineering

---

## 1. Executive Summary & Security Principle

Untrusted rich text is a primary vector for Cross-Site Scripting (XSS), data exfiltration, and presentation injection.

**Architectural Rule:** Browser-side sanitization is **NEVER** the sole security boundary. A multi-layered defense model is mandatory:
1. **Structured Ingress (Client):** Input is parsed against an allowlisted schema.
2. **Server-Side Validation (Ingress Gate):** Server validates and sanitizes before persistence.
3. **Canonical Storage:** Structured ProseMirror/Tiptap JSON (`JSONContent`) is the preferred persistent representation.
4. **Controlled Rendering (Egress Gate):** Safe DOM rendering with defense-in-depth sanitization and strict link policies.

---

## 2. Canonical Storage Representation

The preferred storage representation for all rich text across ZainX Workforce (Job Descriptions, Performance Review Notes, Policy Templates) is **Tiptap/ProseMirror Structured JSON (`JSONContent`)**:

```json
{
  "type": "doc",
  "content": [
    {
      "type": "heading",
      "attrs": { "level": 2 },
      "content": [{ "type": "text", "text": "Senior Workforce Architect" }]
    },
    {
      "type": "paragraph",
      "content": [
        { "type": "text", "text": "We require an experienced " },
        { "type": "text", "marks": [{ "type": "bold" }], "text": "Platform Lead" },
        { "type": "text", "text": " for on-premise systems." }
      ]
    }
  ]
}
```

### Advantages of Structured JSON Storage:
1. **Schema Enforcement:** Prevents arbitrary HTML element injection at the structural level.
2. **Cross-Platform Compatibility:** Easily serialized into PDF reports, Word documents, mobile screens, or plaintext email notifications without HTML parsing vulnerabilities.
3. **Auditability & Diffing:** Structural JSON tree allows fine-grained diffing and change tracking across revisions.

---

## 3. Allowed Node Types & Marks Schema

| Category | Allowed Schema Identifier | Restrictions & Attributes |
| :--- | :--- | :--- |
| **Document Root** | `doc` | Top-level document container |
| **Blocks** | `paragraph` | Text paragraphs |
| **Headings** | `heading` | `attrs.level`: `[1, 2, 3]` only (H1, H2, H3) |
| **Lists** | `bulletList`, `orderedList` | Unordered and ordered containers |
| **List Items** | `listItem` | Single item inside lists |
| **Inline Breaks** | `hardBreak` | Line breaks |
| **Text** | `text` | Plain text node |
| **Formatting Marks**| `bold`, `italic`, `strike`, `underline`, `code` | Inline text styling |
| **Links** | `link` | `attrs.href`: Allowed protocols `https:`, `http:`, `mailto:`, `tel:` only.<br>Enforced attributes: `target="_blank"`, `rel="noopener noreferrer nofollow"`. |

### Strictly Prohibited Elements:
- `<script>`, `<style>`, `<iframe>`, `<object>`, `<embed>`, `<applet>`, `<svg>`, `<canvas>`, `<form>`, `<input>`, `<button>`, `<link>`, `<meta>`.
- Any inline event handlers (`onload`, `onerror`, `onclick`, `onmouseover`, etc.).
- Dangerous protocols: `javascript:`, `data:`, `vbscript:`, `file:`.

---

## 4. Dual-Boundary Sanitization Matrix

```
[ User Input / Editor ] 
          │
          ▼
   (Client Pre-Filter) ──> DOMPurify URL/Tag Whitelist
          │
          ▼  (REST API Command Payload: Structured JSON or HTML)
[ Backend Ingress API ]
          │
          ▼
(Server-Side Sanitizer) ──> HtmlSanitizer / Ganss.Xss / JSON Schema Validator
          │
          ▼
[ Canonical Database ] (Stores validated JSONContent or Sanitized HTML)
          │
          ▼  (Read Query Payload)
[ Backend Egress API ]
          │
          ▼
[ Client Rendering / Email Export ]
          │
          ▼
(Client Defense-in-Depth) ──> Sanitized DOM rendering with strict rel policies
```

---

## 5. Export & Email Rendering Policy

1. **Email Notifications:**
   - Strip all CSS classes and interactive attributes.
   - Convert to standard email-safe HTML subset (`<p>`, `<strong>`, `<em>`, `<ul>`, `<ol>`, `<li>`, `<a>`).
   - Links must be fully qualified `https://` URLs with security tracking tokens if applicable.
2. **Document / PDF Export:**
   - Render through backend headless document generator from the canonical JSONContent tree, bypassing browser DOM rendering entirely.
