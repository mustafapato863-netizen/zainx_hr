import * as React from "react"
import { cn } from "../../lib/utils"

export interface BrandMarkProps extends React.HTMLAttributes<HTMLSpanElement> {
  compact?: boolean
  inverse?: boolean
}

/** Uses the approved Zain X HR artwork from Ref/03_DESIGN_SYSTEM/ZainX_HR_Brand_Kit_v1.1_APPROVED. */
export function BrandMark({ className, compact = false, inverse = false, ...props }: BrandMarkProps) {
  return (
    <span className={cn("inline-flex items-center gap-3", className)} {...props}>
      <span
        aria-hidden="true"
        className={cn(
          "relative inline-flex shrink-0 items-center justify-center overflow-hidden",
          compact ? "h-9 w-9" : "h-11 w-11",
        )}
      >
        <img
          src={inverse ? "/brand/logos/zainx-hr-mark-white.png" : "/brand/logos/zainx-hr-mark.webp"}
          alt=""
          className="h-full w-full object-contain"
          decoding="async"
        />
      </span>
      {!compact && (
        <span className={cn("min-w-0 leading-none", inverse ? "text-white" : "text-text-primary")}>
          <span className="block truncate text-[0.95rem] font-semibold tracking-[-0.02em]">Zain X HR</span>
          <span className={cn("mt-1 block text-[0.65rem] font-medium uppercase tracking-[0.16em]", inverse ? "text-brand-cyan-300/75" : "text-text-tertiary")}>
            Human resources platform
          </span>
        </span>
      )}
    </span>
  )
}
