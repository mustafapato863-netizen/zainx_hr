import * as React from "react"
import {
  Breadcrumbs as RACBreadcrumbs,
  Breadcrumb as RACBreadcrumb,
  BreadcrumbProps as RACBreadcrumbProps,
  Link as RACLink,
} from "react-aria-components"
import { cn } from "../../lib/utils"
import { Icon } from "../Icon/Icon"

export interface BreadcrumbItem {
  id: string
  label: string
  href?: string
  current?: boolean
}

export interface BreadcrumbProps {
  className?: string
  items: BreadcrumbItem[]
  onSelect?: (item: BreadcrumbItem) => void
}

export function Breadcrumb({ className, items, onSelect }: BreadcrumbProps) {
  return (
    <RACBreadcrumbs className={cn("flex flex-wrap items-center gap-1.5 text-xs text-text-tertiary", className)}>
      {items.map((item, index) => {
        const isLast = index === items.length - 1

        return (
          <RACBreadcrumb key={item.id} className="flex items-center gap-1.5">
            {isLast || item.current ? (
              <span className="font-semibold text-text-primary" aria-current="page">
                {item.label}
              </span>
            ) : (
              <RACLink
                href={item.href}
                onPress={() => onSelect?.(item)}
                className="text-text-secondary hover:text-primary transition-colors outline-none focus-visible:ring-1 focus-visible:ring-primary rounded"
              >
                {item.label}
              </RACLink>
            )}
            {!isLast && (
              <Icon
                name="chevron-right"
                size="xs"
                className="text-text-disabled"
              />
            )}
          </RACBreadcrumb>
        )
      })}
    </RACBreadcrumbs>
  )
}
