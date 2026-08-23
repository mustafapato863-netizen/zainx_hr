import * as React from "react"
import { cn } from "../../lib/utils"
import { Card, CardProps } from "./Card"

export interface EmphasisCardProps extends CardProps {
  status?: "primary" | "warning" | "danger" | "success"
}

export function EmphasisCard({ className, status = "primary", ...props }: EmphasisCardProps) {
  const statusStyles = {
    primary: "border-primary/40 bg-surface-card shadow-sm hover:border-primary",
    warning: "border-warning/40 bg-surface-card shadow-sm hover:border-warning",
    danger: "border-danger/40 bg-surface-card shadow-sm hover:border-danger",
    success: "border-success/40 bg-surface-card shadow-sm hover:border-success",
  }

  return (
    <Card
      className={cn("border-2", statusStyles[status], className)}
      {...props}
    />
  )
}
