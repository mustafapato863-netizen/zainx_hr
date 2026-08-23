import * as React from "react"
import { cn } from "../../lib/utils"
import { Button } from "../Button/Button"
import { Icon } from "../Icon/Icon"

export interface BulkActionBarProps {
  className?: string
  selectedCount: number
  onClearSelection: () => void
  actions?: React.ReactNode
}

export function BulkActionBar({
  className,
  selectedCount,
  onClearSelection,
  actions,
}: BulkActionBarProps) {
  if (selectedCount === 0) return null

  return (
    <div className="fixed bottom-6 inset-x-0 z-40 flex justify-center pointer-events-none px-4">
      <div
        className={cn(
          "pointer-events-auto flex items-center justify-between gap-4 rounded-xl border border-border-default bg-surface-raised px-4 py-2.5 shadow-2xl animate-in slide-in-from-bottom-5 text-sm",
          className
        )}
      >
        <div className="flex items-center gap-3">
          <div className="flex h-6 w-6 items-center justify-center rounded-full bg-primary text-text-inverse text-xs font-bold">
            {selectedCount}
          </div>
          <span className="font-semibold text-text-primary text-xs">
            {selectedCount} item{selectedCount > 1 ? "s" : ""} selected
          </span>
          <button
            type="button"
            onClick={onClearSelection}
            className="text-xs text-text-tertiary hover:text-text-primary underline ms-1"
          >
            Deselect all
          </button>
        </div>

        {actions && <div className="flex items-center gap-2">{actions}</div>}
      </div>
    </div>
  )
}
