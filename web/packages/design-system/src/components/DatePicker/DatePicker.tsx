import * as React from "react"
import {
  DatePicker as RACDatePicker,
  DatePickerProps as RACDatePickerProps,
  DateInput,
  DateSegment,
  DateValue,
  Dialog,
  Popover,
  Calendar,
  CalendarGrid,
  CalendarCell,
  Heading,
  Button as RACButton,
  Label as RACLabel,
  FieldError,
  Text,
} from "react-aria-components"
import { cn } from "../../lib/utils"
import { Icon } from "../Icon/Icon"

export interface DatePickerProps<T extends DateValue>
  extends Omit<RACDatePickerProps<T>, "className"> {
  className?: string
  label?: string
  description?: string
  error?: string
  invalid?: boolean
  disabled?: boolean
}

export function DatePicker<T extends DateValue>({
  className,
  label,
  description,
  error,
  invalid,
  isInvalid,
  disabled,
  isDisabled,
  ...props
}: DatePickerProps<T>) {
  const isActuallyInvalid = Boolean(error || invalid || isInvalid)
  const isActuallyDisabled = Boolean(disabled || isDisabled)

  return (
    <RACDatePicker
      isDisabled={isActuallyDisabled}
      isInvalid={isActuallyInvalid}
      className={cn("flex flex-col gap-1.5 w-full", className)}
      {...props}
    >
      {label && <RACLabel className="text-sm font-medium text-text-primary leading-none">{label}</RACLabel>}
      <div
        className={cn(
          "flex h-10 w-full items-center justify-between rounded-md border border-border-default bg-surface-input px-3 py-2 text-sm text-text-primary transition-colors focus-within:ring-2 focus-within:ring-primary focus-within:border-primary",
          isActuallyInvalid && "border-danger focus-within:ring-danger text-danger",
          isActuallyDisabled && "bg-surface-subtle opacity-70 cursor-not-allowed"
        )}
      >
        <DateInput className="flex gap-0.5">
          {(segment) => (
            <DateSegment
              segment={segment}
              className="rounded px-0.5 outline-none focus:bg-primary focus:text-text-inverse"
            />
          )}
        </DateInput>
        <RACButton className="text-text-tertiary hover:text-text-primary outline-none">
          <Icon name="calendar" size="sm" />
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
      <Popover className="z-50 rounded-lg border border-border-default bg-surface-floating p-3 shadow-lg outline-none enter:animate-in enter:fade-in-0 enter:zoom-in-95">
        <Dialog className="outline-none">
          <Calendar className="w-full">
            <header className="flex items-center justify-between pb-2">
              <RACButton slot="previous" className="p-1 rounded hover:bg-surface-subtle">
                <Icon name="chevron-left" size="sm" />
              </RACButton>
              <Heading className="text-sm font-semibold text-text-primary" />
              <RACButton slot="next" className="p-1 rounded hover:bg-surface-subtle">
                <Icon name="chevron-right" size="sm" />
              </RACButton>
            </header>
            <CalendarGrid className="border-collapse">
              {(date) => (
                <CalendarCell
                  date={date}
                  className="flex h-8 w-8 items-center justify-center rounded-sm text-sm text-text-primary hover:bg-surface-subtle focus:outline-none focus:ring-2 focus:ring-primary selected:bg-primary selected:text-text-inverse disabled:opacity-30"
                />
              )}
            </CalendarGrid>
          </Calendar>
        </Dialog>
      </Popover>
    </RACDatePicker>
  )
}
