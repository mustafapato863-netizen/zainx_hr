import * as React from "react"
import { NumberField, NumberFieldProps, Group, Input as RACInput, Button as RACButton, Label as RACLabel, FieldError, Text } from "react-aria-components"
import { cn } from "../../lib/utils"

export interface NumberInputProps extends Omit<NumberFieldProps, "className"> {
  className?: string
  label?: string
  description?: string
  error?: string
  currency?: string
  disabled?: boolean
  invalid?: boolean
  size?: "sm" | "md" | "lg"
}

const sizeClasses = {
  sm: "h-8 text-xs",
  md: "h-10 text-sm",
  lg: "h-12 text-base",
}

export function NumberInput({
  className,
  label,
  description,
  error,
  currency,
  disabled,
  isDisabled,
  invalid,
  isInvalid,
  size = "md",
  ...props
}: NumberInputProps) {
  const isFieldInvalid = Boolean(error || invalid || isInvalid)
  const isFieldDisabled = Boolean(disabled || isDisabled)

  return (
    <NumberField
      isDisabled={isFieldDisabled}
      isInvalid={isFieldInvalid}
      formatOptions={currency ? { style: "currency", currency } : undefined}
      className={cn("flex flex-col gap-1.5 w-full", className)}
      {...props}
    >
      {label && (
        <RACLabel className="text-sm font-medium text-text-primary leading-none">
          {label}
        </RACLabel>
      )}
      <Group
        className={cn(
          "flex w-full items-center rounded-md border border-border-default bg-surface-input focus-within:ring-2 focus-within:ring-primary focus-within:border-primary",
          isFieldInvalid && "border-danger focus-within:ring-danger focus-within:border-danger",
          isFieldDisabled && "bg-surface-subtle opacity-70 cursor-not-allowed"
        )}
      >
        <RACInput
          className={cn(
            "flex-1 bg-transparent px-3 py-2 text-text-primary outline-none placeholder:text-text-tertiary",
            sizeClasses[size]
          )}
        />
        <div className="flex flex-col border-s border-border-default">
          <RACButton
            slot="increment"
            className="flex h-5 w-6 items-center justify-center text-text-secondary hover:bg-surface-subtle active:bg-border-default disabled:opacity-40"
          >
            ▲
          </RACButton>
          <RACButton
            slot="decrement"
            className="flex h-5 w-6 items-center justify-center border-t border-border-default text-text-secondary hover:bg-surface-subtle active:bg-border-default disabled:opacity-40"
          >
            ▼
          </RACButton>
        </div>
      </Group>
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
    </NumberField>
  )
}
