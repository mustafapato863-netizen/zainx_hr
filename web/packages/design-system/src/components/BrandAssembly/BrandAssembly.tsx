import * as React from "react"
import { cn } from "../../lib/utils"

/**
 * BRAND ASSEMBLY CHOREOGRAPHY — PROVISIONAL
 * 
 * NOTE:
 * - This component provides the baseline startup / login bootstrap presentation.
 * - Final official vector choreography will be verified when official brand assets are delivered.
 * - Includes mandatory reduced-motion fallback (`motion-reduce:animate-none`).
 */

export interface BrandAssemblyProps {
  className?: string
  message?: string
}

export function BrandAssembly({ className, message = "Initializing ZainX Workforce Platform..." }: BrandAssemblyProps) {
  return (
    <div
      className={cn(
        "flex min-h-screen w-full flex-col items-center justify-center bg-canvas p-6 text-center select-none",
        className
      )}
    >
      <div className="relative mb-8 flex items-center justify-center">
        {/* Ambient subtle glow with reduced-motion fallback */}
        <div className="absolute -inset-4 rounded-full bg-primary/20 blur-xl animate-pulse motion-reduce:animate-none" />
        
        {/* Geometric ZainX Brand Assembly Icon */}
        <div className="relative flex h-20 w-20 items-center justify-center rounded-2xl bg-surface border border-border-default shadow-2xl">
          <svg width="48" height="48" viewBox="0 0 48 48" fill="none" xmlns="http://www.w3.org/2000/svg">
            <rect width="48" height="48" rx="10" fill="currentColor" className="text-primary/10" />
            <path
              d="M14 16H34L20 32H34"
              stroke="currentColor"
              strokeWidth="4"
              strokeLinecap="round"
              strokeLinejoin="round"
              className="text-primary"
            />
          </svg>
        </div>
      </div>

      <h1 className="text-2xl font-bold tracking-tight text-text-primary mb-2">
        ZainX <span className="text-primary font-normal">Workforce</span>
      </h1>
      <p className="text-sm text-text-secondary max-w-xs">{message}</p>

      <div className="mt-8 h-1 w-48 overflow-hidden rounded-full bg-surface-subtle">
        <div className="h-full w-1/3 rounded-full bg-primary animate-[indeterminate_1.5s_infinite_linear] motion-reduce:animate-none" />
      </div>
    </div>
  )
}
