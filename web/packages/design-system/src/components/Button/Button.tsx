import * as React from "react"
import { Button as RACButton, ButtonProps as RACButtonProps } from "react-aria-components"
import { cva, type VariantProps } from "class-variance-authority"
import { cn } from "../../lib/utils"

const buttonVariants = cva(
  "inline-flex items-center justify-center whitespace-nowrap rounded-md text-sm font-medium transition-all focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2 focus-visible:ring-offset-canvas disabled:pointer-events-none disabled:bg-surface-subtle disabled:text-text-disabled cursor-pointer disabled:cursor-not-allowed",
  {
    variants: {
      variant: {
        primary: "bg-primary text-text-inverse hover:bg-primary-hover active:bg-primary-pressed shadow-xs",
        secondary: "bg-surface-card border border-border-default text-text-primary hover:bg-surface-card-hover active:bg-surface-subtle shadow-xs",
        tertiary: "bg-surface-subtle text-text-primary hover:bg-border-default active:bg-border-strong",
        ghost: "hover:bg-surface-subtle hover:text-text-primary active:bg-border-default",
        danger: "bg-danger text-text-inverse hover:bg-danger-hover active:bg-danger/90 shadow-xs",
        outline: "border border-border-strong text-text-primary hover:bg-surface-subtle active:bg-border-default",
      },
      size: {
        xs: "h-7 px-2.5 text-xs gap-1.5",
        sm: "h-8 px-3 text-xs gap-1.5",
        md: "h-10 px-4 py-2 text-sm gap-2",
        lg: "h-12 px-6 text-base gap-2.5",
        icon: "h-10 w-10 p-0",
        "icon-sm": "h-8 w-8 p-0",
        "icon-xs": "h-7 w-7 p-0",
      },
    },
    defaultVariants: {
      variant: "primary",
      size: "md",
    },
  }
)

export interface ButtonProps
  extends Omit<RACButtonProps, "className">,
    VariantProps<typeof buttonVariants> {
  className?: string
  loading?: boolean
  disabled?: boolean
  children?: React.ReactNode
}

const Button = React.forwardRef<HTMLButtonElement, ButtonProps>(
  ({ className, variant, size, loading = false, disabled = false, isDisabled, children, ...props }, ref) => {
    const isActuallyDisabled = disabled || isDisabled || loading

    return (
      <RACButton
        ref={ref}
        isDisabled={isActuallyDisabled}
        className={cn(
          buttonVariants({ variant, size, className }),
          loading && "opacity-75 cursor-wait"
        )}
        {...props}
      >
        {loading && (
          <svg
            className="animate-spin -ms-0.5 me-2 h-4 w-4 shrink-0 text-current"
            xmlns="http://www.w3.org/2000/svg"
            fill="none"
            viewBox="0 0 24 24"
            aria-hidden="true"
          >
            <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
            <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
          </svg>
        )}
        {children}
      </RACButton>
    )
  }
)
Button.displayName = "Button"

export { Button, buttonVariants }
