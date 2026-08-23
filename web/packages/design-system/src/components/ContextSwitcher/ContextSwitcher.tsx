import * as React from "react"
import { cn } from "../../lib/utils"
import { Icon } from "../Icon/Icon"
import { Menu, MenuItem, MenuSection, MenuSeparator } from "../Menu/Menu"

export interface TenantContext {
  id: string
  name: string
  code: string
  type: "holding" | "entity" | "subsidiary"
}

export interface ContextSwitcherProps {
  className?: string
  currentContext?: TenantContext
  contexts?: TenantContext[]
  onSelectContext?: (context: TenantContext) => void
}

export function ContextSwitcher({
  className,
  currentContext = {
    id: "corp-1",
    name: "ZainX Holding Group",
    code: "ZHG-HQ",
    type: "holding",
  },
  contexts = [
    { id: "corp-1", name: "ZainX Holding Group", code: "ZHG-HQ", type: "holding" },
    { id: "corp-2", name: "ZainX Saudi Arabia LLC", code: "ZSA-01", type: "entity" },
    { id: "corp-3", name: "ZainX UAE Tech Hub", code: "ZAE-02", type: "subsidiary" },
  ],
  onSelectContext,
}: ContextSwitcherProps) {
  return (
    <div className={cn("inline-flex items-center", className)}>
      <Menu
        trigger={
          <button
            type="button"
            className="flex items-center gap-2 rounded-md border border-border-default bg-surface px-2.5 py-1 text-xs font-medium text-text-primary hover:bg-surface-subtle focus:outline-none focus:ring-1 focus:ring-primary shadow-xs"
          >
            <Icon name="building" size="xs" className="text-primary" />
            <div className="flex flex-col text-start">
              <span className="font-semibold leading-tight">{currentContext.name}</span>
              <span className="text-[10px] text-text-tertiary">{currentContext.code}</span>
            </div>
            <Icon name="chevron-down" size="xs" className="text-text-tertiary ms-1" />
          </button>
        }
      >
        <MenuSection title="Legal Entities / Workspaces">
          {contexts.map((ctx) => (
            <MenuItem
              key={ctx.id}
              onAction={() => onSelectContext?.(ctx)}
            >
              <div className="flex w-full items-center justify-between gap-4">
                <div className="flex flex-col">
                  <span className="font-medium text-xs text-text-primary">{ctx.name}</span>
                  <span className="text-[10px] text-text-tertiary">{ctx.code} ({ctx.type})</span>
                </div>
                {ctx.id === currentContext.id && (
                  <Icon name="check" size="xs" className="text-primary" />
                )}
              </div>
            </MenuItem>
          ))}
        </MenuSection>
        <MenuSeparator />
        <MenuItem onAction={() => {}}>
          <div className="flex items-center gap-1.5 text-xs text-text-secondary">
            <Icon name="settings" size="xs" />
            <span>Manage Organizations</span>
          </div>
        </MenuItem>
      </Menu>
    </div>
  )
}
