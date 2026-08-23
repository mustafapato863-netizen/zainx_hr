import * as React from "react"
import { Button, ButtonProps } from "./Button"
import { cn } from "../../lib/utils"

export interface IconButtonProps extends Omit<ButtonProps, "size"> {
  "aria-label": string
  size?: "icon-xs" | "icon-sm" | "icon"
}

const IconButton = React.forwardRef<HTMLButtonElement, IconButtonProps>(
  ({ className, size = "icon", "aria-label": ariaLabel, children, ...props }, ref) => {
    return (
      <Button
        ref={ref}
        size={size}
        aria-label={ariaLabel}
        className={cn("shrink-0", className)}
        {...props}
      >
        {children}
      </Button>
    )
  }
)
IconButton.displayName = "IconButton"

export { IconButton }
