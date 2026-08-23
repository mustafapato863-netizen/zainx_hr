import * as React from "react"
import { cn } from "../../lib/utils"
import { Icon } from "../Icon/Icon"
import { Menu, MenuItem, MenuSection } from "../Menu/Menu"

export interface SavedViewItem {
  id: string
  name: string
  isDefault?: boolean
}

export interface SavedViewsProps {
  className?: string
  currentView?: SavedViewItem
  views?: SavedViewItem[]
  onSelectView?: (view: SavedViewItem) => void
  onSaveCurrentView?: () => void
}

export function SavedViews({
  className,
  currentView = { id: "view-default", name: "Default View", isDefault: true },
  views = [
    { id: "view-default", name: "Default View", isDefault: true },
    { id: "view-active", name: "Active Employees" },
    { id: "view-probation", name: "On Probation" },
    { id: "view-payroll-ready", name: "Payroll Ready" },
  ],
  onSelectView,
  onSaveCurrentView,
}: SavedViewsProps) {
  return (
    <div className={cn("inline-flex items-center", className)}>
      <Menu
        trigger={
          <button
            type="button"
            className="flex items-center gap-1.5 rounded-md border border-border-default bg-surface px-2.5 py-1 text-xs font-medium text-text-primary hover:bg-surface-subtle focus:outline-none focus:ring-1 focus:ring-primary shadow-xs"
          >
            <Icon name="columns" size="xs" className="text-text-tertiary" />
            <span>{currentView.name}</span>
            <Icon name="chevron-down" size="xs" className="text-text-tertiary opacity-70" />
          </button>
        }
      >
        <MenuSection title="Saved Table Views">
          {views.map((v) => (
            <MenuItem key={v.id} onAction={() => onSelectView?.(v)}>
              <div className="flex w-full items-center justify-between gap-4">
                <span>{v.name}</span>
                {v.id === currentView.id && (
                  <Icon name="check" size="xs" className="text-primary" />
                )}
              </div>
            </MenuItem>
          ))}
        </MenuSection>
        {onSaveCurrentView && (
          <MenuSection>
            <MenuItem onAction={onSaveCurrentView}>
              <div className="flex items-center gap-1.5 text-primary">
                <Icon name="plus" size="xs" />
                <span>Save current view</span>
              </div>
            </MenuItem>
          </MenuSection>
        )}
      </Menu>
    </div>
  )
}
