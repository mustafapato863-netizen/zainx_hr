import * as React from "react"
import { cn } from "../../lib/utils"

export interface PageHeaderProps extends React.HTMLAttributes<HTMLDivElement> {
  title: string
  subtitle?: string
  badge?: React.ReactNode
  breadcrumbs?: React.ReactNode
  actions?: React.ReactNode
}

export function PageHeader({
  className,
  title,
  subtitle,
  badge,
  breadcrumbs,
  actions,
  children,
  ...props
}: PageHeaderProps) {
  return (
    <div className={cn("flex flex-col gap-3 pb-5 mb-5 border-b border-border-default", className)} {...props}>
      {breadcrumbs && <div>{breadcrumbs}</div>}

      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div className="flex flex-col gap-1">
          <div className="flex items-center gap-3">
            <h1 className="text-2xl font-bold tracking-tight text-text-primary">
              {title}
            </h1>
            {badge}
          </div>
          {subtitle && (
            <p className="text-sm text-text-secondary">{subtitle}</p>
          )}
        </div>

        {actions && <div className="flex flex-wrap items-center gap-2">{actions}</div>}
      </div>

      {children}
    </div>
  )
}
