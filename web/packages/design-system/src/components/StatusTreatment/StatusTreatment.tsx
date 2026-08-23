import * as React from "react"
import { cn } from "../../lib/utils"
import { Icon } from "../Icon/Icon"
import { Badge } from "../Badge/Badge"

export interface StatusTreatmentProps {
  className?: string
  type: "read-only" | "locked" | "finalized"
  reason?: string
  by?: string
  at?: string
}

export function StatusTreatment({
  className,
  type,
  reason,
  by,
  at,
}: StatusTreatmentProps) {
  const configs = {
    "read-only": {
      label: "Read Only",
      icon: "eye" as const,
      variant: "neutral" as const,
      defaultReason: "This record is currently in view-only mode.",
    },
    locked: {
      label: "Locked Record",
      icon: "lock" as const,
      variant: "warning" as const,
      defaultReason: "This record is locked from editing during active processing.",
    },
    finalized: {
      label: "Finalized / Closed",
      icon: "check-circle" as const,
      variant: "success" as const,
      defaultReason: "This period has been finalized. Changes require authorized reopening.",
    },
  }

  const config = configs[type]

  return (
    <div
      className={cn(
        "flex flex-wrap items-center justify-between gap-3 rounded-lg border p-3 text-sm",
        type === "read-only" && "border-border-default bg-surface-subtle",
        type === "locked" && "border-warning/30 bg-warning-subtle text-warning-subtle-text",
        type === "finalized" && "border-success/30 bg-success-subtle text-success-subtle-text",
        className
      )}
    >
      <div className="flex items-center gap-2">
        <Icon name={config.icon} size="sm" className="shrink-0" />
        <Badge variant={config.variant} size="sm">
          {config.label}
        </Badge>
        <span className="text-xs font-normal opacity-90">
          {reason || config.defaultReason}
        </span>
      </div>

      {(by || at) && (
        <div className="text-[11px] opacity-75">
          {by && <span>By {by} </span>}
          {at && <span>on {at}</span>}
        </div>
      )}
    </div>
  )
}

export function ReadOnlyState({ reason, className }: { reason?: string; className?: string }) {
  return <StatusTreatment type="read-only" reason={reason} className={className} />
}

export function LockedState({ lockedBy, className }: { lockedBy?: string; className?: string }) {
  return <StatusTreatment type="locked" by={lockedBy} className={className} />
}

export function FinalizedState({ date, className }: { date?: string; className?: string }) {
  return <StatusTreatment type="finalized" at={date} className={className} />
}
