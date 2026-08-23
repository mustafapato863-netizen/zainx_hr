import * as React from "react"
import { cva, type VariantProps } from "class-variance-authority"
import { cn } from "../../lib/utils"
import { Icon } from "../Icon/Icon"

const alertVariants = cva(
  "relative w-full rounded-lg border p-4 text-sm flex gap-3 items-start",
  {
    variants: {
      variant: {
        neutral: "bg-surface-subtle border-border-default text-text-primary",
        info: "bg-info-subtle border-info/30 text-info-subtle-text",
        success: "bg-success-subtle border-success/30 text-success-subtle-text",
        warning: "bg-warning-subtle border-warning/30 text-warning-subtle-text",
        danger: "bg-danger-subtle border-danger/30 text-danger-subtle-text",
      },
    },
    defaultVariants: {
      variant: "neutral",
    },
  }
)

export interface AlertProps
  extends React.HTMLAttributes<HTMLDivElement>,
    VariantProps<typeof alertVariants> {
  title?: string
  icon?: boolean
  onClose?: () => void
}

export function Alert({
  className,
  variant = "neutral",
  title,
  icon = true,
  onClose,
  children,
  ...props
}: AlertProps) {
  const getIconName = () => {
    switch (variant) {
      case "info":
        return "info"
      case "success":
        return "check-circle"
      case "warning":
        return "alert-triangle"
      case "danger":
        return "alert-circle"
      default:
        return "info"
    }
  }

  return (
    <div
      role="alert"
      className={cn(alertVariants({ variant }), className)}
      {...props}
    >
      {icon && (
        <Icon
          name={getIconName()}
          size="md"
          className="shrink-0 mt-0.5"
        />
      )}
      <div className="flex-1 space-y-1">
        {title && (
          <h5 className="font-semibold leading-none tracking-tight">
            {title}
          </h5>
        )}
        <div className="text-sm opacity-90 leading-relaxed">{children}</div>
      </div>
      {onClose && (
        <button
          type="button"
          onClick={onClose}
          className="rounded p-1 opacity-70 hover:opacity-100 hover:bg-surface/20 focus:outline-none focus:ring-1 focus:ring-primary"
          aria-label="Dismiss alert"
        >
          <Icon name="x" size="xs" />
        </button>
      )}
    </div>
  )
}

export function Banner({
  className,
  variant = "info",
  children,
  onClose,
  ...props
}: AlertProps) {
  return (
    <div
      role="status"
      className={cn(
        "flex w-full items-center justify-between px-4 py-2.5 text-sm font-medium border-b",
        variant === "info" && "bg-info-subtle border-info/30 text-info-subtle-text",
        variant === "warning" && "bg-warning-subtle border-warning/30 text-warning-subtle-text",
        variant === "danger" && "bg-danger-subtle border-danger/30 text-danger-subtle-text",
        variant === "success" && "bg-success-subtle border-success/30 text-success-subtle-text",
        className
      )}
      {...props}
    >
      <div className="flex items-center gap-2 mx-auto">
        {children}
      </div>
      {onClose && (
        <button
          type="button"
          onClick={onClose}
          className="rounded p-1 hover:bg-surface/20 focus:outline-none focus:ring-1 focus:ring-primary"
          aria-label="Dismiss banner"
        >
          <Icon name="x" size="xs" />
        </button>
      )}
    </div>
  )
}
