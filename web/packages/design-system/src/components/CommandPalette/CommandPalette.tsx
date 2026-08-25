import * as React from "react"
import { cn } from "../../lib/utils"
import { Icon, IconName } from "../Icon/Icon"

export interface CommandItem {
  id: string
  title: string
  subtitle?: string
  icon?: IconName
  shortcut?: string
  section?: string
  onSelect: () => void
}

export interface CommandPaletteProps {
  isOpen: boolean
  onClose: () => void
  onOpen?: () => void
  items?: CommandItem[]
}

export function CommandPalette({ isOpen, onClose, onOpen, items = [] }: CommandPaletteProps) {
  const [search, setSearch] = React.useState("")
  const inputRef = React.useRef<HTMLInputElement>(null)

  React.useEffect(() => {
    if (isOpen) {
      setTimeout(() => inputRef.current?.focus(), 50)
    } else {
      setSearch("")
    }
  }, [isOpen])

  React.useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if ((e.metaKey || e.ctrlKey) && e.key === "k") {
        e.preventDefault()
        if (isOpen) onClose()
        else onOpen?.()
      }
      if (e.key === "Escape" && isOpen) {
        onClose()
      }
    }
    window.addEventListener("keydown", handleKeyDown)
    return () => window.removeEventListener("keydown", handleKeyDown)
  }, [isOpen, onClose])

  if (!isOpen) return null

  const filteredItems = items.filter(
    (item) =>
      item.title.toLowerCase().includes(search.toLowerCase()) ||
      item.subtitle?.toLowerCase().includes(search.toLowerCase()) ||
      item.section?.toLowerCase().includes(search.toLowerCase())
  )

  return (
    <div className="fixed inset-0 z-50 flex items-start justify-center bg-surface-overlay p-4 pt-20 backdrop-blur-xs" role="dialog" aria-modal="true" aria-label="Command palette">
      <div className="w-full max-w-xl overflow-hidden rounded-xl border border-border-default bg-surface-raised shadow-2xl animate-in fade-in-0 zoom-in-95">
        <div className="flex items-center border-b border-border-default px-4 py-3">
          <Icon name="search" size="sm" className="text-text-tertiary me-3 shrink-0" />
          <input
            ref={inputRef}
            type="text"
            placeholder="Type a command or search..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="flex-1 bg-transparent text-sm text-text-primary outline-none placeholder:text-text-tertiary"
          />
          <kbd className="rounded border border-border-default bg-surface-subtle px-1.5 py-0.5 text-[10px] font-mono text-text-tertiary">
            ESC
          </kbd>
        </div>

        <div className="max-h-80 overflow-y-auto p-2">
          {filteredItems.length === 0 ? (
            <div className="py-8 text-center text-xs text-text-tertiary">
              No results found for "{search}"
            </div>
          ) : (
            <div className="space-y-1">
              {filteredItems.map((item) => (
                <button
                  key={item.id}
                  type="button"
                  onClick={() => {
                    item.onSelect()
                    onClose()
                  }}
                  className="flex w-full items-center justify-between rounded-md px-3 py-2 text-start text-sm text-text-primary hover:bg-surface-subtle focus:bg-surface-subtle focus:outline-none"
                >
                  <div className="flex items-center gap-3">
                    {item.icon && (
                      <Icon name={item.icon} size="sm" className="text-text-tertiary" />
                    )}
                    <div>
                      <div className="font-medium text-xs text-text-primary">{item.title}</div>
                      {item.subtitle && (
                        <div className="text-[11px] text-text-tertiary">{item.subtitle}</div>
                      )}
                    </div>
                  </div>
                  {item.shortcut && (
                    <kbd className="rounded border border-border-default bg-surface-subtle px-1.5 py-0.5 text-[10px] font-mono text-text-tertiary">
                      {item.shortcut}
                    </kbd>
                  )}
                </button>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  )
}
