import * as React from "react"
import { cn } from "../../lib/utils"
import { Button } from "../Button/Button"
import { Icon, IconName } from "../Icon/Icon"
import { Menu, MenuItem, MenuSection } from "../Menu/Menu"

export interface QuickCreateItem {
  id: string
  label: string
  description?: string
  icon?: IconName
  permission?: string
  entitlement?: string
  disabled?: boolean
  onAction?: () => void
}

export interface QuickCreateProps {
  className?: string
  buttonLabel?: string
  title?: string
  /** The application/platform layer supplies authorized and entitled items */
  items: QuickCreateItem[]
}

export function QuickCreate({
  className,
  buttonLabel = "Create",
  title = "Quick Actions",
  items = [],
}: QuickCreateProps) {
  if (items.length === 0) return null

  return (
    <div className={cn("inline-flex items-center", className)}>
      <Menu
        trigger={
          <Button variant="primary" size="sm" className="gap-1.5 shadow-xs">
            <Icon name="plus" size="xs" />
            <span>{buttonLabel}</span>
            <Icon name="chevron-down" size="xs" className="opacity-80" />
          </Button>
        }
      >
        <MenuSection title={title}>
          {items.map((item) => (
            <MenuItem
              key={item.id}
              isDisabled={item.disabled}
              onAction={item.onAction}
            >
              <div className="flex items-center gap-2.5 py-0.5">
                {item.icon && <Icon name={item.icon} size="sm" className="text-primary" />}
                <div className="flex flex-col text-start">
                  <span className="font-medium text-xs text-text-primary">{item.label}</span>
                  {item.description && (
                    <span className="text-[10px] text-text-tertiary">{item.description}</span>
                  )}
                </div>
              </div>
            </MenuItem>
          ))}
        </MenuSection>
      </Menu>
    </div>
  )
}
