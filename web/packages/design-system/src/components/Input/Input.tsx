import * as React from "react"
import { Input as RACInput, InputProps as RACInputProps } from "react-aria-components"
import { cn } from "../../lib/utils"

export interface InputProps extends Omit<RACInputProps, "className" | "size"> {
  className?: string
  error?: boolean
  invalid?: boolean
  disabled?: boolean
  isDisabled?: boolean
  size?: "sm" | "md" | "lg"
}

const sizeClasses = {
  sm: "h-8 px-2.5 text-xs",
  md: "h-10 px-3 py-2 text-sm",
  lg: "h-12 px-4 text-base",
}

const Input = React.forwardRef<HTMLInputElement, InputProps>(
  ({ className, type = "text", error, invalid, disabled, isDisabled, size = "md", ...props }, ref) => {
    const isInvalid = Boolean(error || invalid)
    const isActuallyDisabled = Boolean(disabled || isDisabled)

    return (
      <RACInput
        ref={ref}
        type={type}
        disabled={isActuallyDisabled}
        className={cn(
          "flex w-full rounded-md border border-border-default bg-surface-input text-text-primary transition-colors file:border-0 file:bg-transparent file:text-sm file:font-medium placeholder:text-text-tertiary focus:outline-none focus:ring-2 focus:ring-primary focus:border-primary disabled:cursor-not-allowed disabled:bg-surface-subtle disabled:text-text-disabled read-only:bg-surface-subtle read-only:cursor-default",
          sizeClasses[size],
          isInvalid && "border-danger focus:ring-danger focus:border-danger text-danger placeholder:text-danger/50",
          className
        )}
        {...props}
      />
    )
  }
)
Input.displayName = "Input"

export { Input }
