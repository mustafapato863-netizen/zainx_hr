import * as React from "react"
import {
  DialogTrigger as RACDialogTrigger,
  Popover as RACPopover,
  PopoverProps as RACPopoverProps,
  Dialog as RACDialog,
  OverlayArrow,
} from "react-aria-components"
import { cn } from "../../lib/utils"

export interface PopoverProps extends Omit<RACPopoverProps, "className" | "children" | "trigger"> {
  className?: string
  trigger: React.ReactNode
  children: React.ReactNode
  placement?: RACPopoverProps["placement"]
}

export function Popover({
  className,
  trigger,
  children,
  placement = "bottom",
  offset = 8,
  ...props
}: PopoverProps) {
  return (
    <RACDialogTrigger>
      {trigger}
      <RACPopover
        placement={placement}
        offset={offset}
        className={cn(
          "z-50 rounded-lg border border-border-default bg-surface-floating p-4 shadow-xl outline-none enter:animate-in enter:fade-in-0 enter:zoom-in-95 exit:animate-out exit:fade-out-0 exit:zoom-out-95",
          className
        )}
        {...props}
      >
        <OverlayArrow>
          <svg width={10} height={10} viewBox="0 0 10 10" className="fill-surface-floating stroke-border-default">
            <path d="M0 0 L5 5 L10 0 Z" />
          </svg>
        </OverlayArrow>
        <RACDialog className="outline-none text-text-primary">
          {children}
        </RACDialog>
      </RACPopover>
    </RACDialogTrigger>
  )
}
