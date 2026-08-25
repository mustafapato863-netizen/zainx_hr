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
    <div className={cn("relative mb-6 flex flex-col gap-3 border-b border-border-default pb-6", className)} {...props}>
      {breadcrumbs && <div>{breadcrumbs}</div>}

      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div className="flex flex-col gap-1">
          <div className="flex items-center gap-3">
            <h1 className="max-w-3xl text-[clamp(1.65rem,2vw,2.25rem)] font-semibold leading-tight tracking-[-0.025em] text-text-primary">
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
