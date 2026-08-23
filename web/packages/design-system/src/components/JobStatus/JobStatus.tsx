import * as React from "react"
import { cn } from "../../lib/utils"
import { Spinner } from "../Spinner/Spinner"
import { Icon } from "../Icon/Icon"
import { Badge } from "../Badge/Badge"

export type JobStateType = "queued" | "running" | "completed" | "failed" | "cancelled"

export interface JobStatusProps {
  className?: string
  status: JobStateType
  title: string
  progress?: number
  message?: string
  startedAt?: string
}

export function JobStatus({
  className,
  status,
  title,
  progress,
  message,
  startedAt,
}: JobStatusProps) {
  const getBadgeVariant = () => {
    switch (status) {
      case "queued":
        return "neutral"
      case "running":
        return "primary"
      case "completed":
        return "success"
      case "failed":
        return "danger"
      case "cancelled":
        return "neutral"
    }
  }

  return (
    <div className={cn("flex flex-col gap-2 rounded-lg border border-border-default bg-surface p-4 text-sm", className)}>
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          {status === "running" && <Spinner size="sm" />}
          {status === "completed" && <Icon name="check-circle" size="sm" className="text-success" />}
          {status === "failed" && <Icon name="alert-circle" size="sm" className="text-danger" />}
          {status === "queued" && <Icon name="clock" size="sm" className="text-text-tertiary" />}
          <span className="font-semibold text-text-primary">{title}</span>
        </div>
        <Badge variant={getBadgeVariant()} dot>
          {status.toUpperCase()}
        </Badge>
      </div>

      {typeof progress === "number" && (
        <div className="space-y-1">
          <div className="flex justify-between text-xs text-text-secondary">
            <span>Progress</span>
            <span>{Math.round(progress)}%</span>
          </div>
          <div className="h-1.5 w-full overflow-hidden rounded-full bg-surface-subtle">
            <div
              className="h-full bg-primary transition-all duration-300"
              style={{ width: `${Math.min(100, Math.max(0, progress))}%` }}
            />
          </div>
        </div>
      )}

      {(message || startedAt) && (
        <div className="flex items-center justify-between text-xs text-text-tertiary">
          {message && <span>{message}</span>}
          {startedAt && <span>Started {startedAt}</span>}
        </div>
      )}
    </div>
  )
}
