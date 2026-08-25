import * as React from "react"
import { cn } from "../../lib/utils"
import { BrandMark } from "../BrandMark/BrandMark"

/**
 * BRAND ASSEMBLY CHOREOGRAPHY
 *
 * The mark is sourced from the approved Zain X HR Brand Kit. The animation is
 * intentionally restrained and sourced from the approved raster artwork.
 * Reduced-motion users receive the same mark without animated treatment.
 */

export interface BrandAssemblyProps {
  className?: string
  message?: string
}

export function BrandAssembly({ className, message = "Initializing Zain X HR..." }: BrandAssemblyProps) {
  return (
    <div
      className={cn(
        "flex min-h-screen w-full flex-col items-center justify-center bg-canvas p-6 text-center select-none",
        className
      )}
    >
      <div className="relative mb-8 flex items-center justify-center">
        <div className="absolute -inset-8 rounded-full bg-primary/15 blur-3xl animate-pulse motion-reduce:animate-none" />
        <div className="relative rounded-2xl border border-border-default bg-surface p-4 shadow-overlay motion-safe:animate-[brand-mark-enter_640ms_var(--ease-zainx)_both]">
          <BrandMark compact className="h-20 w-20" />
        </div>
      </div>

      <h1 className="mb-2 text-2xl font-semibold tracking-tight text-text-primary">Zain X HR</h1>
      <p className="text-sm text-text-secondary max-w-xs">{message}</p>

      <div className="mt-8 h-1 w-48 overflow-hidden rounded-full bg-surface-subtle">
        <div className="h-full w-1/3 rounded-full bg-primary animate-[indeterminate_1.5s_infinite_linear] motion-reduce:animate-none" />
      </div>
    </div>
  )
}
