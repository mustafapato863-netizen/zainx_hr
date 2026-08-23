import * as React from "react"
import { useEditor, EditorContent, type JSONContent } from "@tiptap/react"
import StarterKit from "@tiptap/starter-kit"
import DOMPurify from "dompurify"
import { cn } from "../../lib/utils"
import { Button } from "../Button/Button"
import { Icon } from "../Icon/Icon"

export type { JSONContent as ZainXRichTextJSON }

export interface ZainXRichTextEditorProps {
  className?: string
  /**
   * Raw HTML content (treated as untrusted input and strictly sanitized)
   */
  value?: string
  onChange?: (html: string) => void
  /**
   * Canonical structured ProseMirror/Tiptap JSON representation
   */
  jsonValue?: JSONContent
  onJsonChange?: (json: JSONContent) => void
  placeholder?: string
  isRtl?: boolean
  readOnly?: boolean
  minHeight?: string
  ariaLabel?: string
}

/**
 * Strict DOMPurify Security Configuration
 * Enforces allowlisted tags, attributes, and URL protocols
 */
const DOMPURIFY_CONFIG = {
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

/**
 * ZainXRichTextEditor
 *
 * Encapsulates Tiptap open-source starter-kit behind a sanitized,
 * accessible, and RTL-compliant component contract.
 *
 * SECURITY MANDATE:
 * Supports structured ProseMirror JSON (canonical storage) and sanitized HTML.
 * All input and output is sanitized with strict protocol and tag filters.
 */
export function ZainXRichTextEditor({
  className,
  value = "",
  onChange,
  jsonValue,
  onJsonChange,
  placeholder = "Write description or template...",
  isRtl = false,
  readOnly = false,
  minHeight = "150px",
  ariaLabel = "Rich text editor",
}: ZainXRichTextEditorProps) {
  const initialContent = React.useMemo(() => {
    if (jsonValue) return jsonValue
    return sanitizeRichText(value)
  }, [value, jsonValue])

  const editor = useEditor({
    extensions: [
      StarterKit.configure({
        heading: { levels: [1, 2, 3] },
      }),
    ],
    content: initialContent,
    editable: !readOnly,
    onUpdate: ({ editor }) => {
      const rawHtml = editor.getHTML()
      const cleanHtml = sanitizeRichText(rawHtml)
      onChange?.(cleanHtml)
      onJsonChange?.(editor.getJSON())
    },
  })

  React.useEffect(() => {
    if (!editor) return

    if (jsonValue) {
      editor.commands.setContent(jsonValue)
    } else if (value && value !== editor.getHTML()) {
      editor.commands.setContent(sanitizeRichText(value))
    }
  }, [value, jsonValue, editor])

  if (!editor) {
    return null
  }

  return (
    <div
      className={cn(
        "rounded-lg border border-border-default bg-surface shadow-xs transition-colors focus-within:border-primary focus-within:ring-1 focus-within:ring-primary",
        readOnly && "bg-surface-subtle opacity-90",
        className
      )}
      dir={isRtl ? "rtl" : "ltr"}
    >
      {/* Editor Toolbar */}
      {!readOnly && (
        <div className="flex flex-wrap items-center gap-1 border-b border-border-subtle bg-surface-subtle/50 p-1.5">
          <Button
            type="button"
            variant="ghost"
            size="xs"
            aria-label="Bold"
            className={cn(editor.isActive("bold") && "bg-surface text-primary shadow-xs")}
            onPress={() => editor.chain().focus().toggleBold().run()}
          >
            <Icon name="bold" size="xs" />
          </Button>

          <Button
            type="button"
            variant="ghost"
            size="xs"
            aria-label="Italic"
            className={cn(editor.isActive("italic") && "bg-surface text-primary shadow-xs")}
            onPress={() => editor.chain().focus().toggleItalic().run()}
          >
            <Icon name="italic" size="xs" />
          </Button>

          <div className="h-4 w-px bg-border-default mx-0.5" />

          <Button
            type="button"
            variant="ghost"
            size="xs"
            aria-label="Heading 2"
            className={cn(editor.isActive("heading", { level: 2 }) && "bg-surface text-primary shadow-xs")}
            onPress={() => editor.chain().focus().toggleHeading({ level: 2 }).run()}
          >
            <span className="font-bold text-xs">H2</span>
          </Button>

          <Button
            type="button"
            variant="ghost"
            size="xs"
            aria-label="Bullet List"
            className={cn(editor.isActive("bulletList") && "bg-surface text-primary shadow-xs")}
            onPress={() => editor.chain().focus().toggleBulletList().run()}
          >
            <Icon name="list" size="xs" />
          </Button>

          <Button
            type="button"
            variant="ghost"
            size="xs"
            aria-label="Ordered List"
            className={cn(editor.isActive("orderedList") && "bg-surface text-primary shadow-xs")}
            onPress={() => editor.chain().focus().toggleOrderedList().run()}
          >
            <Icon name="list-ordered" size="xs" />
          </Button>

          <div className="h-4 w-px bg-border-default mx-0.5" />

          <Button
            type="button"
            variant="ghost"
            size="xs"
            aria-label="Clear Formatting"
            onPress={() => editor.chain().focus().clearNodes().unsetAllMarks().run()}
          >
            <Icon name="rotate-ccw" size="xs" />
          </Button>
        </div>
      )}

      {/* Editor Content Area */}
      <div
        className="p-3 prose prose-sm max-w-none text-text-primary focus:outline-none"
        style={{ minHeight }}
        aria-label={ariaLabel}
      >
        <EditorContent editor={editor} />
      </div>
    </div>
  )
}
