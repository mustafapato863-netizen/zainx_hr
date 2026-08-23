import * as React from "react"
import { cn } from "../../lib/utils"

export interface SkeletonProps extends React.HTMLAttributes<HTMLDivElement> {
  variant?: "rectangular" | "circular" | "text"
  width?: string | number
  height?: string | number
}

export function Skeleton({
  className,
  variant = "rectangular",
  width,
  height,
  style,
  ...props
}: SkeletonProps) {
  const variantClasses = {
    rectangular: "rounded-md",
    circular: "rounded-full",
    text: "rounded h-4 my-1",
  }

  return (
    <div
      aria-hidden="true"
      className={cn(
        "animate-pulse bg-surface-subtle",
        variantClasses[variant],
        className
      )}
      style={{
        width,
        height,
        ...style,
      }}
      {...props}
    />
  )
}
