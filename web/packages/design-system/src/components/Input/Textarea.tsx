import * as React from "react"
import { TextArea as RACTextArea, TextAreaProps as RACTextAreaProps } from "react-aria-components"
import { cn } from "../../lib/utils"

export interface TextareaProps extends Omit<RACTextAreaProps, "className"> {
  className?: string
  error?: boolean
  invalid?: boolean
  disabled?: boolean
  isDisabled?: boolean
}

const Textarea = React.forwardRef<HTMLTextAreaElement, TextareaProps>(
  ({ className, error, invalid, disabled, isDisabled, ...props }, ref) => {
    const isInvalid = Boolean(error || invalid)
    const isActuallyDisabled = Boolean(disabled || isDisabled)

    return (
      <RACTextArea
        ref={ref}
        disabled={isActuallyDisabled}
        className={cn(
          "flex min-h-[80px] w-full rounded-md border border-border-default bg-surface-input px-3 py-2 text-sm text-text-primary placeholder:text-text-tertiary transition-colors focus:outline-none focus:ring-2 focus:ring-primary focus:border-primary disabled:cursor-not-allowed disabled:bg-surface-subtle disabled:text-text-disabled read-only:bg-surface-subtle",
          isInvalid && "border-danger focus:ring-danger focus:border-danger text-danger",
          className
        )}
        {...props}
      />
    )
  }
)
Textarea.displayName = "Textarea"

export { Textarea }
