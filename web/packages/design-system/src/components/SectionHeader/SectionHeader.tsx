import * as React from "react"
import { cn } from "../../lib/utils"

export interface SectionHeaderProps extends React.HTMLAttributes<HTMLDivElement> {
  title: string
  description?: string
  actions?: React.ReactNode
}

export function SectionHeader({ className, title, description, actions, ...props }: SectionHeaderProps) {
  return (
    <div className={cn("flex items-center justify-between pb-3 mb-3 border-b border-border-default", className)} {...props}>
      <div>
        <h3 className="text-base font-semibold text-text-primary">{title}</h3>
        {description && <p className="text-xs text-text-secondary">{description}</p>}
      </div>
      {actions && <div className="flex items-center gap-2">{actions}</div>}
    </div>
  )
}
