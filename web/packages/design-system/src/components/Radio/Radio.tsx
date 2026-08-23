import * as React from "react"
import {
  RadioGroup as RACRadioGroup,
  RadioGroupProps as RACRadioGroupProps,
  Radio as RACRadio,
  RadioProps as RACRadioProps,
  Label as RACLabel,
  FieldError,
  Text,
} from "react-aria-components"
import { cn } from "../../lib/utils"

export interface RadioGroupProps extends Omit<RACRadioGroupProps, "className"> {
  className?: string
  label?: string
  description?: string
  error?: string
  disabled?: boolean
  children?: React.ReactNode
}

export function RadioGroup({
  className,
  label,
  description,
  error,
  disabled,
  isDisabled,
  children,
  ...props
}: RadioGroupProps) {
  const isGroupDisabled = Boolean(disabled || isDisabled)

  return (
    <RACRadioGroup
      isDisabled={isGroupDisabled}
      className={cn("flex flex-col gap-2", className)}
      {...props}
    >
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
    </RACRadioGroup>
  )
}

export interface RadioProps extends Omit<RACRadioProps, "className"> {
  className?: string
  disabled?: boolean
  children?: React.ReactNode
}

export function Radio({ className, disabled, isDisabled, children, ...props }: RadioProps) {
  const isRadioDisabled = Boolean(disabled || isDisabled)

  return (
    <RACRadio
      isDisabled={isRadioDisabled}
      className={cn(
        "group flex items-center gap-2.5 text-sm text-text-primary cursor-pointer disabled:cursor-not-allowed disabled:opacity-60",
        className
      )}
      {...props}
    >
      {({ isSelected, isFocusVisible }) => (
        <>
          <div
            className={cn(
              "flex h-4 w-4 shrink-0 items-center justify-center rounded-full border border-border-strong bg-surface transition-colors",
              isSelected && "border-primary",
              isFocusVisible && "ring-2 ring-primary ring-offset-2 ring-offset-canvas",
              isRadioDisabled && "bg-surface-subtle border-border-default"
            )}
          >
            {isSelected && <div className="h-2 w-2 rounded-full bg-primary" />}
          </div>
          {children}
        </>
      )}
    </RACRadio>
  )
}
