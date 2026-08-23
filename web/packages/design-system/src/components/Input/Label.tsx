import * as React from "react"
import { Label as RACLabel, LabelProps as RACLabelProps } from "react-aria-components"
import { cn } from "../../lib/utils"

export interface LabelProps extends RACLabelProps {
  required?: boolean
}

const Label = React.forwardRef<HTMLLabelElement, LabelProps>(
  ({ className, required, children, ...props }, ref) => (
    <RACLabel
      ref={ref}
      className={cn(
        "text-sm font-medium text-text-primary leading-none cursor-default peer-disabled:cursor-not-allowed peer-disabled:opacity-70",
        required && "after:content-['*'] after:ms-0.5 after:text-danger",
        className
      )}
      {...props}
    >
      {children}
    </RACLabel>
  )
)
Label.displayName = "Label"

export { Label }
