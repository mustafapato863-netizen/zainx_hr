import * as React from "react"
import { cn } from "../../lib/utils"
import { Icon } from "../Icon/Icon"
import { Spinner } from "../Spinner/Spinner"

/**
 * SensitiveValue Component
 * 
 * SECURITY CONTRACT:
 * - This component represents UI state ONLY.
 * - Client callbacks (`onRevealRequest`) are UI interaction triggers, NOT an audit control.
 * - The backend is the sole authority for evaluating permissions, generating the audit log,
 *   and returning the authorized plaintext value.
 * - Under no circumstances does the client claim an audit occurred merely because `onRevealRequest` fired.
 */

export type SensitiveValueState = "masked" | "pending" | "revealed" | "error"

export interface SensitiveValueProps extends React.HTMLAttributes<HTMLSpanElement> {
  /** The revealed plaintext value supplied by backend when authorized */
  value?: string | number
  /** Masked placeholder string shown when hidden */
  maskedPlaceholder?: string
  /** Current state of the sensitive field */
  state?: SensitiveValueState
  /** Error message if reveal authorization was denied or failed */
  errorMessage?: string
  /** Callback fired when user clicks to request reveal from the backend */
  onRevealRequest?: () => void
  /** Callback fired when user clicks to re-mask the field */
  onMask?: () => void
  /** Accessible label for the reveal button */
  revealLabel?: string
  /** Accessible label for the hide button */
  hideLabel?: string
}

export function SensitiveValue({
  className,
  value,
  maskedPlaceholder = "••••••••••••",
  state = "masked",
  errorMessage,
  onRevealRequest,
  onMask,
  revealLabel = "Request reveal of confidential value (authorized & audited on server)",
  hideLabel = "Hide confidential value",
  ...props
}: SensitiveValueProps) {
  const isRevealed = state === "revealed" && value !== undefined
  const isPending = state === "pending"
  const isError = state === "error"

  return (
    <span
      className={cn("inline-flex items-center gap-1.5 font-mono text-sm", className)}
      {...props}
    >
      {isPending ? (
        <span className="inline-flex items-center gap-1.5 text-xs text-text-tertiary">
          <Spinner size="xs" />
          <span>Authorizing...</span>
        </span>
      ) : isError ? (
        <span className="inline-flex items-center gap-1 text-xs text-danger" title={errorMessage || "Access Denied"}>
          <Icon name="alert-circle" size="xs" />
          <span>{errorMessage || "Reveal Denied"}</span>
        </span>
      ) : isRevealed ? (
        <span className="text-text-primary">{value}</span>
      ) : (
        <span className="tracking-widest text-text-tertiary select-none">
          {maskedPlaceholder}
        </span>
      )}

      {isRevealed ? (
        onMask && (
          <button
            type="button"
            onClick={onMask}
            className="rounded p-0.5 text-text-tertiary hover:text-text-primary hover:bg-surface-subtle focus:outline-none focus:ring-1 focus:ring-primary"
            title={hideLabel}
            aria-label={hideLabel}
          >
            <Icon name="eye-off" size="xs" />
          </button>
        )
      ) : (
        onRevealRequest && !isPending && (
          <button
            type="button"
            onClick={onRevealRequest}
            className="rounded p-0.5 text-text-tertiary hover:text-text-primary hover:bg-surface-subtle focus:outline-none focus:ring-1 focus:ring-primary"
            title={revealLabel}
            aria-label={revealLabel}
          >
            <Icon name="eye" size="xs" />
          </button>
        )
      )}
    </span>
  )
}
