import * as React from "react"
import {
  MenuTrigger as RACMenuTrigger,
  Menu as RACMenu,
  MenuProps as RACMenuProps,
  MenuItem as RACMenuItem,
  MenuItemProps as RACMenuItemProps,
  Popover as RACPopover,
  Separator as RACSeparator,
  Section as RACSection,
  Header as RACHeader,
} from "react-aria-components"
import { cn } from "../../lib/utils"

export interface MenuProps<T> extends Omit<RACMenuProps<T>, "className"> {
  className?: string
  trigger: React.ReactNode
}

export function Menu<T extends object>({
  className,
  trigger,
  children,
  ...props
}: MenuProps<T>) {
  return (
    <RACMenuTrigger>
      {trigger}
      <RACPopover
        offset={6}
        className="z-50 min-w-[180px] rounded-md border border-border-default bg-surface-floating p-1 shadow-lg outline-none enter:animate-in enter:fade-in-0 enter:zoom-in-95 exit:animate-out exit:fade-out-0 exit:zoom-out-95"
      >
        <RACMenu className={cn("outline-none", className)} {...props}>
          {children}
        </RACMenu>
      </RACPopover>
    </RACMenuTrigger>
  )
}

export interface MenuItemProps extends Omit<RACMenuItemProps, "className"> {
  className?: string
  destructive?: boolean
  children?: React.ReactNode
}

export function MenuItem({ className, destructive, children, ...props }: MenuItemProps) {
  return (
    <RACMenuItem
      className={cn(
        "relative flex cursor-pointer select-none items-center rounded-sm px-2.5 py-1.5 text-sm text-text-primary outline-none transition-colors hover:bg-surface-subtle focus:bg-surface-subtle focus:text-primary data-[disabled=true]:pointer-events-none data-[disabled=true]:opacity-50",
        destructive && "text-danger hover:bg-danger-subtle hover:text-danger-hover focus:bg-danger-subtle focus:text-danger-hover",
        className
      )}
      {...props}
    >
      {children}
    </RACMenuItem>
  )
}

export function MenuSeparator({ className }: { className?: string }) {
  return <RACSeparator className={cn("my-1 h-px bg-border-default", className)} />
}

export function MenuSection<T extends object>({
  title,
  children,
  ...props
}: { title?: string; children: React.ReactNode }) {
  return (
    <RACSection className="py-1">
      {title && (
        <RACHeader className="px-2.5 py-1 text-xs font-semibold uppercase tracking-wider text-text-tertiary">
          {title}
        </RACHeader>
      )}
      {children}
    </RACSection>
  )
}
