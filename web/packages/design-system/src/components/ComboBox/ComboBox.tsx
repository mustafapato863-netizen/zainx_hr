import * as React from "react"
import {
  ComboBox as RACComboBox,
  ComboBoxProps as RACComboBoxProps,
  Input as RACInput,
  Button as RACButton,
  Popover as RACPopover,
  ListBox as RACListBox,
  ListBoxItem as RACListBoxItem,
  ListBoxItemProps as RACListBoxItemProps,
  Label as RACLabel,
  FieldError,
  Text,
} from "react-aria-components"
import { cn } from "../../lib/utils"
import { Icon } from "../Icon/Icon"

/**
 * ComboBox Component
 * 
 * CONTRACT DISTINCTION:
 * - `ComboBox` provides an editable/searchable text input filter over options.
 * - For non-editable selection from known options, use `Select`.
 */

export interface ComboBoxOption {
  id: string | number
  label: string
  description?: string
  disabled?: boolean
}

export interface ComboBoxProps<T extends object>
  extends Omit<RACComboBoxProps<T>, "className" | "children"> {
  className?: string
  label?: string
  description?: string
  error?: string
  placeholder?: string
  invalid?: boolean
  disabled?: boolean
  size?: "sm" | "md" | "lg"
  items?: Iterable<T>
  children?: React.ReactNode | ((item: T) => React.ReactNode)
}

const sizeClasses = {
  sm: "h-8 text-xs",
  md: "h-10 text-sm",
  lg: "h-12 text-base",
}

export function ComboBox<T extends object>({
  className,
  label,
  description,
  error,
  placeholder = "Search or select...",
  invalid,
  isInvalid,
  disabled,
  isDisabled,
  size = "md",
  items,
  children,
  ...props
}: ComboBoxProps<T>) {
  const isActuallyInvalid = Boolean(error || invalid || isInvalid)
  const isActuallyDisabled = Boolean(disabled || isDisabled)

  return (
    <RACComboBox
      isDisabled={isActuallyDisabled}
      isInvalid={isActuallyInvalid}
      className={cn("flex flex-col gap-1.5 w-full", className)}
      {...props}
    >
      {label && <RACLabel className="text-sm font-medium text-text-primary leading-none">{label}</RACLabel>}
      <div
        className={cn(
          "flex w-full items-center rounded-md border border-border-default bg-surface-input text-text-primary transition-colors focus-within:ring-2 focus-within:ring-primary focus-within:border-primary disabled:cursor-not-allowed disabled:bg-surface-subtle",
          sizeClasses[size],
          isActuallyInvalid && "border-danger focus-within:ring-danger focus-within:border-danger text-danger",
          isActuallyDisabled && "bg-surface-subtle opacity-70 cursor-not-allowed"
        )}
      >
        <RACInput
          placeholder={placeholder}
          className="flex-1 bg-transparent px-3 py-2 text-text-primary outline-none placeholder:text-text-tertiary"
        />
        <RACButton className="flex h-full items-center px-2 text-text-tertiary hover:text-text-primary">
          <Icon name="chevron-down" size="sm" />
        </RACButton>
      </div>
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
      <RACPopover
        className="z-50 min-w-[var(--trigger-width)] max-h-60 overflow-y-auto rounded-md border border-border-default bg-surface-floating shadow-lg outline-none enter:animate-in enter:fade-in-0 enter:zoom-in-95 exit:animate-out exit:fade-out-0 exit:zoom-out-95"
      >
        <RACListBox items={items} className="p-1 outline-none">
          {children ? (
            children
          ) : (
            (item: any) => (
              <ComboBoxItem key={item.id} id={item.id} textValue={item.label}>
                <div className="flex flex-col">
                  <span className="truncate">{item.label}</span>
                  {item.description && (
                    <span className="text-xs text-text-tertiary">{item.description}</span>
                  )}
                </div>
              </ComboBoxItem>
            )
          )}
        </RACListBox>
      </RACPopover>
    </RACComboBox>
  )
}

export interface ComboBoxItemProps extends Omit<RACListBoxItemProps, "className"> {
  className?: string
  children?: React.ReactNode
}

export function ComboBoxItem({ className, children, ...props }: ComboBoxItemProps) {
  return (
    <RACListBoxItem
      className={cn(
        "relative flex w-full cursor-pointer select-none items-center justify-between rounded-sm px-2.5 py-1.5 text-sm text-text-primary outline-none transition-colors hover:bg-surface-subtle focus:bg-surface-subtle focus:text-primary data-[selected=true]:bg-surface-selected data-[selected=true]:text-primary data-[disabled=true]:pointer-events-none data-[disabled=true]:opacity-50",
        className
      )}
      {...props}
    >
      {({ isSelected }) => (
        <>
          <div className="flex-1 truncate">{children}</div>
          {isSelected && <Icon name="check" size="sm" className="text-primary ms-2 shrink-0" />}
        </>
      )}
    </RACListBoxItem>
  )
}
