import * as React from "react"
import {
  Checkbox as RACCheckbox,
  CheckboxProps as RACCheckboxProps,
  CheckboxGroup as RACCheckboxGroup,
  CheckboxGroupProps as RACCheckboxGroupProps,
  Label as RACLabel,
  FieldError,
  Text,
} from "react-aria-components"
import { cn } from "../../lib/utils"

export interface CheckboxProps extends Omit<RACCheckboxProps, "className"> {
  className?: string
  error?: boolean
  invalid?: boolean
  disabled?: boolean
  children?: React.ReactNode
}

const Checkbox = React.forwardRef<HTMLLabelElement, CheckboxProps>(
  ({ className, error, invalid, disabled, isDisabled, isInvalid, children, ...props }, ref) => {
    const isActuallyDisabled = Boolean(disabled || isDisabled)
    const isActuallyInvalid = Boolean(error || invalid || isInvalid)

    return (
      <RACCheckbox
        ref={ref}
        isDisabled={isActuallyDisabled}
        isInvalid={isActuallyInvalid}
        className={cn(
          "group flex items-center gap-2.5 text-sm text-text-primary cursor-pointer disabled:cursor-not-allowed disabled:opacity-60",
          className
        )}
        {...props}
      >
        {({ isSelected, isIndeterminate, isFocusVisible }) => (
          <>
            <div
              className={cn(
                "flex h-4 w-4 shrink-0 items-center justify-center rounded-sm border border-border-strong bg-surface transition-colors",
                (isSelected || isIndeterminate) && "bg-primary border-primary text-text-inverse",
                isFocusVisible && "ring-2 ring-primary ring-offset-2 ring-offset-canvas",
                isActuallyInvalid && "border-danger bg-danger/5",
                isActuallyDisabled && "bg-surface-subtle border-border-default"
              )}
            >
              {isIndeterminate ? (
                <svg width="10" height="2" viewBox="0 0 10 2" fill="none">
                  <rect width="10" height="2" rx="1" fill="currentColor" />
                </svg>
              ) : isSelected ? (
                <svg width="12" height="12" viewBox="0 0 15 15" fill="none">
                  <path
                    d="M11.4669 3.72684C11.7558 3.91574 11.8369 4.30308 11.648 4.59198L7.39799 11.092C7.29783 11.2452 7.13556 11.3467 6.95402 11.3699C6.77247 11.3931 6.58989 11.3355 6.45446 11.2124L3.70446 8.71241C3.44905 8.48022 3.43023 8.08494 3.66242 7.82953C3.89461 7.57412 4.28989 7.55529 4.5453 7.78749L6.75292 9.79441L10.6018 3.90792C10.7907 3.61902 11.178 3.53795 11.4669 3.72684Z"
                    fill="currentColor"
                    fillRule="evenodd"
                    clipRule="evenodd"
                  />
                </svg>
              ) : null}
            </div>
            {children}
          </>
        )}
      </RACCheckbox>
    )
  }
)
Checkbox.displayName = "Checkbox"

export interface CheckboxGroupProps extends Omit<RACCheckboxGroupProps, "className"> {
  className?: string
  label?: string
  description?: string
  error?: string
  children?: React.ReactNode
}

export function CheckboxGroup({
  className,
  label,
  description,
  error,
  children,
  ...props
}: CheckboxGroupProps) {
  return (
    <RACCheckboxGroup className={cn("flex flex-col gap-2", className)} {...props}>
      {label && <RACLabel className="text-sm font-medium text-text-primary">{label}</RACLabel>}
      {children}
      {description && !error && (
        <Text slot="description" className="text-xs text-text-tertiary">
          {description}
        </Text>
      )}
      {error && (
        <FieldError className="text-xs font-medium text-danger">
          {error}
        </FieldError>
      )}
    </RACCheckboxGroup>
  )
}

export { Checkbox }
