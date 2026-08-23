import * as React from "react"
import { cn } from "../../lib/utils"
import { Icon } from "../Icon/Icon"

export interface ToastItem {
  id: string
  title?: string
  message: React.ReactNode
  variant?: "info" | "success" | "warning" | "danger"
  duration?: number
}

interface ToastContextType {
  toasts: ToastItem[]
  showToast: (toast: Omit<ToastItem, "id">) => string
  dismissToast: (id: string) => void
}

const ToastContext = React.createContext<ToastContextType | null>(null)

export function ToastProvider({ children }: { children: React.ReactNode }) {
  const [toasts, setToasts] = React.useState<ToastItem[]>([])

  const dismissToast = React.useCallback((id: string) => {
    setToasts((prev) => prev.filter((t) => t.id !== id))
  }, [])

  const showToast = React.useCallback(
    (toast: Omit<ToastItem, "id">) => {
      const id = Math.random().toString(36).substring(2, 9)
      const newToast: ToastItem = { ...toast, id }

      setToasts((prev) => [...prev, newToast])

      if (toast.duration !== 0) {
        setTimeout(() => {
          dismissToast(id)
        }, toast.duration || 5000)
      }

      return id
    },
    [dismissToast]
  )

  return (
    <ToastContext.Provider value={{ toasts, showToast, dismissToast }}>
      {children}
      <div className="fixed bottom-4 end-4 z-50 flex flex-col gap-2 max-w-sm w-full pointer-events-none">
        {toasts.map((toast) => (
          <div
            key={toast.id}
            className={cn(
              "pointer-events-auto flex items-start gap-3 rounded-lg border p-4 shadow-xl text-sm transition-all enter:animate-in enter:slide-in-from-bottom-5",
              toast.variant === "success" && "bg-surface-raised border-success/30 text-text-primary",
              toast.variant === "danger" && "bg-surface-raised border-danger/30 text-text-primary",
              toast.variant === "warning" && "bg-surface-raised border-warning/30 text-text-primary",
              (!toast.variant || toast.variant === "info") && "bg-surface-raised border-border-default text-text-primary"
            )}
          >
            {toast.variant === "success" && <Icon name="check-circle" size="md" className="text-success mt-0.5" />}
            {toast.variant === "danger" && <Icon name="alert-circle" size="md" className="text-danger mt-0.5" />}
            {toast.variant === "warning" && <Icon name="alert-triangle" size="md" className="text-warning mt-0.5" />}
            {(!toast.variant || toast.variant === "info") && <Icon name="info" size="md" className="text-info mt-0.5" />}

            <div className="flex-1 space-y-1">
              {toast.title && <div className="font-semibold text-text-primary">{toast.title}</div>}
              <div className="text-text-secondary text-xs">{toast.message}</div>
            </div>

            <button
              type="button"
              onClick={() => dismissToast(toast.id)}
              className="text-text-tertiary hover:text-text-primary rounded p-1"
            >
              <Icon name="x" size="xs" />
            </button>
          </div>
        ))}
      </div>
    </ToastContext.Provider>
  )
}

export function useToast() {
  const context = React.useContext(ToastContext)
  if (!context) {
    throw new Error("useToast must be used within a ToastProvider")
  }
  return context
}
