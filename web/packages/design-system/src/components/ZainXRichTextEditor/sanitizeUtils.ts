import DOMPurify from "dompurify"

/**
 * Strict DOMPurify Security Configuration
 * Enforces allowlisted tags, attributes, and URL protocols
 */
export const DOMPURIFY_CONFIG = {
  ALLOWED_TAGS: [
    "p",
    "h1",
    "h2",
    "h3",
    "strong",
    "b",
    "em",
    "i",
    "s",
    "strike",
    "u",
    "code",
    "pre",
    "ul",
    "ol",
    "li",
    "br",
    "a",
    "span",
  ],
  ALLOWED_ATTR: ["href", "target", "rel", "class", "dir"],
  ALLOWED_URI_REGEXP: /^(?:(?:https?|mailto|tel):|[^a-z]|[a-z+.-]+(?:[^a-z+.-:]|$))/i,
  FORBID_TAGS: [
    "script",
    "style",
    "iframe",
    "object",
    "embed",
    "form",
    "input",
    "button",
    "svg",
    "canvas",
    "link",
    "meta",
  ],
  FORBID_ATTR: [
    "onload",
    "onerror",
    "onclick",
    "onmouseover",
    "onfocus",
    "onblur",
    "style",
    "action",
    "formaction",
  ],
}

/**
 * Sanitize untrusted HTML with strict URL, attribute, and rel tag policies.
 */
export function sanitizeRichText(dirtyHtml: string): string {
  if (!dirtyHtml) return ""

  // Add DOMPurify hook to enforce safe rel attributes on links
  DOMPurify.addHook("afterSanitizeAttributes", (node) => {
    if (node.tagName === "A" && node.hasAttribute("href")) {
      node.setAttribute("rel", "noopener noreferrer nofollow")
      node.setAttribute("target", "_blank")
    }
  })

  const clean = DOMPurify.sanitize(dirtyHtml, DOMPURIFY_CONFIG)
  DOMPurify.removeHook("afterSanitizeAttributes")
  return clean as unknown as string
}
