import * as React from "react"
import {
  TooltipTrigger as RACTooltipTrigger,
  Tooltip as RACTooltip,
  TooltipProps as RACTooltipProps,
  OverlayArrow,
} from "react-aria-components"
import { cn } from "../../lib/utils"

export interface TooltipProps extends Omit<RACTooltipProps, "className" | "children"> {
  className?: string
  content: React.ReactNode
  children: React.ReactElement
  delay?: number
}

export function Tooltip({
  className,
  content,
  children,
  delay = 300,
  offset = 8,
  ...props
}: TooltipProps) {
  return (
    <RACTooltipTrigger delay={delay}>
      {children}
      <RACTooltip
        offset={offset}
        className={cn(
          "z-50 max-w-xs rounded-md bg-surface-tooltip px-2.5 py-1 text-xs font-medium text-text-tooltip shadow-md outline-none enter:animate-in enter:fade-in-0 enter:zoom-in-95 exit:animate-out exit:fade-out-0 exit:zoom-out-95",
          className
        )}
        {...props}
      >
        <OverlayArrow>
          <svg width={8} height={8} viewBox="0 0 8 8" className="fill-surface-tooltip">
            <path d="M0 0 L4 4 L8 0 Z" />
          </svg>
        </OverlayArrow>
        {content}
      </RACTooltip>
    </RACTooltipTrigger>
  )
}
