import * as React from "react"
import { cn } from "../../lib/utils"
import { Button } from "../Button/Button"
import { Icon } from "../Icon/Icon"

export interface ErrorStateProps extends React.HTMLAttributes<HTMLDivElement> {
  icon?: React.ReactNode
  title?: string
  description?: string
  action?: React.ReactNode
  onRetry?: () => void
  retryLabel?: string
}

const ErrorState = React.forwardRef<HTMLDivElement, ErrorStateProps>(
  (
    {
      className,
      icon,
      title = "Something went wrong",
      description = "We couldn't load the requested data. Please try again.",
      action,
      onRetry,
      retryLabel = "Try Again",
      ...props
    },
    ref
  ) => {
    return (
      <div
        ref={ref}
        className={cn(
          "flex flex-col items-center justify-center rounded-lg border border-danger/20 bg-danger/5 p-8 text-center animate-in fade-in-50",
          className
        )}
        {...props}
      >
        {icon ? (
          <div className="mb-4 text-danger">{icon}</div>
        ) : (
          <div className="mb-4 flex h-12 w-12 items-center justify-center rounded-full bg-danger/10 text-danger">
            <Icon name="alert-triangle" size="md" />
          </div>
        )}
        <h3 className="mb-2 text-lg font-semibold text-text-primary">{title}</h3>
        <p className="mb-6 max-w-sm text-sm text-text-secondary">{description}</p>
        {action ? (
          <div>{action}</div>
        ) : (
          onRetry && (
            <Button variant="secondary" size="sm" onClick={onRetry}>
              {retryLabel}
            </Button>
          )
        )}
      </div>
    )
  }
)
ErrorState.displayName = "ErrorState"

export { ErrorState }
