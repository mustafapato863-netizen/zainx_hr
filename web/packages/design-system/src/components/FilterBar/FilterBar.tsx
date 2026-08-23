import * as React from "react"
import { cn } from "../../lib/utils"
import { Icon } from "../Icon/Icon"
import { Button } from "../Button/Button"
import { Tag } from "../Tag/Tag"

export interface FilterItem {
  id: string
  label: string
  value: string
}

export interface FilterBarProps {
  className?: string
  filters?: FilterItem[]
  onRemoveFilter?: (id: string) => void
  onClearAll?: () => void
  onAddFilter?: () => void
  searchValue?: string
  onSearchChange?: (val: string) => void
}

export function FilterBar({
  className,
  filters = [],
  onRemoveFilter,
  onClearAll,
  onAddFilter,
  searchValue = "",
  onSearchChange,
}: FilterBarProps) {
  return (
    <div className={cn("flex flex-wrap items-center gap-2 py-2 text-xs", className)}>
      <div className="relative flex items-center min-w-[200px]">
        <Icon name="search" size="xs" className="absolute start-2.5 text-text-tertiary" />
        <input
          type="text"
          placeholder="Filter results..."
          value={searchValue}
          onChange={(e) => onSearchChange?.(e.target.value)}
          className="h-8 w-full rounded-md border border-border-default bg-surface-input ps-8 pe-3 text-xs text-text-primary focus:outline-none focus:ring-1 focus:ring-primary"
        />
      </div>

      {onAddFilter && (
        <Button variant="secondary" size="xs" onClick={onAddFilter} className="gap-1">
          <Icon name="filter" size="xs" />
          <span>Add Filter</span>
        </Button>
      )}

      {filters.map((f) => (
        <Tag
          key={f.id}
          variant="primary"
          onRemove={() => onRemoveFilter?.(f.id)}
        >
          <span className="font-semibold me-1">{f.label}:</span>
          <span>{f.value}</span>
        </Tag>
      ))}

      {filters.length > 0 && onClearAll && (
        <button
          type="button"
          onClick={onClearAll}
          className="text-xs text-text-tertiary hover:text-text-primary underline cursor-pointer ms-1"
        >
          Clear all
        </button>
      )}
    </div>
  )
}
