import * as React from "react"
import {
  Tabs as RACTabs,
  TabList as RACTabList,
  Tab as RACTab,
  TabPanel as RACTabPanel,
  TabsProps as RACTabsProps,
  TabProps as RACTabProps,
  TabPanelProps as RACTabPanelProps,
} from "react-aria-components"
import { cn } from "../../lib/utils"

export interface TabsProps extends Omit<RACTabsProps, "className"> {
  className?: string
  variant?: "underline" | "pill"
  children: React.ReactNode
}

export function Tabs({ className, variant = "underline", children, ...props }: TabsProps) {
  return (
    <RACTabs
      className={cn("flex flex-col gap-3 w-full", className)}
      {...props}
    >
      {children}
    </RACTabs>
  )
}

export function TabList({ className, children, ...props }: React.ComponentPropsWithoutRef<typeof RACTabList>) {
  return (
    <RACTabList
      className={cn(
        "flex items-center gap-1 border-b border-border-default pb-px overflow-x-auto",
        className
      )}
      {...props}
    >
      {children}
    </RACTabList>
  )
}

export interface TabItemProps extends Omit<RACTabProps, "className"> {
  className?: string
  children: React.ReactNode
}

export function Tab({ className, children, ...props }: TabItemProps) {
  return (
    <RACTab
      className={cn(
        "relative flex cursor-pointer items-center justify-center whitespace-nowrap px-3.5 py-2 text-sm font-medium transition-colors outline-none",
        "text-text-secondary hover:text-text-primary",
        "selected:text-primary selected:font-semibold",
        "selected:after:absolute selected:after:bottom-0 selected:after:start-0 selected:after:end-0 selected:after:h-0.5 selected:after:bg-primary",
        "focus-visible:ring-2 focus-visible:ring-primary focus-visible:rounded-sm",
        "disabled:pointer-events-none disabled:opacity-40",
        className
      )}
      {...props}
    >
      {children}
    </RACTab>
  )
}

export function TabPanel({ className, children, ...props }: React.ComponentPropsWithoutRef<typeof RACTabPanel>) {
  return (
    <RACTabPanel
      className={cn("mt-2 outline-none focus-visible:ring-2 focus-visible:ring-primary rounded-md", className)}
      {...props}
    >
      {children}
    </RACTabPanel>
  )
}
