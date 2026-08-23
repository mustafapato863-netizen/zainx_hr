import * as React from "react"
import { cn } from "../../lib/utils"
import { Icon } from "../Icon/Icon"

export interface TagProps extends React.HTMLAttributes<HTMLSpanElement> {
  variant?: "neutral" | "primary" | "success" | "warning" | "danger" | "info"
  onRemove?: () => void
  disabled?: boolean
}

export function Tag({
  className,
  variant = "neutral",
  children,
  onRemove,
  disabled = false,
  ...props
}: TagProps) {
  const variantStyles = {
    neutral: "bg-surface-subtle border-border-default text-text-primary",
    primary: "bg-primary-subtle border-primary/20 text-primary-subtle-text",
    success: "bg-success-subtle border-success/20 text-success-subtle-text",
    warning: "bg-warning-subtle border-warning/20 text-warning-subtle-text",
    danger: "bg-danger-subtle border-danger/20 text-danger-subtle-text",
    info: "bg-info-subtle border-info/20 text-info-subtle-text",
  }

  return (
    <span
      className={cn(
        "inline-flex items-center gap-1.5 rounded-md border px-2 py-0.5 text-xs font-medium transition-colors",
        variantStyles[variant],
        className
      )}
      {...props}
    >
      <span>{children}</span>
      {onRemove && (
        <button
          type="button"
          disabled={disabled}
          onClick={(e) => {
            e.stopPropagation()
            onRemove()
          }}
          className="rounded p-0.5 hover:bg-surface/50 focus:outline-none focus:ring-1 focus:ring-primary disabled:opacity-50"
          aria-label="Remove tag"
        >
          <Icon name="x" size="xs" />
        </button>
      )}
    </span>
  )
}
