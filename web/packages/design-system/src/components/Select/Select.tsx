import * as React from "react"
import {
  Select as RACSelect,
  SelectProps as RACSelectProps,
  Button as RACButton,
  SelectValue as RACSelectValue,
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
 * Select Component
 * 
 * CONTRACT DISTINCTION:
 * - `Select` is for non-editable choice from known options (no text typing).
 * - For searchable / autocomplete filtering with text input, use `ComboBox`.
 */

export interface SelectOption {
  id: string | number
  label: string
  description?: string
  disabled?: boolean
}

export interface SelectProps<T extends object>
  extends Omit<RACSelectProps<T>, "className" | "children"> {
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
  sm: "h-8 px-2.5 text-xs",
  md: "h-10 px-3 py-2 text-sm",
  lg: "h-12 px-4 text-base",
}

export function Select<T extends object>({
  className,
  label,
  description,
  error,
  placeholder = "Select an option...",
  invalid,
  isInvalid,
  disabled,
  isDisabled,
  size = "md",
  items,
  children,
  ...props
}: SelectProps<T>) {
  const isActuallyInvalid = Boolean(error || invalid || isInvalid)
  const isActuallyDisabled = Boolean(disabled || isDisabled)

  return (
    <RACSelect
      isDisabled={isActuallyDisabled}
      isInvalid={isActuallyInvalid}
      className={cn("flex flex-col gap-1.5 w-full", className)}
      {...props}
    >
      {label && <RACLabel className="text-sm font-medium text-text-primary leading-none">{label}</RACLabel>}
      <RACButton
        className={cn(
          "flex w-full items-center justify-between rounded-md border border-border-default bg-surface-input text-text-primary transition-colors focus:outline-none focus:ring-2 focus:ring-primary focus:border-primary disabled:cursor-not-allowed disabled:bg-surface-subtle disabled:text-text-disabled",
          sizeClasses[size],
          isActuallyInvalid && "border-danger focus:ring-danger focus:border-danger text-danger"
        )}
      >
        <RACSelectValue className="truncate placeholder:text-text-tertiary">
          {({ defaultChildren }) => defaultChildren || <span className="text-text-tertiary">{placeholder}</span>}
        </RACSelectValue>
        <Icon name="chevron-down" size="sm" className="text-text-tertiary shrink-0 ms-2" />
      </RACButton>
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
              <SelectItem key={item.id} id={item.id} textValue={item.label}>
                <div className="flex flex-col">
                  <span className="truncate">{item.label}</span>
                  {item.description && (
                    <span className="text-xs text-text-tertiary">{item.description}</span>
                  )}
                </div>
              </SelectItem>
            )
          )}
        </RACListBox>
      </RACPopover>
    </RACSelect>
  )
}

export interface SelectItemProps extends Omit<RACListBoxItemProps, "className"> {
  className?: string
  children?: React.ReactNode
}

export function SelectItem({ className, children, ...props }: SelectItemProps) {
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
