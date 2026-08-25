import * as React from "react"
import {
  ModalOverlay as RACModalOverlay,
  Modal as RACModal,
  Dialog as RACDialog,
  DialogTrigger as RACDialogTrigger,
  Heading as RACHeading,
  ModalOverlayProps,
} from "react-aria-components"
import { cn } from "../../lib/utils"
import { Button } from "../Button/Button"
import { Icon } from "../Icon/Icon"

export interface DialogProps extends ModalOverlayProps {
  className?: string
  title?: string
  description?: string
  isOpen?: boolean
  onOpenChange?: (isOpen: boolean) => void
  children?: React.ReactNode
}

export function Dialog({
  className,
  title,
  description,
  isOpen,
  onOpenChange,
  children,
  ...props
}: DialogProps) {
  return (
    <RACModalOverlay
      isOpen={isOpen}
      onOpenChange={onOpenChange}
      isDismissable
      className="fixed inset-0 z-50 flex items-center justify-center bg-surface-overlay p-4 backdrop-blur-xs enter:animate-in enter:fade-in-0 exit:animate-out exit:fade-out-0"
      {...props}
    >
      <RACModal
        className={cn(
          "flex max-h-[calc(100dvh-2rem)] w-full max-w-lg flex-col overflow-hidden rounded-xl border border-border-default bg-surface-raised p-6 shadow-2xl outline-none enter:animate-in enter:zoom-in-95 enter:slide-in-from-bottom-2 exit:animate-out exit:zoom-out-95 exit:slide-out-to-bottom-2",
          className
        )}
      >
        <RACDialog className="flex min-h-0 flex-1 flex-col outline-none">
          {({ close }) => (
            <>
              <div className="flex shrink-0 items-center justify-between border-b border-border-default pb-3">
                {title && (
                  <RACHeading slot="title" className="text-lg font-semibold text-text-primary">
                    {title}
                  </RACHeading>
                )}
                <button
                  type="button"
                  onClick={close}
                  className="rounded-sm p-1 text-text-tertiary hover:bg-surface-subtle hover:text-text-primary focus:outline-none focus:ring-2 focus:ring-primary"
                >
                  <Icon name="x" size="sm" />
                </button>
              </div>
              <div className="min-h-0 flex-1 overflow-y-auto pe-1">
                {description && (
                  <p className="mt-2 text-sm text-text-secondary">{description}</p>
                )}
                <div className="mt-4">{children}</div>
              </div>
            </>
          )}
        </RACDialog>
      </RACModal>
    </RACModalOverlay>
  )
}

export interface ConfirmDialogProps {
  isOpen: boolean
  onOpenChange: (isOpen: boolean) => void
  title: string
  description: string
  confirmLabel?: string
  cancelLabel?: string
  variant?: "primary" | "danger"
  loading?: boolean
  onConfirm: () => void
}

export function ConfirmDialog({
  isOpen,
  onOpenChange,
  title,
  description,
  confirmLabel = "Confirm",
  cancelLabel = "Cancel",
  variant = "primary",
  loading = false,
  onConfirm,
}: ConfirmDialogProps) {
  return (
    <Dialog
      isOpen={isOpen}
      onOpenChange={onOpenChange}
      title={title}
      description={description}
    >
      <div className="mt-6 flex justify-end gap-3">
        <Button
          variant="secondary"
          disabled={loading}
          onClick={() => onOpenChange(false)}
        >
          {cancelLabel}
        </Button>
        <Button
          variant={variant === "danger" ? "danger" : "primary"}
          loading={loading}
          onClick={() => {
            onConfirm()
          }}
        >
          {confirmLabel}
        </Button>
      </div>
    </Dialog>
  )
}

export function DestructiveDialog(props: Omit<ConfirmDialogProps, "variant">) {
  return <ConfirmDialog {...props} variant="danger" />
}

export { RACDialogTrigger as DialogTrigger }
