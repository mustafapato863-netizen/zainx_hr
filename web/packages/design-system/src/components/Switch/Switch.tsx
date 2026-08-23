import * as React from "react"
import { Switch as RACSwitch, SwitchProps as RACSwitchProps } from "react-aria-components"
import { cn } from "../../lib/utils"

export interface SwitchProps extends Omit<RACSwitchProps, "className"> {
  className?: string
  disabled?: boolean
  children?: React.ReactNode
}

const Switch = React.forwardRef<HTMLLabelElement, SwitchProps>(
  ({ className, disabled, isDisabled, children, ...props }, ref) => {
    const isActuallyDisabled = Boolean(disabled || isDisabled)

    return (
      <RACSwitch
        ref={ref}
        isDisabled={isActuallyDisabled}
        className={cn(
          "group inline-flex items-center gap-3 text-sm text-text-primary cursor-pointer disabled:cursor-not-allowed disabled:opacity-60",
          className
        )}
        {...props}
      >
        {({ isSelected, isFocusVisible }) => (
          <>
            <div
              className={cn(
                "relative inline-flex h-5 w-9 shrink-0 items-center rounded-full border-2 border-transparent bg-border-strong transition-colors",
                isSelected && "bg-primary",
                isFocusVisible && "ring-2 ring-primary ring-offset-2 ring-offset-canvas",
                isActuallyDisabled && "bg-border-default opacity-60"
              )}
            >
              <div
                className={cn(
                  "pointer-events-none block h-4 w-4 rounded-full bg-surface shadow-xs transition-transform transform translate-x-0 rtl:-translate-x-0",
                  isSelected && "translate-x-4 rtl:-translate-x-4"
                )}
              />
            </div>
            {children}
          </>
        )}
      </RACSwitch>
    )
  }
)
Switch.displayName = "Switch"

export { Switch }
