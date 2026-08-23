import * as React from "react"
import { cn } from "../../lib/utils"

export interface PageToolbarProps extends React.HTMLAttributes<HTMLDivElement> {
  left?: React.ReactNode
  right?: React.ReactNode
}

export function PageToolbar({ className, left, right, children, ...props }: PageToolbarProps) {
  return (
    <div
      className={cn(
        "flex flex-wrap items-center justify-between gap-3 rounded-lg border border-border-default bg-surface p-3 mb-4 shadow-xs",
        className
      )}
      {...props}
    >
      <div className="flex flex-wrap items-center gap-2">{left}</div>
      <div className="flex flex-wrap items-center gap-2">{right}</div>
      {children}
    </div>
  )
}
