import * as React from "react"
import {
  ModalOverlay as RACModalOverlay,
  Modal as RACModal,
  Dialog as RACDialog,
  Heading as RACHeading,
  ModalOverlayProps,
} from "react-aria-components"
import { cn } from "../../lib/utils"
import { Icon } from "../Icon/Icon"

export type DrawerSide = "start" | "end" | "bottom"

export interface DrawerProps extends ModalOverlayProps {
  className?: string
  title?: string
  description?: string
  /** Semantic logical slide-out position ('start', 'end', or 'bottom') */
  side?: DrawerSide
  isOpen?: boolean
  onOpenChange?: (isOpen: boolean) => void
  children?: React.ReactNode
}

export function Drawer({
  className,
  title,
  description,
  side = "end",
  isOpen,
  onOpenChange,
  children,
  ...props
}: DrawerProps) {
  const sideClasses: Record<DrawerSide, string> = {
    end: "inset-y-0 end-0 h-full w-full max-w-md border-s border-border-default enter:slide-in-from-right exit:slide-out-to-right rtl:enter:slide-in-from-left rtl:exit:slide-out-to-left",
    start: "inset-y-0 start-0 h-full w-full max-w-md border-e border-border-default enter:slide-in-from-left exit:slide-out-to-left rtl:enter:slide-in-from-right rtl:exit:slide-out-to-right",
    bottom: "inset-x-0 bottom-0 max-h-[85vh] w-full border-t border-border-default rounded-t-2xl enter:slide-in-from-bottom exit:slide-out-to-bottom",
  }

  return (
    <RACModalOverlay
      isOpen={isOpen}
      onOpenChange={onOpenChange}
      isDismissable
      className="fixed inset-0 z-50 bg-surface-overlay backdrop-blur-xs enter:animate-in enter:fade-in-0 exit:animate-out exit:fade-out-0"
      {...props}
    >
      <RACModal
        className={cn(
          "fixed bg-surface-raised p-6 shadow-2xl outline-none duration-200 enter:animate-in exit:animate-out overflow-y-auto flex flex-col",
          sideClasses[side],
          className
        )}
      >
        <RACDialog className="outline-none flex flex-col h-full">
          {({ close }) => (
            <>
              <div className="flex items-center justify-between pb-3 border-b border-border-default">
                {title && (
                  <RACHeading slot="title" className="text-lg font-semibold text-text-primary">
                    {title}
                  </RACHeading>
                )}
                <button
                  type="button"
                  onClick={close}
                  className="rounded-sm p-1 text-text-tertiary hover:bg-surface-subtle hover:text-text-primary focus:outline-none focus:ring-2 focus:ring-primary"
                  aria-label="Close drawer"
                >
                  <Icon name="x" size="sm" />
                </button>
              </div>
              {description && (
                <p className="mt-2 text-sm text-text-secondary">{description}</p>
              )}
              <div className="mt-4 flex-1">{children}</div>
            </>
          )}
        </RACDialog>
      </RACModal>
    </RACModalOverlay>
  )
}
