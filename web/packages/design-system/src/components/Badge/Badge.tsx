import * as React from "react"
import { cva, type VariantProps } from "class-variance-authority"
import { cn } from "../../lib/utils"

const badgeVariants = cva(
  "inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium transition-colors border",
  {
    variants: {
      variant: {
        neutral: "bg-surface-subtle border-border-default text-text-primary",
        primary: "bg-primary-subtle border-primary/20 text-primary-subtle-text",
        success: "bg-success-subtle border-success/20 text-success-subtle-text",
        warning: "bg-warning-subtle border-warning/20 text-warning-subtle-text",
        danger: "bg-danger-subtle border-danger/20 text-danger-subtle-text",
        info: "bg-info-subtle border-info/20 text-info-subtle-text",
        outline: "bg-transparent border-border-strong text-text-primary",
      },
      size: {
        sm: "px-2 py-0.2 text-[10px]",
        md: "px-2.5 py-0.5 text-xs",
        lg: "px-3 py-1 text-sm font-semibold",
      },
    },
    defaultVariants: {
      variant: "neutral",
      size: "md",
    },
  }
)

export interface BadgeProps
  extends React.HTMLAttributes<HTMLSpanElement>,
    VariantProps<typeof badgeVariants> {
  dot?: boolean
}

export function Badge({ className, variant, size, dot = false, children, ...props }: BadgeProps) {
  return (
    <span className={cn(badgeVariants({ variant, size }), className)} {...props}>
      {dot && (
        <span
          className={cn(
            "me-1.5 h-1.5 w-1.5 rounded-full",
            variant === "success" && "bg-success",
            variant === "warning" && "bg-warning",
            variant === "danger" && "bg-danger",
            variant === "info" && "bg-info",
            variant === "primary" && "bg-primary",
            (variant === "neutral" || variant === "outline" || !variant) && "bg-text-secondary"
          )}
        />
      )}
      {children}
    </span>
  )
}
