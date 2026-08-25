import * as React from "react"
import type { JSONContent } from "@tiptap/react"
import { sanitizeRichText } from "./sanitizeUtils"
import TiptapView from "./TiptapView"

export type { JSONContent as ZainXRichTextJSON }
export { sanitizeRichText }

export interface ZainXRichTextEditorProps {
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
  return (
    <TiptapView
      className={className}
      value={value}
      onChange={onChange}
      jsonValue={jsonValue}
      onJsonChange={onJsonChange}
      placeholder={placeholder}
      isRtl={isRtl}
      readOnly={readOnly}
      minHeight={minHeight}
      ariaLabel={ariaLabel}
    />
  )
}
