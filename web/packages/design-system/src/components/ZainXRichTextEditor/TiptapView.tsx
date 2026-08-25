import * as React from "react"
import { useEditor, EditorContent, type JSONContent } from "@tiptap/react"
import StarterKit from "@tiptap/starter-kit"
import { sanitizeRichText } from "./sanitizeUtils"
import { cn } from "../../lib/utils"
import { Button } from "../Button/Button"
import { Icon } from "../Icon/Icon"

export interface TiptapViewProps {
  className?: string
  value?: string
  onChange?: (html: string) => void
  jsonValue?: JSONContent
  onJsonChange?: (json: JSONContent) => void
  placeholder?: string
  isRtl?: boolean
  readOnly?: boolean
  minHeight?: string
  ariaLabel?: string
}

export default function TiptapView({
  className,
  value = "",
  onChange,
  jsonValue,
  onJsonChange,
  isRtl = false,
  readOnly = false,
  minHeight = "150px",
  ariaLabel = "Rich text editor",
}: TiptapViewProps) {
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
    onUpdate: ({ editor: currentEditor }) => {
      const rawHtml = currentEditor.getHTML()
      const cleanHtml = sanitizeRichText(rawHtml)
      onChange?.(cleanHtml)
      onJsonChange?.(currentEditor.getJSON())
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
    return <div className="p-4 text-xs text-text-secondary" style={{ minHeight }}>Loading editor…</div>
  }

  return (
    <div
      className={cn(
        "rounded-lg border border-border-default bg-surface shadow-xs transition-colors focus-within:border-border-focus",
        readOnly && "bg-surface-subtle opacity-90",
        className
      )}
      dir={isRtl ? "rtl" : "ltr"}
    >
      {/* Accessible Formatting Toolbar */}
      {!readOnly && (
        <div
          className="flex flex-wrap items-center gap-1 border-b border-border-subtle bg-surface-subtle p-1.5"
          role="toolbar"
          aria-label={ariaLabel}
        >
          <Button
            variant={editor.isActive("bold") ? "secondary" : "ghost"}
            size="xs"
            aria-label="Bold (Ctrl+B)"
            onPress={() => editor.chain().focus().toggleBold().run()}
          >
            <Icon name="bold" size="xs" />
          </Button>
          <Button
            variant={editor.isActive("italic") ? "secondary" : "ghost"}
            size="xs"
            aria-label="Italic (Ctrl+I)"
            onPress={() => editor.chain().focus().toggleItalic().run()}
          >
            <Icon name="italic" size="xs" />
          </Button>
          <Button
            variant={editor.isActive("strike") ? "secondary" : "ghost"}
            size="xs"
            aria-label="Strikethrough"
            onPress={() => editor.chain().focus().toggleStrike().run()}
          >
            <Icon name="minus" size="xs" />
          </Button>

          <div className="h-4 w-px bg-border-default mx-1" />

          <Button
            variant={editor.isActive("heading", { level: 1 }) ? "secondary" : "ghost"}
            size="xs"
            aria-label="Heading 1"
            onPress={() => editor.chain().focus().toggleHeading({ level: 1 }).run()}
          >
            H1
          </Button>
          <Button
            variant={editor.isActive("heading", { level: 2 }) ? "secondary" : "ghost"}
            size="xs"
            aria-label="Heading 2"
            onPress={() => editor.chain().focus().toggleHeading({ level: 2 }).run()}
          >
            H2
          </Button>

          <div className="h-4 w-px bg-border-default mx-1" />

          <Button
            variant={editor.isActive("bulletList") ? "secondary" : "ghost"}
            size="xs"
            aria-label="Bullet List"
            onPress={() => editor.chain().focus().toggleBulletList().run()}
          >
            <Icon name="list" size="xs" />
          </Button>
          <Button
            variant={editor.isActive("orderedList") ? "secondary" : "ghost"}
            size="xs"
            aria-label="Numbered List"
            onPress={() => editor.chain().focus().toggleOrderedList().run()}
          >
            <Icon name="list-ordered" size="xs" />
          </Button>

          <div className="h-4 w-px bg-border-default mx-1" />

          <Button
            variant="ghost"
            size="xs"
            aria-label="Undo"
            isDisabled={!editor.can().undo()}
            onPress={() => editor.chain().focus().undo().run()}
          >
            <Icon name="rotate-ccw" size="xs" />
          </Button>
          <Button
            variant="ghost"
            size="xs"
            aria-label="Redo"
            isDisabled={!editor.can().redo()}
            onPress={() => editor.chain().focus().redo().run()}
          >
            <Icon name="refresh" size="xs" />
          </Button>
        </div>
      )}

      {/* Editor Content Area */}
      <EditorContent
        editor={editor}
        className={cn(
          "prose max-w-none p-3 text-sm text-text-primary focus:outline-hidden",
          isRtl && "text-right"
        )}
        style={{ minHeight }}
      />
    </div>
  )
}
