import * as React from "react"
import { cn } from "../../lib/utils"
import { Card, CardProps } from "./Card"
import { Icon } from "../Icon/Icon"

export interface SpotlightCardProps extends CardProps {
  badgeText?: string
}

export function SpotlightCard({
  className,
  badgeText = "AI Insight",
  children,
  ...props
}: SpotlightCardProps) {
  return (
    <div className="relative group">
      {/* Subtle controlled glow - reserved for significant insights */}
      <div className="absolute -inset-0.5 rounded-xl bg-gradient-to-r from-primary/30 to-purple-600/30 opacity-75 blur-sm transition duration-500 group-hover:opacity-100" />
      
      <Card
        className={cn(
          "relative border border-primary/30 bg-surface-card-spotlight backdrop-blur-xs",
          className
        )}
        {...props}
      >
        {badgeText && (
          <div className="absolute top-3 end-3 flex items-center gap-1 rounded-full bg-primary/10 px-2.5 py-0.5 text-[11px] font-semibold text-primary border border-primary/20">
            <Icon name="sparkles" size="xs" />
            <span>{badgeText}</span>
          </div>
        )}
        {children}
      </Card>
    </div>
  )
}
