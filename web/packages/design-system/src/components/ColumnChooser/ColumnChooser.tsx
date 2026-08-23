import * as React from "react"
import { cn } from "../../lib/utils"
import { Icon } from "../Icon/Icon"
import { Popover } from "../Popover/Popover"
import { Checkbox } from "../Checkbox/Checkbox"
import { Button } from "../Button/Button"

export interface ColumnItem {
  id: string
  label: string
  visible: boolean
  required?: boolean
}

export interface ColumnChooserProps {
  className?: string
  columns: ColumnItem[]
  onToggleColumn: (id: string, visible: boolean) => void
  onReset?: () => void
}

export function ColumnChooser({
  className,
  columns,
  onToggleColumn,
  onReset,
}: ColumnChooserProps) {
  return (
    <div className={cn("inline-flex items-center", className)}>
      <Popover
        trigger={
          <Button variant="secondary" size="xs" className="gap-1.5 shadow-xs">
            <Icon name="columns" size="xs" />
            <span>Columns</span>
          </Button>
        }
      >
        <div className="w-56 space-y-3">
          <div className="flex items-center justify-between border-b border-border-default pb-2">
            <span className="text-xs font-semibold text-text-primary">Customize Columns</span>
            {onReset && (
              <button
                type="button"
                onClick={onReset}
                className="text-[11px] text-primary hover:underline"
              >
                Reset
              </button>
            )}
          </div>

          <div className="max-h-60 overflow-y-auto space-y-2 py-1">
            {columns.map((col) => (
              <Checkbox
                key={col.id}
                isSelected={col.visible}
                isDisabled={col.required}
                onChange={(isChecked) => onToggleColumn(col.id, isChecked)}
              >
                <span className="text-xs text-text-primary">
                  {col.label} {col.required && "(Required)"}
                </span>
              </Checkbox>
            ))}
          </div>
        </div>
      </Popover>
    </div>
  )
}
