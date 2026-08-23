import * as React from "react"
import { cn } from "../../lib/utils"
import { Icon } from "../Icon/Icon"
import { Button } from "../Button/Button"

export interface AccessDeniedProps extends React.HTMLAttributes<HTMLDivElement> {
  title?: string
  description?: string
  /** Capability / permission token required (e.g. 'payroll.run.finalize') */
  requiredPermission?: string
  /** Diagnostic correlation ID for audit and support tracing */
  correlationId?: string
  /** Optional callback to return to previous view */
  onGoBack?: () => void
  /** Optional callback to request permission via workflow */
  onRequestAccess?: () => void
}

export function AccessDenied({
  className,
  title = "Access Denied",
  description = "You do not have the required permission to access or modify this resource.",
  requiredPermission,
  correlationId,
  onGoBack,
  onRequestAccess,
  ...props
}: AccessDeniedProps) {
  return (
    <div
      role="alert"
      className={cn(
        "flex min-h-[360px] flex-col items-center justify-center rounded-xl border border-danger/20 bg-surface p-8 text-center",
        className
      )}
      {...props}
    >
      <div className="mb-4 flex h-14 w-14 items-center justify-center rounded-full bg-danger-subtle text-danger">
        <Icon name="shield-alert" size="lg" />
      </div>
      <h3 className="mb-2 text-xl font-bold text-text-primary">{title}</h3>
      <p className="mb-4 max-w-md text-sm text-text-secondary">{description}</p>
      
      {requiredPermission && (
        <div className="mb-4 rounded-md bg-surface-subtle border border-border-default px-3 py-1.5 text-xs text-text-tertiary">
          Required capability: <code className="font-mono font-semibold text-text-primary">{requiredPermission}</code>
        </div>
      )}

      {correlationId && (
        <div className="mb-6 text-[11px] font-mono text-text-tertiary">
          Trace ID: {correlationId}
        </div>
      )}

      <div className="flex flex-wrap items-center justify-center gap-3">
        {onGoBack && (
          <Button variant="secondary" size="sm" onClick={onGoBack}>
            Return to previous page
          </Button>
        )}
        {onRequestAccess && (
          <Button variant="primary" size="sm" onClick={onRequestAccess}>
            Request Access
          </Button>
        )}
      </div>
    </div>
  )
}
