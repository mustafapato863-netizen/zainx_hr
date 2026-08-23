import * as React from "react"
import { cn } from "../../lib/utils"
import { Icon } from "../Icon/Icon"
import { IconButton } from "../Button/IconButton"

export interface TopbarProps extends React.HTMLAttributes<HTMLDivElement> {
  left?: React.ReactNode
  search?: React.ReactNode
  contextSwitcher?: React.ReactNode
  actions?: React.ReactNode
  user?: React.ReactNode
  onSearchClick?: () => void
  onToggleSidebar?: () => void
}

export function Topbar({
  className,
  left,
  search,
  contextSwitcher,
  actions,
  user,
  onSearchClick,
  onToggleSidebar,
  ...props
}: TopbarProps) {
  return (
    <div
      className={cn("flex w-full items-center justify-between gap-4", className)}
      {...props}
    >
      <div className="flex items-center gap-3">
        {onToggleSidebar && (
          <IconButton
            variant="ghost"
            size="icon-sm"
            aria-label="Toggle Navigation"
            className="md:hidden"
            onClick={onToggleSidebar}
          >
            <Icon name="menu" size="sm" />
          </IconButton>
        )}
        {left}
        {contextSwitcher}
      </div>

      <div className="flex-1 max-w-md hidden sm:block">
        {search ? (
          search
        ) : onSearchClick ? (
          <button
            type="button"
            onClick={onSearchClick}
            className="flex w-full items-center justify-between rounded-md border border-border-default bg-surface-subtle px-3 py-1.5 text-xs text-text-tertiary hover:border-border-strong focus:outline-none focus:ring-1 focus:ring-primary"
          >
            <div className="flex items-center gap-2">
              <Icon name="search" size="xs" />
              <span>Search employees, actions, reports...</span>
            </div>
            <kbd className="rounded border border-border-default bg-surface px-1.5 py-0.5 text-[10px] font-mono text-text-tertiary">
              Ctrl+K
            </kbd>
          </button>
        ) : null}
      </div>

      <div className="flex items-center gap-2">
        {actions}
        {user}
      </div>
    </div>
  )
}
