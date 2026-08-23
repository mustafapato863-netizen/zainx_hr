import * as React from "react"
import { cn } from "../../lib/utils"
import { Icon, IconName } from "../Icon/Icon"
import { Badge } from "../Badge/Badge"

export interface NavItem {
  id: string
  label: string
  icon: IconName
  href?: string
  badge?: string | number
  active?: boolean
  onClick?: () => void
}

export interface NavSection {
  title?: string
  items: NavItem[]
}

export interface SidebarProps extends React.HTMLAttributes<HTMLDivElement> {
  brand?: React.ReactNode
  sections?: NavSection[]
  footer?: React.ReactNode
}

export function Sidebar({ className, brand, sections = [], footer, children, ...props }: SidebarProps) {
  return (
    <div className={cn("flex h-full w-full flex-col justify-between p-3 select-none", className)} {...props}>
      <div>
        {brand && <div className="px-3 py-3 mb-2">{brand}</div>}

        <nav className="flex flex-col gap-4">
          {sections.map((section, idx) => (
            <div key={idx} className="flex flex-col gap-1">
              {section.title && (
                <div className="px-3 text-[11px] font-semibold uppercase tracking-wider text-text-tertiary">
                  {section.title}
                </div>
              )}
              {section.items.map((item) => (
                <button
                  key={item.id}
                  type="button"
                  onClick={item.onClick}
                  className={cn(
                    "group flex w-full items-center justify-between rounded-md px-3 py-2 text-sm font-medium transition-colors text-start",
                    item.active
                      ? "bg-primary text-text-inverse font-semibold shadow-xs"
                      : "text-text-secondary hover:bg-surface-subtle hover:text-text-primary"
                  )}
                >
                  <div className="flex items-center gap-2.5">
                    <Icon
                      name={item.icon}
                      size="sm"
                      className={cn(
                        "transition-colors",
                        item.active ? "text-text-inverse" : "text-text-tertiary group-hover:text-text-primary"
                      )}
                    />
                    <span>{item.label}</span>
                  </div>
                  {item.badge !== undefined && (
                    <Badge
                      variant={item.active ? "neutral" : "primary"}
                      size="sm"
                      className={item.active ? "bg-surface text-primary" : undefined}
                    >
                      {item.badge}
                    </Badge>
                  )}
                </button>
              ))}
            </div>
          ))}
          {children}
        </nav>
      </div>

      {footer && <div className="pt-3 border-t border-border-default">{footer}</div>}
    </div>
  )
}
