import * as React from "react"
import { TextField as RACTextField, TextFieldProps as RACTextFieldProps, FieldError, Text } from "react-aria-components"
import { cn } from "../../lib/utils"
import { Label } from "./Label"

export interface FieldProps extends Omit<RACTextFieldProps, "className"> {
  className?: string
  label?: string
  description?: string
  error?: string
  required?: boolean
  invalid?: boolean
  disabled?: boolean
  children?: React.ReactNode
}

const Field = React.forwardRef<HTMLDivElement, FieldProps>(
  ({ className, label, description, error, required = false, invalid = false, disabled = false, isDisabled, isInvalid, isRequired, children, ...props }, ref) => {
    const fieldInvalid = Boolean(error || invalid || isInvalid)
    const fieldRequired = Boolean(required || isRequired)
    const fieldDisabled = Boolean(disabled || isDisabled)

    return (
      <RACTextField
        ref={ref}
        isRequired={fieldRequired}
        isInvalid={fieldInvalid}
        isDisabled={fieldDisabled}
        className={cn("flex flex-col gap-1.5 w-full", className)}
        {...props}
      >
        {label && <Label required={fieldRequired}>{label}</Label>}
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
      </RACTextField>
    )
  }
)
Field.displayName = "Field"

export { Field }
